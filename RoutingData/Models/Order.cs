using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    //comment for change
    public class Order
    {
        public static readonly String[] ORDER_STATUSES = { "PLANNED", "ON-ROUTE", "DELIVERED", "CANCELLED", "ASSIGNED", "ISSUE" };

        [Key]
        public int Id { get; set; }
        public DateTime DateOrdered { get; set; }
        public string OrderNotes { get; set; }
        public int CustomerId { get; set; }
        public int LocationId { get; set; }
        public int DeliveryRouteId { get; set; }
        public int PositionNumber { get; set; }
        private string? status = "PLANNED"; // All new orders must start from planned
        public DateTime DeliveryDate {  get; set; } = DateTime.Today;
        public Boolean Delayed { get; set; }

        public string? Status
        {
            get => status;
            private set
            {
                // Validate that status exists in array AND state change is valid
                if (ORDER_STATUSES.Contains(status) && IsValidStateChange(value))
                {
                    status = value;
                }
                else
                {
                    throw new ArgumentException($"Invalid state change. '{status}' to '{value}'");
                }
            }
        }
        // Method to change the status
        public void ChangeStatus(string newStatus)
        {
            Status = newStatus;
        }

        private bool IsValidStateChange(string value)
        {
            // Handle reflexive case
            if (status == value)
            {
                return true;
            }
            // Current state to valid states
            return (status == null && value == "PLANNED" ||
            status == "PLANNED" && (value == "ASSIGNED" || value == "CANCELLED") ||
            status == "ASSIGNED" && (value == "ON-ROUTE" /*|| value == "CANCELLED"*/) ||
            status == "ON-ROUTE" && (value == "DELIVERED" || value == "ISSUE" /*|| value == "CANCELLED"*/) ||
            status == "ISSUE" && (value == "CANCELLED" || value == "PLANNED"));
        }

    }
}
