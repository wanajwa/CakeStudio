using CakeStudio.Models;
using CakeStudio.ViewModels;

namespace CakeStudio.Services
{
    public interface IUserService
    {
        Response Login(string email, string password);
        Response Register(User user, string password);
        Response GetCustomerProfile(int customerId);
        Response UpdateCustomerProfile(int customerId, CustomerProfileViewModel model);
    }
}