using System;

namespace Botanika_Desktop.Firebase.Models
{
    // Represents a customer who has placed at least one order on the website.
    // TotalSpent and OrderCount are computed fields we maintain ourselves.
    public class Client
    {
        // Firestore document ID
        public string Id { get; set; }

        // Full name — used everywhere in the UI
        public string Name { get; set; }

        // Primary contact email
        public string Email { get; set; }

        // Phone number — optional, many customers don't provide one
        public string Phone { get; set; }

        // Shipping / billing address
        public string Address { get; set; }

        // Internal admin notes — not visible to the customer
        public string Notes { get; set; }
        
        // Profile picture URL or Base64 (used mostly for admin profile)
        public string ProfilePicture { get; set; }
        
        // User role, e.g. "admin" or "customer"
        public string Role { get; set; }

        // When did this customer first register / place an order
        public DateTime CreatedAt { get; set; }

        // Running total of all their orders — we update this after each order
        public double TotalSpent { get; set; }

        // Total number of orders placed — shown in the client list
        public int OrderCount { get; set; }

        // Nicely formatted for the "Since" column in the list view
        public string MemberSince => CreatedAt.ToString("MMM yyyy");

        // Formatted currency for the list view Total Spent column
        public string TotalSpentDisplay => $"${TotalSpent:F2}";
    }
}
