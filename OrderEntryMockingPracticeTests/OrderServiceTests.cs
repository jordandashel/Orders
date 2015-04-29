using System;
using System.Collections.Generic;
using FakeItEasy;
using NUnit;
using NUnit.Framework;
using OrderEntryMockingPractice.Models;
using OrderEntryMockingPractice.Services;

namespace OrderEntryMockingPracticeTests
{
    [TestFixture]
    public class OrderServiceTests
    {

        private OrderService orderService;
        private IProductRepository fakeProductRepository;
        private IOrderFulfillmentService fakeOrderFulfillmentService;
        private ICustomerRepository fakeCustomerRepository;
        private ITaxRateService fakeTaxRateService;
        private Order order;
        private OrderItem orderItem;

        [SetUp]
        public void BeforeEach()
        {
            fakeProductRepository = A.Fake<IProductRepository>();
            fakeOrderFulfillmentService = A.Fake<IOrderFulfillmentService>();
            fakeCustomerRepository = A.Fake<ICustomerRepository>();
            fakeTaxRateService = A.Fake<ITaxRateService>();

            orderService = new OrderService(fakeProductRepository,
                fakeOrderFulfillmentService,
                fakeCustomerRepository,
                fakeTaxRateService);

            orderItem = new OrderItem()
            {
                Product = new Product()
                {
                    Sku = "5"
                }
            };

            order = new Order(new List<OrderItem>()
            {
                orderItem,
            }
            ) { CustomerId = 5 };
        }


        public OrderSummary PlaceValidOrder()
        {
            orderService = new OrderService(fakeProductRepository,
                fakeOrderFulfillmentService,
                fakeCustomerRepository,
                fakeTaxRateService);

            A.CallTo(() => fakeProductRepository.IsInStock(A<string>.Ignored)).Returns(true);

            return orderService.PlaceOrder(order);
        }
        
        [Test]
        public void OrderItemsAreUniqeBySku()
        {
            A.CallTo(() => fakeProductRepository.IsInStock(A<string>.Ignored)).Returns(true);

            orderItem = new OrderItem()
            {
                Product = new Product()
                {
                    Sku = "5"
                }
            };

            order = new Order(new List<OrderItem>()
            {
                orderItem,
                orderItem
            });


            Assert.Throws<InvalidOperationException>(() => this.orderService.PlaceOrder(order));
        }


        [Test]
        public void NotAllItemsAreInStock()
        {
            A.CallTo(() => fakeProductRepository.IsInStock(A<string>.Ignored)).Returns(false);

            orderItem = new OrderItem()
            {
                Product = new Product()
                {
                    Sku = "5"
                }
            };

            order = new Order(new List<OrderItem>()
            {
                orderItem,
                orderItem
            });

            Assert.Throws<InvalidOperationException>(() => PlaceValidOrder());
        }

        [Test]
        public void OrderSummaryReturnedFromValidOrder()
        {
            Assert.That(PlaceValidOrder(), Is.InstanceOf(typeof(OrderSummary)));
        }

        [Test]
        public void OrderSummaryIsSubmittedToOrderFulfillmentService()
        {
            fakeOrderFulfillmentService = A.Fake<IOrderFulfillmentService>();
            PlaceValidOrder();
            A.CallTo(() => fakeOrderFulfillmentService.Fulfill(A<Order>.Ignored))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void OrderSummaryContainsConfirmationNumber()
        {
            OrderConfirmation oc = new OrderConfirmation(){OrderNumber = "ORDER NUMBER!!"};
            fakeOrderFulfillmentService = A.Fake<IOrderFulfillmentService>();
            A.CallTo(() => fakeOrderFulfillmentService.Fulfill(A<Order>.Ignored)).Returns(oc);

            OrderSummary orderSummary = PlaceValidOrder();

            Assert.That(orderSummary.OrderNumber, Is.EqualTo("ORDER NUMBER!!"));
        }

        [Test]
        public void OrderSummaryContainsId()
        {
            OrderConfirmation oc = new OrderConfirmation() { OrderId = 10 };
            fakeOrderFulfillmentService = A.Fake<IOrderFulfillmentService>();
            A.CallTo(() => fakeOrderFulfillmentService.Fulfill(A<Order>.Ignored)).Returns(oc);

            OrderSummary orderSummary = PlaceValidOrder();

            Assert.That(orderSummary.OrderId, Is.EqualTo(10));
        }

        [Test]
        public void OrderSummaryContainsApplicableTaxesForCustomer()
        {

            Customer customer = new Customer() 
            { Country = "Wadiya", PostalCode = "12345", CustomerId = 0};

            fakeCustomerRepository = A.Fake<ICustomerRepository>();
            A.CallTo(() => fakeCustomerRepository.Get(A<int>.Ignored)).Returns(customer);

            TaxEntry taxEntry = new TaxEntry() { Rate = .05m };

            IEnumerable<TaxEntry> taxEntries = new TaxEntry[] { taxEntry };

            fakeTaxRateService = A.Fake<ITaxRateService>();
            A.CallTo(() => fakeTaxRateService.GetTaxEntries("12345", "Wadiya")).Returns(taxEntries);

            var taxEnumerator = PlaceValidOrder().Taxes.GetEnumerator();
            taxEnumerator.MoveNext();
            var rate = taxEnumerator.Current.Rate;


            Assert.That(rate, Is.EqualTo(.05m));

        }
    
    }
}
