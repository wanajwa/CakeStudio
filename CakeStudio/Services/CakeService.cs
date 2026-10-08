using CakeStudio.Data;
using CakeStudio.Models;
using NHibernate.Linq;
using NHibernate;
namespace CakeStudio.Services
{
    public class CakeService : ICakeService
    {
        private readonly NHibernateHelper _nhibernateHelper;

        public CakeService(NHibernateHelper nhibernateHelper)
        {
            _nhibernateHelper = nhibernateHelper;
        }

        public List<Cake> GetAll()
        {
            try
            {

                using var session = _nhibernateHelper.OpenSession();

                var cakes = session.QueryOver<Cake>()
                    .Fetch(SelectMode.Fetch, x => x.Image)
                    .List()
                    .ToList();

                return cakes;
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"An error occurred while fetching cakes: {ex.Message}");
                throw;
            }
        }

        public Cake? GetById(int id)
        {
            try
            {
                using var session = _nhibernateHelper.OpenSession();

                var cake = session.QueryOver<Cake>()
                    .Where(x => x.CakeId == id)
                    .Fetch(SelectMode.Fetch, x => x.Image)
                    .SingleOrDefault();

                return cake;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching the cake with ID {id}: {ex.Message}");
                throw;
            }
        }

        public void Create(Cake cake)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();
            try
            {

                if (cake.Image != null)
                {
                    session.Save(cake.Image);
                }

                session.Save(cake);

                transaction.Commit();
            }
            catch (Exception ex)
            {
                if (transaction.IsActive)
                {
                    transaction.Rollback();
                }
                Console.WriteLine($"An error occurred while creating the cake: {ex.Message}");
                throw;
            }
        }

        public void Update(Cake cake)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();

            try
            {
            
                // Get the existing cake from the database
                var existingCake = session.QueryOver<Cake>()
                    .Where(x => x.CakeId == cake.CakeId)
                    .SingleOrDefault(); 

                if (existingCake == null)
                {
                    transaction.Rollback();
                    return;
                }

                // Update normal cake information
                existingCake.CakeName = cake.CakeName;
                existingCake.Description = cake.Description;
                existingCake.BasePrice = cake.BasePrice;
                existingCake.IsAvailable = cake.IsAvailable;

                // If the baker uploaded a new image
                if (cake.Image != null)
                {
                    // Keep reference to the old image
                    var oldImage = existingCake.Image;

                    // Save the new image first
                    session.Save(cake.Image);

                    // Replace the cake's image
                    existingCake.Image = cake.Image;

                    // Make sure Cake is updated before deleting old Image
                    session.Flush();

                    // Delete the old image if there was one
                    if (oldImage != null)
                    {
                        session.Delete(oldImage);
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                if (transaction.IsActive)
                {
                    transaction.Rollback();
                }
                Console.WriteLine($"An error occurred while updating the cake with ID {cake.CakeId}: {ex.Message}");
                throw;
            }
        }

        public void Delete(int id)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();
            try
            {
                var cake = session.QueryOver<Cake>()
                    .Where(x => x.CakeId == id)
                    .SingleOrDefault();

                if (cake == null)
                {
                    transaction.Rollback();
                    return;
                }
                // Keep the image before deleting the cake
                var image = cake.Image;

                // Delete the cake first
                session.Delete(cake);

                // Make sure Cake is deleted before its Image
                session.Flush();

                // Delete the related image if it exists
                if (image != null)
                {
                    session.Delete(image);
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                if (transaction.IsActive)
                {
                    transaction.Rollback();
                }
                Console.WriteLine($"An error occurred while deleting the cake with ID {id}: {ex.Message}");
                throw;

            }
        }
    }
}