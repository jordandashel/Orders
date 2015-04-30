using System;
using System.Linq;
using OrderEntryMockingPractice.Models;

namespace OrderEntryMockingPractice.Services
{
    public class OrderService
    {
        private IProductRepository pr;
        private IOrderFulfillmentService ofs;
        private ICustomerRepository cr;
        private ITaxRateService trs;
        private IEmailService es;

        public OrderService(IProductRepository productRepository, 
            IOrderFulfillmentService orderFulfillmentService,
            ICustomerRepository customerRepository,
            ITaxRateService taxRateService,
            IEmailService emailService)
        {
            this.pr = productRepository;
            this.ofs = orderFulfillmentService;
            this.cr = customerRepository;
            this.trs = taxRateService;
            this.es = emailService;
        }

        public OrderSummary PlaceOrder(Order order)
        {
            if (order.OrderItems
                .GroupBy(n => n)
                .Any(c => c.Count() > 1))
            {
                throw new InvalidOperationException("Duplicate Items");
            }
            foreach (var orderItem in order.OrderItems)
            {
                if (!pr.IsInStock(orderItem.Product.Sku))
                {
                    throw new InvalidOperationException("item is out of stock");
                }
            }

            OrderSummary orderSummary = new OrderSummary();

            OrderConfirmation confirmation = ofs.Fulfill(order);

            orderSummary.OrderNumber = confirmation.OrderNumber;
            orderSummary.OrderId = confirmation.OrderId;

            int customerId = order.CustomerId;
            Customer customer = cr.Get(customerId);

            var zipCode = customer.PostalCode;
            var country = customer.Country;

            var taxes = trs.GetTaxEntries(zipCode, country);

            orderSummary.Taxes = taxes;

            foreach (OrderItem item in order.OrderItems)
            {
                orderSummary.NetTotal += item.Product.Price;
            }

            es.SendOrderConfirmationEmail(customerId, confirmation.OrderId);

            return orderSummary;
        }
    }
}
