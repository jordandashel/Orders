namespace OrderEntryMockingPractice.Models
{
    public class OrderItem
    {
        public virtual Product Product { get; set; }
        public decimal Quantity { get; set; }
    }
}
