using Mango.Services.PaymentAPI.Messages;
using Mango.Services.PaymentAPI.RabbitMQSender;
using Newtonsoft.Json;
using PaymentProcessor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.PaymentAPI.Messaging
{
    public class RabbitMQPaymentConsumer : BackgroundService
    {
        private readonly string _orderPaymentProcessTopic;

        private readonly IConfiguration _configuration;
        private IConnection _connection;
        private IModel _channel;

        private readonly IProcessPayment _processPayment;
        private readonly IRabbitMQPaymentMessageSender _rabbitMQPaymentMessageSender;
        public RabbitMQPaymentConsumer(
            IConfiguration configuration, 
            IProcessPayment processPayment,
            IRabbitMQPaymentMessageSender rabbitMQPaymentMessageSender)
        {
            _configuration = configuration;
            _rabbitMQPaymentMessageSender = rabbitMQPaymentMessageSender;
            _orderPaymentProcessTopic = _configuration["RabbitMQ:OrderPaymentProcessTopic"];
            _processPayment = processPayment;

            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(
                queue: _orderPaymentProcessTopic,
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

                PaymentRequestMessage checkoutHeaderDto = JsonConvert.DeserializeObject<PaymentRequestMessage>(content);
                HandleMessage(checkoutHeaderDto).GetAwaiter().GetResult();

                _channel.BasicAck(ev.DeliveryTag, false);
            };
            _channel.BasicConsume(_orderPaymentProcessTopic, false, consumer);

            return Task.CompletedTask;
        }

        private async Task HandleMessage(PaymentRequestMessage paymentRequestMessage)
        {
            var result = _processPayment.PaymentProcessor();

            UpdatePaymentResultMessage updatePaymentResultMessage = new UpdatePaymentResultMessage()
            {
                Status = result,
                OrderId = paymentRequestMessage.OrderId,
                Email = paymentRequestMessage.Email
            };

            try
            {
                _rabbitMQPaymentMessageSender.SendMessage(updatePaymentResultMessage);
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
