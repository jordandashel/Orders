using System;
using System.Linq;
using OrderEntryMockingPractice.Models;

namespace OrderEntryMockingPractice.Services
{
    public class OrderService
    {
        private IProductRepository productRepository;
        private IOrderFulfillmentService orderFulfillmentService;
        private ICustomerRepository customerRepository;
        private ITaxRateService taxRateService;
        private IEmailService emailService;

        public OrderService(IProductRepository productRepository, 
            IOrderFulfillmentService orderFulfillmentService,
            ICustomerRepository customerRepository,
            ITaxRateService taxRateService,
            IEmailService emailService)
        {
            this.productRepository = productRepository;
            this.orderFulfillmentService = orderFulfillmentService;
            this.customerRepository = customerRepository;
            this.taxRateService = taxRateService;
            this.emailService = emailService;
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
                if (!productRepository.IsInStock(orderItem.Product.Sku))
                {
                    throw new InvalidOperationException("item is out of stock");
                }
            }

            OrderSummary orderSummary = new OrderSummary();

            OrderConfirmation confirmation = orderFulfillmentService.Fulfill(order);

            orderSummary.OrderNumber = confirmation.OrderNumber;
            orderSummary.OrderId = confirmation.OrderId;

            int customerId = order.CustomerId;
            Customer customer = customerRepository.Get(customerId);

            var zipCode = customer.PostalCode;
            var country = customer.Country;

            var taxes = taxRateService.GetTaxEntries(zipCode, country);

            orderSummary.Taxes = taxes;

            foreach (OrderItem item in order.OrderItems)
            {
                orderSummary.NetTotal += item.Product.Price;
            }

            emailService.SendOrderConfirmationEmail(customerId, confirmation.OrderId);

            return orderSummary;
        }
    }
}
