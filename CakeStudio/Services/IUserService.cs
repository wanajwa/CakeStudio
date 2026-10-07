using CakeStudio.Models;

namespace CakeStudio.Services
{
    public interface IUserService
    {
        Response Login(string email, string password);
    }
}