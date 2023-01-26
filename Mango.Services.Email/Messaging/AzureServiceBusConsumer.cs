using Azure.Messaging.ServiceBus;
using Mango.Services.Email.Messages;
using Mango.Services.Email.Repository;
using Newtonsoft.Json;
using System.Text;

namespace Mango.Services.Email.Messaging
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        //Azure credentials for Service Bus
        private readonly string serviceBusConnectionString;
        private readonly string emailSubscription;
        private readonly string orderUpdatePaymentResultTopic;

        private ServiceBusProcessor orderUpdatePaymentStatusProcessor;
        
        private readonly EmailRepository _emailRepository;
        private readonly IConfiguration _configuration;

        public AzureServiceBusConsumer(
            EmailRepository emailRepository, 
            IConfiguration configuration)
        {
            _emailRepository = emailRepository;
            _configuration = configuration;

            serviceBusConnectionString = _configuration["ServiceBus:ConnectionString"];
            emailSubscription = _configuration["ServiceBus:EmailSubscrition"];
            orderUpdatePaymentResultTopic = _configuration["ServiceBus:OrderUpdatePaymentResultTopic"];

            var client = new ServiceBusClient(serviceBusConnectionString);

            orderUpdatePaymentStatusProcessor = client.CreateProcessor(orderUpdatePaymentResultTopic, emailSubscription);
        }

        public async Task Start()
        {
            orderUpdatePaymentStatusProcessor.ProcessMessageAsync += OnOrderPaymentUpdateReceived;
            orderUpdatePaymentStatusProcessor.ProcessErrorAsync += ErrorHandler;

            await orderUpdatePaymentStatusProcessor.StartProcessingAsync();
        }

        public async Task Stop()
        {
            await orderUpdatePaymentStatusProcessor.StopProcessingAsync();
            await orderUpdatePaymentStatusProcessor.DisposeAsync();
        }

        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        private async Task OnOrderPaymentUpdateReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            UpdatePaymentResultMessage paymentResultMessage = JsonConvert.DeserializeObject<UpdatePaymentResultMessage>(body);

            try
            {
                await _emailRepository.SendAndLogEmail(paymentResultMessage);
                await args.CompleteMessageAsync(args.Message);
            }
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}
