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
            }

            return serviceResponse;
        }

        public Response Register(User user, string password)
        {
            Response serviceResponse = new Response();

            try
            {
                // Validate required registration information
                if (string.IsNullOrWhiteSpace(user.Name) ||
                    string.IsNullOrWhiteSpace(user.Email) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    serviceResponse.Message = "Please complete all required fields.";
                    return serviceResponse;
                }

                using var session = _nhibernateHelper.OpenSession();

                // Check if email already exists
                var existingUser = session.QueryOver<User>()
                    .Where(x => x.Email == user.Email)
                    .SingleOrDefault();

                if (existingUser != null)
                {
                    serviceResponse.Message = "Email is already registered.";
                    return serviceResponse;
                }

                // Hash the password
                user.PasswordHash = _passwordHasher.HashPassword(user, password);

                if (user is not Customer)
                {
                    serviceResponse.Message = "Invalid registration type.";
                    return serviceResponse;
                }

                // Begin transaction before saving
                using var transaction = session.BeginTransaction();

                // Save new customer
                session.Save(user);

                // Commit changes to SQL Server
                transaction.Commit();

                // Registration successful
                serviceResponse.Status = true;
                serviceResponse.Message = "Customer registered successfully.";
                serviceResponse.Data = user.UserId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                serviceResponse.Message = "An error occurred during registration.";
            }

            return serviceResponse;
        }
    }
}