using System;
using System.Linq;
using OrderEntryMockingPractice.Models;
using System.Collections.Generic;

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
            validateOrder(order);

            OrderConfirmation confirmation = orderFulfillmentService.Fulfill(order);

            OrderSummary orderSummary = new OrderSummary();

            orderSummary.OrderNumber = confirmation.OrderNumber;
            orderSummary.OrderId = confirmation.OrderId;
            
            Customer customer = customerRepository.Get(order.CustomerId);

            orderSummary.Taxes = getTaxes(customer);

            orderSummary.NetTotal = netTotal(order);

            emailService.SendOrderConfirmationEmail(order.CustomerId, confirmation.OrderId);

            return orderSummary;
        }

        private void validateOrder(Order order)
        {
            if (hasDuplicates(order))
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
        }

        private IEnumerable<TaxEntry> getTaxes(Customer customer)
        {
            var zipCode = customer.PostalCode;
            var country = customer.Country;
            return taxRateService.GetTaxEntries(zipCode, country);
        }

        private static decimal netTotal(Order order)
        {
            decimal netTotal = 0;

            foreach (OrderItem item in order.OrderItems)
            {
                netTotal += item.Product.Price;
            }

            return netTotal;
        }

        private static bool hasDuplicates(Order order)
        {
            return order.OrderItems
                            .GroupBy(n => n)
                            .Any(c => c.Count() > 1);
        }
    }
}
