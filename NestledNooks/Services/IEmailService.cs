using System.Threading.Tasks;

namespace NestledNooks.Services
{
    public interface IEmailService
    {
        Task SendContactEmail(string name, string fromEmail, string message);
    }
}
