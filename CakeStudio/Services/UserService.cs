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

                // Find user by email
                var user = session.QueryOver<User>()
                    .Where(x => x.Email == email)
                    .SingleOrDefault();

                // Check whether user exists
                if (user == null)
                {
                    serviceResponse.Message = "Invalid email or password.";
                    serviceResponse.MessageCode = "INVALID_LOGIN";
                    return serviceResponse;
                }

                // Verify password
                var passwordResult = _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    password
                );

                // Check whether password is correct
                if (passwordResult == PasswordVerificationResult.Failed)
                {
                    serviceResponse.Message = "Invalid email or password.";
                    serviceResponse.MessageCode = "INVALID_LOGIN";
                    return serviceResponse;
                }

                // Login successful
                serviceResponse.Status = true;
                serviceResponse.Data = user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                serviceResponse.Message = "An error occurred during login.";
                serviceResponse.MessageCode = "LOGIN_ERROR";
            }

            return serviceResponse;
        }
    }
}