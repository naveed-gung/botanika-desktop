using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Botanika_Desktop.Firebase
{
    // Main bridge to Firestore — uses the Firebase Admin SDK service account JSON
    // to mint a service account JWT and exchange it for a Google OAuth2 access token.
    // This works on .NET Framework 4.7.2 without any extra packages beyond Newtonsoft.Json.
    public sealed class FirebaseService
    {
        // Singleton — one connection for the whole app
        public static FirebaseService Instance { get; } = new FirebaseService();

        // Project ID read from serviceAccount.json
        private string _projectId;

        // Firestore REST base URL
        private string BaseUrl => $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents";

        // Shared HttpClient
        private readonly HttpClient _http = new HttpClient();

        // Service account OAuth2 access token (not a user token)
        private string _serviceToken;
        // When the service token expires (they last 1 hour)
        private DateTime _serviceTokenExpiry = DateTime.MinValue;

        // User ID token from Firebase Auth (set after sign-in, used for admin check)
        private string _idToken;

        // Loaded Admin SDK config
        private AdminSdkConfig _adminConfig;

        // Web API key — only needed for the email/password sign-in (isAdmin check)
        private string _webApiKey;

        private FirebaseService() { LoadConfig(); }

        // ─── Config Loading ────────────────────────────────────────────────────

        private void LoadConfig()
        {
            try
            {
                // Try output directory first (copied by build), then project source
                string[] candidates =
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "serviceAccount.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets", "serviceAccount.json"),
                };

                foreach (string candidate in candidates)
                {
                    if (!File.Exists(candidate)) continue;
                    string json = File.ReadAllText(candidate);
                    _adminConfig = JsonConvert.DeserializeObject<AdminSdkConfig>(json);
                    _projectId   = _adminConfig.ProjectId;
                    System.Diagnostics.Debug.WriteLine($"[Firebase] Loaded config for project: {_projectId}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[Firebase] serviceAccount.json not found — Firestore calls will fail.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firebase] Config load failed: {ex.Message}");
            }
        }

        // Set the web API key so LoginForm can call SignInAsync.
        // Grab this from Firebase Console → Project Settings → General → Web API key.
        public void SetWebApiKey(string key) => _webApiKey = key;

        // Stores the user's ID token after email/password sign-in (for admin check only)
        public void SetAuthToken(string idToken) => _idToken = idToken;

        public bool IsAuthenticated => !string.IsNullOrEmpty(_serviceToken) || !string.IsNullOrEmpty(_idToken);

        // The client_email from the service account — used as a trusted admin identity fallback
        public string AdminEmail => _adminConfig?.ClientEmail ?? string.Empty;

        // Legacy accessor kept for LoginForm compatibility
        public string ApiKey => _webApiKey ?? string.Empty;

        // ─── Service Account JWT / Token ───────────────────────────────────────

        // Returns a valid OAuth2 access token for the service account.
        // Automatically mints a new one when the current one is about to expire.
        private async Task<string> GetServiceTokenAsync()
        {
            // Reuse if still valid (with a 60-second buffer)
            if (!string.IsNullOrEmpty(_serviceToken) && DateTime.UtcNow < _serviceTokenExpiry.AddSeconds(-60))
                return _serviceToken;

            if (_adminConfig == null)
                throw new InvalidOperationException("serviceAccount.json is missing or invalid. " +
                    "Copy it to Assets\\serviceAccount.json and rebuild.");

            string jwt = BuildServiceAccountJwt();
            string token = await ExchangeJwtForAccessTokenAsync(jwt);
            _serviceToken = token;
            _serviceTokenExpiry = DateTime.UtcNow.AddSeconds(3600);
            return token;
        }

        // Builds a signed JWT asserting the service account's identity.
        // Google's token endpoint accepts this in exchange for an access token.
        private string BuildServiceAccountJwt()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Header
            string headerJson   = JsonConvert.SerializeObject(new { alg = "RS256", typ = "JWT" });
            string headerBase64  = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));

            // Claim set
            var claims = new
            {
                iss   = _adminConfig.ClientEmail,
                scope = "https://www.googleapis.com/auth/datastore https://www.googleapis.com/auth/firebase",
                aud   = "https://oauth2.googleapis.com/token",
                iat   = now,
                exp   = now + 3600
            };
            string claimsJson   = JsonConvert.SerializeObject(claims);
            string claimsBase64  = Base64UrlEncode(Encoding.UTF8.GetBytes(claimsJson));

            string unsignedJwt  = $"{headerBase64}.{claimsBase64}";

            // Sign with the private key from the Admin SDK JSON
            byte[] signature = SignWithRsa(Encoding.UTF8.GetBytes(unsignedJwt), _adminConfig.PrivateKey);
            string sigBase64  = Base64UrlEncode(signature);

            return $"{unsignedJwt}.{sigBase64}";
        }

        // Signs data using the RSA private key from the service account JSON.
        // The key is in PEM PKCS#8 format — we parse it manually for .NET 4.7.2 compat.
        private static byte[] SignWithRsa(byte[] data, string pemKey)
        {
            // Strip PEM headers and decode the base64 DER content
            string base64 = pemKey
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----",   "")
                .Replace("-----BEGIN PRIVATE KEY-----",     "")
                .Replace("-----END PRIVATE KEY-----",       "")
                .Replace("\n", "").Replace("\r", "").Trim();

            byte[] keyBytes = Convert.FromBase64String(base64);

            // Decode the PKCS#8 wrapper to get the inner PKCS#1 RSA key bytes
            byte[] pkcs1 = StripPkcs8Header(keyBytes);

            // Import the raw PKCS#1 parameters into RSA
            RSAParameters rsaParams = DecodePkcs1RsaPrivateKey(pkcs1);

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParams);
                // SHA256 with PKCS#1 v1.5 padding — what Google requires for JWT signing
                return rsa.SignData(data, "SHA256");
            }
        }

        // Strips the PKCS#8 wrapper from a private key DER blob,
        // returning the inner PKCS#1 RSAPrivateKey bytes.
        // PKCS#8 structure: SEQUENCE { SEQUENCE(AlgId), OCTET STRING { RSAPrivateKey } }
        private static byte[] StripPkcs8Header(byte[] pkcs8)
        {
            int pos = 0;
            // outer SEQUENCE
            if (pkcs8[pos++] != 0x30) throw new Exception("Expected SEQUENCE at offset 0");
            SkipDerLength(pkcs8, ref pos);
            
            // version INTEGER
            if (pkcs8[pos++] != 0x02) throw new Exception("Expected INTEGER (version)");
            int vLen = ReadDerLength(pkcs8, ref pos);
            pos += vLen;

            // AlgorithmIdentifier SEQUENCE
            if (pkcs8[pos++] != 0x30) throw new Exception("Expected SEQUENCE (AlgorithmIdentifier)");
            int algLen = ReadDerLength(pkcs8, ref pos);
            pos += algLen;
            // OCTET STRING wrapping the RSAPrivateKey
            if (pkcs8[pos++] != 0x04) throw new Exception("Expected OCTET STRING");
            SkipDerLength(pkcs8, ref pos);
            // Remaining bytes are the PKCS#1 RSAPrivateKey
            int remaining = pkcs8.Length - pos;
            byte[] result = new byte[remaining];
            Array.Copy(pkcs8, pos, result, 0, remaining);
            return result;
        }

        private static void SkipDerLength(byte[] data, ref int pos)
        {
            byte b = data[pos++];
            if (b >= 0x80)
            {
                int nb = b & 0x7F;
                pos += nb;
            }
        }

        // Reads and returns a DER-encoded length value, advancing pos past it
        private static int ReadDerLength(byte[] data, ref int pos)
        {
            byte first = data[pos++];
            if (first < 0x80) return first;
            int numBytes = first & 0x7F;
            int length   = 0;
            for (int i = 0; i < numBytes; i++)
                length = (length << 8) | data[pos++];
            return length;
        }

        // Decodes a PKCS#1 RSAPrivateKey DER blob into RSAParameters.
        // Structure: SEQUENCE { version, n, e, d, p, q, dp, dq, qinv }
        private static RSAParameters DecodePkcs1RsaPrivateKey(byte[] pkcs1)
        {
            int pos = 0;
            // SEQUENCE wrapper
            if (pkcs1[pos++] != 0x30) throw new Exception("Expected SEQUENCE in PKCS#1");
            ReadDerLength(pkcs1, ref pos);
            // version INTEGER (must be 0)
            if (pkcs1[pos++] != 0x02) throw new Exception("Expected INTEGER (version)");
            int vLen = ReadDerLength(pkcs1, ref pos);
            pos += vLen;

            // The remaining fields in order per RFC 3447
            return new RSAParameters
            {
                Modulus  = ReadDerIntegerClean(pkcs1, ref pos),  // n
                Exponent = ReadDerIntegerClean(pkcs1, ref pos),  // e (public exponent)
                D        = ReadDerIntegerClean(pkcs1, ref pos),  // d (private exponent)
                P        = ReadDerIntegerClean(pkcs1, ref pos),  // p
                Q        = ReadDerIntegerClean(pkcs1, ref pos),  // q
                DP       = ReadDerIntegerClean(pkcs1, ref pos),  // d mod (p-1)
                DQ       = ReadDerIntegerClean(pkcs1, ref pos),  // d mod (q-1)
                InverseQ = ReadDerIntegerClean(pkcs1, ref pos),  // q^-1 mod p
            };
        }

        // Reads a DER INTEGER, stripping the leading 0x00 sign byte that DER
        // adds when the MSB of the value would otherwise be set (two's complement).
        private static byte[] ReadDerIntegerClean(byte[] data, ref int pos)
        {
            if (data[pos++] != 0x02) throw new Exception("Expected INTEGER tag");
            int   len      = ReadDerLength(data, ref pos);
            int   start    = pos;
            bool  hasZero  = (len > 1 && data[pos] == 0x00);
            if (hasZero) { start++; len--; }
            byte[] result  = new byte[len];
            Array.Copy(data, start, result, 0, len);
            pos = start + len;
            return result;
        }

        // Exchanges the signed JWT for a short-lived Google OAuth2 access token
        private async Task<string> ExchangeJwtForAccessTokenAsync(string jwt)
        {
            string url   = "https://oauth2.googleapis.com/token";
            string body  = $"grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Ajwt-bearer&assertion={jwt}";
            var response = await _http.PostAsync(url,
                new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded"));
            string raw   = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Service account token exchange failed: {raw}");

            var obj = JObject.Parse(raw);
            return obj["access_token"]?.ToString()
                ?? throw new Exception("No access_token in response");
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        // ─── Generic CRUD ──────────────────────────────────────────────────────

        // Fetches all documents in a collection and deserializes them into a list of T.
        // This will return all docs — no pagination yet, should be fine for our scale.
        public async Task<List<T>> GetAllAsync<T>(string collection) where T : class, new()
        {
            try
            {
                string url     = $"{BaseUrl}/{collection}";
                var    request = await BuildRequestAsync(HttpMethod.Get, url);
                var    response = await _http.SendAsync(request);
                string body    = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Firestore error {response.StatusCode}: {body}");

                return ParseDocumentList<T>(body, collection);
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("firebase_error.txt", $"GetAllAsync<{typeof(T).Name}> failed: {ex.ToString()}\n");
                return new List<T>();
            }
        }

        // Fetches a single document by ID
        public async Task<T> GetByIdAsync<T>(string collection, string docId) where T : class, new()
        {
            try
            {
                string url     = $"{BaseUrl}/{collection}/{docId}";
                var    request = await BuildRequestAsync(HttpMethod.Get, url);
                var    response = await _http.SendAsync(request);
                string body    = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return null;

                var docJson = JObject.Parse(body);
                return ConvertDocument<T>(docJson, collection);
            }
            catch
            {
                return null;
            }
        }

        // Creates or fully replaces a document.
        // PATCH with no updateMask acts as a full merge/upsert in Firestore REST.
        public async Task SaveAsync<T>(string collection, string docId, T data)
        {
            string url          = $"{BaseUrl}/{collection}/{docId}";
            string firestoreJson = ConvertToFirestoreJson(data);

            var request = await BuildRequestAsync(new HttpMethod("PATCH"), url);
            request.Content = new StringContent(firestoreJson, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Save failed {response.StatusCode}: {body}");
            }
        }

        // Deletes a document — no going back!
        public async Task DeleteAsync(string collection, string docId)
        {
            string url      = $"{BaseUrl}/{collection}/{docId}";
            var    request  = await BuildRequestAsync(HttpMethod.Delete, url);
            var    response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Delete failed {response.StatusCode}: {body}");
            }
        }

        // ─── Authentication ────────────────────────────────────────────────────

        // Signs in with email/password using the Firebase Auth REST API.
        // We still need this to verify the admin flag in Firestore.
        // The web API key must be set via SetWebApiKey() before calling this.
        public async Task<AuthResponse> SignInAsync(string email, string password)
        {
            if (string.IsNullOrEmpty(_webApiKey))
                throw new InvalidOperationException(
                    "Web API key not set. Call FirebaseService.Instance.SetWebApiKey(\"YOUR_WEB_API_KEY\") " +
                    "before signing in, or hardcode it in FirebaseService.");

            string url     = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_webApiKey}";
            var    payload = new { email, password, returnSecureToken = true };
            string json    = JsonConvert.SerializeObject(payload);

            var response = await _http.PostAsync(url,
                new StringContent(json, Encoding.UTF8, "application/json"));
            string body  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(ParseAuthError(body));

            var result = JsonConvert.DeserializeObject<AuthResponse>(body);
            _idToken = result.IdToken;
            return result;
        }

        // Clears both the user token and service token — called on logout
        public void SignOut()
        {
            _idToken      = null;
            _serviceToken = null;
        }

        // ─── Request Builder ───────────────────────────────────────────────────

        // Builds a request using the service account token (for all Firestore CRUD).
        // Falls back to the user ID token when no service account config is available.
        private async Task<HttpRequestMessage> BuildRequestAsync(HttpMethod method, string url)
        {
            string token;
            if (_adminConfig != null)
            {
                token = await GetServiceTokenAsync();
            }
            else if (!string.IsNullOrEmpty(_idToken))
            {
                token = _idToken;
            }
            else
            {
                throw new InvalidOperationException(
                    "No authentication token available. Either provide a service account JSON " +
                    "in Assets/serviceAccount.json, or sign in first.");
            }

            var req = new HttpRequestMessage(method, url);
            req.Headers.Add("Authorization", $"Bearer {token}");
            return req;
        }

        // Parses the "documents" array from a Firestore list response
        private List<T> ParseDocumentList<T>(string responseBody, string collection) where T : class, new()
        {
            var result = new List<T>();
            var root = JObject.Parse(responseBody);
            var docs = root["documents"] as JArray;

            if (docs == null) return result;

            foreach (var doc in docs)
            {
                var converted = ConvertDocument<T>(doc as JObject, collection);
                if (converted != null)
                    result.Add(converted);
            }

            return result;
        }

        // Converts a Firestore REST document (with typed field wrappers) into our POCO
        private T ConvertDocument<T>(JObject doc, string collection) where T : class, new()
        {
            if (doc == null) return null;

            // Extract the document ID from the "name" path
            string name = doc["name"]?.ToString() ?? "";
            string docId = name.Contains("/") ? name.Substring(name.LastIndexOf('/') + 1) : name;

            // Flatten the Firestore typed fields into a plain key→value dictionary
            var fields = doc["fields"] as JObject;
            var flat = new Dictionary<string, object>();
            flat["Id"] = docId;  // always inject the document ID

            if (fields != null)
            {
                foreach (var field in fields)
                {
                    flat[field.Key] = ExtractFieldValue(field.Value as JObject);
                }
            }

            // Use Newtonsoft to map the flat dict onto our POCO
            string flatJson = JsonConvert.SerializeObject(flat);
            return JsonConvert.DeserializeObject<T>(flatJson);
        }

        // Extracts the actual value from a Firestore typed value wrapper like
        // { "stringValue": "hello" } or { "integerValue": "42" }
        private object ExtractFieldValue(JObject fieldObj)
        {
            if (fieldObj == null) return null;

            if (fieldObj["stringValue"] != null)  return fieldObj["stringValue"].ToString();
            if (fieldObj["integerValue"] != null) return Convert.ToInt64(fieldObj["integerValue"].ToString());
            if (fieldObj["doubleValue"] != null)  return fieldObj["doubleValue"].ToObject<double>();
            if (fieldObj["booleanValue"] != null) return fieldObj["booleanValue"].ToObject<bool>();
            if (fieldObj["timestampValue"] != null) return fieldObj["timestampValue"].ToObject<DateTime>();
            if (fieldObj["nullValue"] != null)    return null;

            // Array fields — recurse into values
            if (fieldObj["arrayValue"] != null)
            {
                var values = fieldObj["arrayValue"]["values"] as JArray;
                if (values == null) return new List<object>();
                var list = new List<object>();
                foreach (var v in values)
                    list.Add(ExtractFieldValue(v as JObject));
                return list;
            }

            // Map fields — recurse into fields
            if (fieldObj["mapValue"] != null)
            {
                var mapFields = fieldObj["mapValue"]["fields"] as JObject;
                if (mapFields == null) return new Dictionary<string, object>();
                var dict = new Dictionary<string, object>();
                foreach (var f in mapFields)
                    dict[f.Key] = ExtractFieldValue(f.Value as JObject);
                return dict;
            }

            return null;
        }

        // Converts our POCO into the Firestore REST JSON format with typed field wrappers
        private string ConvertToFirestoreJson(object data)
        {
            // Serialize to flat JSON first, then wrap each value in Firestore format
            var flat = JObject.FromObject(data);
            var fields = new JObject();

            foreach (var prop in flat.Properties())
            {
                // Skip the Id field — that's the document key, not a field
                if (prop.Name == "Id") continue;

                fields[prop.Name] = WrapFieldValue(prop.Value);
            }

            return new JObject { ["fields"] = fields }.ToString();
        }

        // Wraps a plain JSON value in the Firestore typed value wrapper format
        private JObject WrapFieldValue(JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.String:
                    return new JObject { ["stringValue"] = value.ToString() };
                case JTokenType.Integer:
                    return new JObject { ["integerValue"] = value.ToString() };
                case JTokenType.Float:
                    return new JObject { ["doubleValue"] = value.ToObject<double>() };
                case JTokenType.Boolean:
                    return new JObject { ["booleanValue"] = value.ToObject<bool>() };
                case JTokenType.Null:
                    return new JObject { ["nullValue"] = JValue.CreateNull() };
                case JTokenType.Date:
                    return new JObject { ["timestampValue"] = value.ToObject<DateTime>().ToString("o") };
                case JTokenType.Array:
                    var arr = new JArray();
                    foreach (var item in (JArray)value)
                        arr.Add(WrapFieldValue(item));
                    return new JObject { ["arrayValue"] = new JObject { ["values"] = arr } };
                default:
                    // Fall back to stringValue for anything weird
                    return new JObject { ["stringValue"] = value?.ToString() ?? "" };
            }
        }

        // Pulls a readable error message out of a Firebase Auth error response
        private string ParseAuthError(string body)
        {
            try
            {
                var obj = JObject.Parse(body);
                string code = obj["error"]?["message"]?.ToString() ?? "Unknown error";
                // Make the error messages a bit more human-friendly
                switch (code)
                {
                    case "EMAIL_NOT_FOUND":          return "No account found with that email.";
                    case "INVALID_PASSWORD":         return "Incorrect password.";
                    case "USER_DISABLED":            return "This account has been disabled.";
                    case "TOO_MANY_ATTEMPTS_TRY_LATER": return "Too many failed attempts. Please try again later.";
                    default:                         return $"Login failed: {code}";
                }
            }
            catch
            {
                return "Login failed — please check your credentials.";
            }
        }
    }

    // ─── Supporting types ──────────────────────────────────────────────────────

    // Maps to the Firebase Admin SDK service account JSON format.
    // This is the file you download from Firebase Console → Project Settings → Service Accounts.
    public class AdminSdkConfig
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("project_id")]
        public string ProjectId { get; set; }

        [JsonProperty("private_key_id")]
        public string PrivateKeyId { get; set; }

        // The RSA private key in PEM format — used to sign JWTs
        [JsonProperty("private_key")]
        public string PrivateKey { get; set; }

        // The service account email — used as the JWT issuer
        [JsonProperty("client_email")]
        public string ClientEmail { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("auth_uri")]
        public string AuthUri { get; set; }

        [JsonProperty("token_uri")]
        public string TokenUri { get; set; }
    }

    // Kept for backward compat — wraps the web config shape if you have a separate firebase config
    public class FirebaseConfig
    {
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }

        [JsonProperty("projectId")]
        public string ProjectId { get; set; }

        [JsonProperty("authDomain")]
        public string AuthDomain { get; set; }
    }

    // Response from the Firebase Auth signInWithPassword endpoint
    public class AuthResponse
    {
        [JsonProperty("idToken")]
        public string IdToken { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("localId")]
        public string LocalId { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("expiresIn")]
        public string ExpiresIn { get; set; }
    }
}
