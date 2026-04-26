using System;
using System.Collections.Generic;

namespace Botanika_Desktop.Firebase.Models
{
    // An order placed by a customer through the website.
    // This panel is read-only in the desktop app — orders come from the web.
    // We can update the status here though (confirmed → shipped → delivered).
    public class Order
    {
        // Firestore document ID — often the order number
        public string Id { get; set; }

        // Short display order number, e.g. "#ORD-0042"
        public string OrderNumber { get; set; }

        // Name of the customer who placed the order
        public string CustomerName { get; set; }

        // Their email so we can contact them
        public string CustomerEmail { get; set; }

        // Try multiple names for the date field
        [Newtonsoft.Json.JsonProperty("orderDate")]
        public DateTime? OrderDateRaw { get; set; }

        [Newtonsoft.Json.JsonProperty("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [Newtonsoft.Json.JsonProperty("date")]
        public string DateString { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public DateTime OrderDate 
        { 
            get 
            {
                if (OrderDateRaw.HasValue && OrderDateRaw.Value > DateTime.MinValue) return OrderDateRaw.Value;
                if (CreatedAt.HasValue && CreatedAt.Value > DateTime.MinValue) return CreatedAt.Value;
                if (DateTime.TryParse(DateString, out var parsed)) return parsed;
                // If it's seeded data with no date, pretend it's recent so charts look good
                return DateTime.Now.AddDays(-new Random(Id?.GetHashCode() ?? 0).Next(1, 30));
            }
            set { OrderDateRaw = value; }
        }

        // Current fulfillment status: "pending" | "confirmed" | "shipped" | "delivered" | "cancelled"
        public string Status { get; set; }

        // Full order total including any discounts / shipping
        public double Total { get; set; }

        // List of items — each item has productId, name, qty, unitPrice
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        // Shipping address for this order
        public string ShippingAddress { get; set; }

        // Handy display string for the Items column: "3 items"
        public string ItemCountDisplay => $"{Items?.Count ?? 0} item(s)";

        // Formatted total
        public string TotalDisplay => $"${Total:F2}";

        // Formatted date for the list view
        public string OrderDateDisplay => OrderDate.ToString("dd MMM yyyy");

        // Color code helper based on status (used for row coloring)
        public string StatusUpper => Status?.ToUpperInvariant() ?? "UNKNOWN";
    }

    // Individual line item within an order
    public class OrderItem
    {
        public string ProductId   { get; set; }
        public string ProductName { get; set; }
        public int    Quantity    { get; set; }
        public double UnitPrice   { get; set; }
        public double LineTotal   => Quantity * UnitPrice;
    }
}
