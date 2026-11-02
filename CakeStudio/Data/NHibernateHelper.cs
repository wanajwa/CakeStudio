using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using FluentNHibernate.Cfg;
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
            string connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection was not found.");

            // Create the normal NHibernate configuration.
            var nhConfiguration = new Configuration();

            // Configure the SQL Server connection.
            nhConfiguration.DataBaseIntegration(db =>
            {
                // Database connection string.
                db.ConnectionString = connectionString;
                db.Dialect<MsSql2012Dialect>();
                db.Driver<MicrosoftDataSqlClientDriver>();
            });

            _sessionFactory = Fluently.Configure(nhConfiguration)
                .Mappings(m =>
                    m.FluentMappings
                        .AddFromAssemblyOf<CakeMap>())
                .BuildSessionFactory();
        }
        public NHibernate.ISession OpenSession()
        {
            return _sessionFactory.OpenSession();
        }
    }
}