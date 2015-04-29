using System.Collections.Generic;

namespace OrderEntryMockingPractice.Models
{
    public class Order
    {
        public Order(List<OrderItem> items)
        {
            this.OrderItems = items;
        }
        
        public int CustomerId { get; set; }
        public List<OrderItem> OrderItems { get; set; }
    }
}
