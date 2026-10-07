using CakeStudio.Data;
using CakeStudio.Models;
using NHibernate.Linq;
using System.Net.NetworkInformation;
using System.Transactions;

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
            using var session = _nhibernateHelper.OpenSession();

            var cakes = session.Query<Cake>()
                .Fetch(x => x.Image)
                .ToList();

            return cakes;
        }


        public Cake? GetById(int id)
        {
            using var session = _nhibernateHelper.OpenSession();

            var cake = session.Query<Cake>()
                .Fetch(x => x.Image)
                .FirstOrDefault(x => x.CakeId == id);

            return cake;
        }

        public void Create(Cake cake)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();

            if (cake.Image != null)
            {
                session.Save(cake.Image);
            }

            session.Save(cake);

            transaction.Commit();
        }

        public void Update(Cake cake)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();

            // Get the existing cake from the database
            var existingCake = session.Get<Cake>(cake.CakeId);

            if (existingCake == null)
            {
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

        public void Delete(int id)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();
            var cake = session.Get<Cake>(id);

            if (cake == null)
            {
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
    }
}