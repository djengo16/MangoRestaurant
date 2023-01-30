using Mango.MessageBus;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Mango.Services.PaymentAPI.RabbitMQSender
{
    public class RabbitMQPaymentMessageSender : IRabbitMQPaymentMessageSender
    {
        private readonly string _hostname;
        private readonly string _password;
        private readonly string _username;
        private IConnection _connection;
        private const string ExchangeName = "PublishSubscribePaymentUpdate_Exchange";

        public RabbitMQPaymentMessageSender()
        {
            _hostname = "localhost";
            _password = "guest";
            _username = "guest";
        }

        public void SendMessage(BaseMessage message)
        {
            if (!ConnectionExists())
            {
                CreateConnection();
            }

            using var channel = _connection.CreateModel();
            channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: false);
            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);
            channel.BasicPublish(exchange: ExchangeName, "", basicProperties: null, body: body);
        }

        private void CreateConnection()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _hostname,
                    Password = _password,
                    UserName = _username
                };

                _connection = factory.CreateConnection();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private bool ConnectionExists() => _connection != null;
    }
}
