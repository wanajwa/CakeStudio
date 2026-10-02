using CakeStudio.Data;
using Microsoft.AspNetCore.Mvc;

namespace CakeStudio.Controllers
{
    public class DatabaseTestController : Controller
    {
        // Store our NHibernateHelper.
        private readonly NHibernateHelper _nhibernateHelper;


        // ASP.NET Core gives this controller our NHibernateHelper
        // through Dependency Injection.
        public DatabaseTestController(NHibernateHelper nhibernateHelper)
        {
            // Save it so other methods in this controller can use it.
            _nhibernateHelper = nhibernateHelper;
        }


        public IActionResult Index()
        {
            // Ask NHibernateHelper to open a database session.
            using var session = _nhibernateHelper.OpenSession();

            // Check whether the NHibernate session is connected.
            if (session.IsConnected)
            {
                return Content("Database connected successfully!");
            }

            return Content("Database connection failed.");
        }
    }
}