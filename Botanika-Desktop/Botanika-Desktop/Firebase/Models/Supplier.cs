namespace Botanika_Desktop.Firebase.Models
{
    // A supplier is a company or person we buy plants/seeds/etc. from.
    // Keeping these in Firestore means the website and desktop always see the same list.
    public class Supplier
    {
        // Firestore document ID
        public string Id { get; set; }

        // Company or business name
        public string Name { get; set; }

        // The specific person we deal with at this company
        public string ContactPerson { get; set; }

        // Business email
        public string Email { get; set; }

        // Business phone
        public string Phone { get; set; }

        // What type of stock they supply — Plants, Flowers, Herbs, Seeds, Mixed
        public string Category { get; set; }

        // Their country of origin — useful for import/customs paperwork
        public string Country { get; set; }

        // Any private notes about this supplier — payment terms, lead times, etc.
        public string Notes { get; set; }

        // Whether we're still actively ordering from them
        public bool Active { get; set; }

        // Display text for the Active column in the list
        public string ActiveDisplay => Active ? "Active" : "Inactive";
    }
}
