namespace Mango.MessageBus
{
    public class BaseMessage
    {
        public string Id { get; set; }
        public DateTime MessageCreated { get; set; }
    }
}
