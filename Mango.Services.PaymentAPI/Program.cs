using Mango.MessageBus;
using Mango.Services.PaymentAPI.Extensions;
using Mango.Services.PaymentAPI.Messaging;
using PaymentProcessor;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IProcessPayment, ProcessPayment>();
builder.Services.AddSingleton<IAzureServiceBusConsumer, AzureServiceBusConsumer>();
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
builder.Services.AddSingleton<IMessageBus>(x =>
    new AzureServiceBusMessageBus(serviceBusConnectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseAzureServiceBusConsumer();

app.Run();
