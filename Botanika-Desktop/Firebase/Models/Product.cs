using System;

namespace Botanika_Desktop.Firebase.Models
{
    // Mirrors the product documents sitting in the Firestore "products" collection.
    // The website stores these same fields — keep field names in sync!
    public class Product
    {
        // Firestore document ID — set by us when creating, e.g. "prod_1234567890"
        public string Id { get; set; }

        // The plant's display name — e.g. "Monstera Deliciosa"
        public string Name { get; set; }

        // Category for filtering — e.g. "Tropical", "Succulents", "Herbs"
        public string Category { get; set; }

        // Sale price in the store currency
        public double Price { get; set; }

        // How many units we currently have — alerts fire when this hits 5 or below
        public int Stock { get; set; }

        // Whether this plant shows up in the Featured section on the homepage
        public bool Featured { get; set; }

        // Product description — shown on the product detail page
        public string Description { get; set; }

        // Image URL or base64 encoded image string
        public string ImageUrl { get; set; }

        // Short care instructions for the plant card tooltip
        public string CareLevel { get; set; }

        // When was this product added to the store
        public DateTime CreatedAt { get; set; }

        // Convenience: display string for the featured column in the list
        public string FeaturedDisplay => Featured ? "✓ Yes" : "No";

        // Convenience: display string for stock status
        public string StockStatus
        {
            get
            {
                if (Stock <= 0)  return "Out of Stock";
                if (Stock <= 5)  return $"Low ({Stock})";
                return Stock.ToString();
            }
        }
    }
}
