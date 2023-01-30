using AutoMapper;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.RabbitMQSender;
using Mango.Services.OrderAPI.Repository;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.OrderAPI.Messaging
{
    public class RabbitMQCheckoutConsumer : BackgroundService
    {
        private readonly string _checkoutQueue;
        private readonly string _orderPaymentProcessTopic;

        private readonly OrderRepository _orderRepository;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private IConnection _connection;
        private IModel _channel;
        private readonly IRabbitMQOrderMessageSender _rabbitMQOrderMessageSender;

        public RabbitMQCheckoutConsumer(
            OrderRepository orderRepository, 
            IConfiguration configuration,
            IMapper mapper,
            IRabbitMQOrderMessageSender rabbitMQOrderMessageSender)
        {
            _orderRepository = orderRepository;
            _configuration = configuration;
            _mapper = mapper;
            _checkoutQueue = _configuration["RabbitMQ:QueueName"];
            _orderPaymentProcessTopic = _configuration["RabbitMQ:OrderPaymentProcessTopic"];
            _rabbitMQOrderMessageSender = rabbitMQOrderMessageSender;

            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(
                queue: _checkoutQueue,
                false, 
                false, 
                false, 
                arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (channel, ev) =>
            {
                var content = Encoding.UTF8.GetString(ev.Body.ToArray());

                CheckoutHeaderDto checkoutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(content);
                HandleMessage(checkoutHeaderDto).GetAwaiter().GetResult();

                _channel.BasicAck(ev.DeliveryTag, false);
            };
            _channel.BasicConsume(_checkoutQueue, false, consumer);

            return Task.CompletedTask;
        }

        private async Task HandleMessage(CheckoutHeaderDto checkoutHeaderDto)
        {
            OrderHeader orderHeader = new()
            {
                UserId = checkoutHeaderDto.UserId,
                FirstName = checkoutHeaderDto.FirstName,
                LastName = checkoutHeaderDto.LastName,
                OrderDetails = new List<OrderDetails>(),
                CardNumber = checkoutHeaderDto.CardNumber,
                CouponCode = checkoutHeaderDto.CouponCode,
                CVV = checkoutHeaderDto.CVV,
                DiscountTotal = checkoutHeaderDto.DiscountTotal,
                Email = checkoutHeaderDto.Email,
                ExpiryMonthYear = checkoutHeaderDto.ExpiryMonthYear,
                OrderTime = DateTime.Now,
                OrderTotal = checkoutHeaderDto.OrderTotal,
                PaymentStatus = false,
                Phone = checkoutHeaderDto.Phone,
                PickupDateTime = checkoutHeaderDto.PickupDateTime
            };

            foreach (var detailList in checkoutHeaderDto.CartDetails)
            {
                OrderDetails orderDetails = new()
                {
                    ProductId = detailList.ProductId,
                    Product = new Product
                    {
                        Name = detailList.Product.Name,
                        Price = detailList.Product.Price
                    },
                    Count = detailList.Count
                };
                orderHeader.CartTotalItems += detailList.Count;
                orderHeader.OrderDetails.Add(orderDetails);
            };

            await _orderRepository.AddOrder(orderHeader);

            var paymentRequestMessage = _mapper.Map<PaymentRequestMessage>(orderHeader);

            try
            {
                _rabbitMQOrderMessageSender.SendMessage(paymentRequestMessage, _orderPaymentProcessTopic);
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
