using System;

namespace Botanika_Desktop.Firebase.Models
{
    // A payment record — can be money we RECEIVED from a customer,
    // or money we OWE TO a supplier.
    // The "Direction" field tells us which direction the money flows.
    public class Payment
    {
        // Firestore document ID
        public string Id { get; set; }

        // "received" = customer paid us | "topay" = we owe a supplier
        public string Direction { get; set; }

        // Who is on the other end of this payment — client or supplier name
        public string Party { get; set; }

        // Their Firestore document ID — so we can link back to the client/supplier record
        public string PartyId { get; set; }

        // How much money is involved
        public double Amount { get; set; }

        // "pending" | "paid" | "overdue"
        public string Status { get; set; }

        // What this payment is for — e.g. "Invoice #42", "Order #ORD-0012"
        public string Description { get; set; }

        // When does this payment need to be settled by
        public DateTime DueDate { get; set; }

        // When was it actually paid — null if still pending
        public DateTime? PaidDate { get; set; }

        // External reference number — bank transfer ref, invoice number, etc.
        public string Reference { get; set; }

        // Formatted amount for display
        public string AmountDisplay => $"${Amount:F2}";

        // Display due date
        public string DueDateDisplay => DueDate.ToString("dd MMM yyyy");

        // Overdue check — payment is late if due date passed and still not paid
        public bool IsOverdue => Status != "paid" && DueDate < DateTime.Today;
    }
}
