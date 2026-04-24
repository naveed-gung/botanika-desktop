namespace Botanika_Desktop
{
    // Holds the currently logged-in admin's details for the duration of the session.
    // Reset this on logout so nothing leaks between sessions.
    public static class Session
    {
        // The Firebase Auth ID token — attached to every Firestore API call
        public static string IdToken { get; set; }

        // Firebase user UID — used to look up the user doc in Firestore
        public static string UserId { get; set; }

        // Admin's display name — shown in the sidebar header
        public static string DisplayName { get; set; }

        // Admin's email address
        public static string Email { get; set; }

        // Whether the user has been confirmed as an admin in Firestore
        public static bool IsAdmin { get; set; }

        // Whether there's an active login session
        public static bool IsLoggedIn => !string.IsNullOrEmpty(IdToken) && IsAdmin;

        // Wipes everything — called on logout
        public static void Clear()
        {
            IdToken     = null;
            UserId      = null;
            DisplayName = null;
            Email       = null;
            IsAdmin     = false;
        }
    }
}
