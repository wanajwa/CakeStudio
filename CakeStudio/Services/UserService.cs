using CakeStudio.Data;
using CakeStudio.Models;
using Microsoft.AspNetCore.Identity;

namespace CakeStudio.Services
{
    public class UserService : IUserService
    {
        private readonly NHibernateHelper _nhibernateHelper;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserService(NHibernateHelper nhibernateHelper)
        {
            _nhibernateHelper = nhibernateHelper;
            _passwordHasher = new PasswordHasher<User>();
        }

        public Response Login(string email, string password)
        {
            Response serviceResponse = new Response();

            try
            {
                using var session = _nhibernateHelper.OpenSession();

                var user = session.QueryOver<User>()
                    .Where(x => x.Email == email)
                    .SingleOrDefault();

                if (user == null)
                {
                    serviceResponse.Status = false;
                    serviceResponse.Message = "Invalid email or password.";
                    serviceResponse.MessageCode = "INVALID_LOGIN";
                    serviceResponse.Data = null;

                    return serviceResponse;
                }

                var passwordResult = _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    password
                );

                if (passwordResult == PasswordVerificationResult.Failed)
                {
                    serviceResponse.Status = false;
                    serviceResponse.Message = "Invalid email or password.";
                    serviceResponse.MessageCode = "INVALID_LOGIN";
                    serviceResponse.Data = null;

                    return serviceResponse;
                }

                serviceResponse.Status = true;
                serviceResponse.Message = "Login successful.";
                serviceResponse.MessageCode = "LOGIN_SUCCESS";
                serviceResponse.Data = user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LOGIN ERROR:");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                serviceResponse.Status = false;
                serviceResponse.Message = "An error occurred during login.";
                serviceResponse.MessageCode = ex.Message;
                serviceResponse.Data = null;
            }

            return serviceResponse;
        }
    }
}