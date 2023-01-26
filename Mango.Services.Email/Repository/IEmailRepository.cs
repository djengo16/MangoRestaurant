using Mango.Services.Email.Messages;
using Mango.Services.Email.Models;

namespace Mango.Services.Email.Repository
{
    public interface IEmailRepository
    {
        Task SendAndLogEmail(UpdatePaymentResultMessage message);
    }
}
