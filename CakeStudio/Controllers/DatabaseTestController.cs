using CakeStudio.Data;
using CakeStudio.Models;
using Microsoft.AspNetCore.Mvc;

namespace CakeStudio.Controllers
{
    public class DatabaseTestController : Controller
    {
        private readonly NHibernateHelper _nhibernateHelper;

        public DatabaseTestController(NHibernateHelper nhibernateHelper)
        {
            _nhibernateHelper = nhibernateHelper;
        }

        public IActionResult InsertCake()
        {
            // 1. Open a connection/session to the database through NHibernate.
            using var session = _nhibernateHelper.OpenSession();

            // 2. Start a database transaction.
            using var transaction = session.BeginTransaction();

            // 3. Create a normal C# Cake object.
            var cake = new Cake
            {
                CakeName = "Chocolate Cake",
                Description = "Chocolate cake",
                BasePrice = 60.00m,
                IsAvailable = true
            };

            // 4. Tell NHibernate to save this Cake.
            session.Save(cake);

            // 5. Commit the transaction.
            // This makes the database change permanent.
            transaction.Commit();

            return Content(
                $"Cake inserted successfully! CakeId: {cake.CakeId}"
            );
        }
    }
}