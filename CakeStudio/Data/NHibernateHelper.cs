// Main NHibernate library.
using NHibernate;

// NHibernate configuration.
using NHibernate.Cfg;

// SQL Server dialect.
using NHibernate.Dialect;

// SQL Server database driver.
using NHibernate.Driver;

// Fluent NHibernate configuration.
using FluentNHibernate.Cfg;

// Our Fluent NHibernate mappings.
using CakeStudio.Mappings;

namespace CakeStudio.Data
{
    public class NHibernateHelper
    {
        // SessionFactory creates NHibernate database sessions.
        private readonly ISessionFactory _sessionFactory;

        public NHibernateHelper(IConfiguration configuration)
        {
            // Get the SQL Server connection string
            // from appsettings.json.
            string connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection was not found.");

            // Create the normal NHibernate configuration.
            var nhConfiguration = new Configuration();

            // Configure the SQL Server connection.
            nhConfiguration.DataBaseIntegration(db =>
            {
                // Database connection string.
                db.ConnectionString = connectionString;

                // Tell NHibernate we are using SQL Server.
                db.Dialect<MsSql2012Dialect>();

                // Tell NHibernate which SQL Server driver to use.
                db.Driver<MicrosoftDataSqlClientDriver>();
            });

            // Configure Fluent NHibernate.
            _sessionFactory = Fluently.Configure(nhConfiguration)

                // Tell Fluent NHibernate where our mapping
                // classes are located.
                .Mappings(m =>
                    m.FluentMappings
                        .AddFromAssemblyOf<CakeMap>())

                // Build the SessionFactory.
                .BuildSessionFactory();
        }

        // Open a new NHibernate database session.
        public NHibernate.ISession OpenSession()
        {
            return _sessionFactory.OpenSession();
        }
    }
}