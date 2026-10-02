// Imports the main NHibernate library.
// Gives us access to things such as ISessionFactory and ISession.
using NHibernate;

// Imports NHibernate's configuration classes.
// We need this for the Configuration class below.
using NHibernate.Cfg;

// Imports database dialects.
// A dialect tells NHibernate what type/version of SQL syntax the database uses.
using NHibernate.Dialect;

// Imports NHibernate database drivers.
// The driver is responsible for communicating with the database provider.
using NHibernate.Driver;


// Places this class inside the CakeStudio.Data namespace.
// "Data" is where we keep database-related code for CakeStudio.
namespace CakeStudio.Data
{
    // Creates a class called NHibernateHelper.
    // Its job is to configure NHibernate and provide database sessions.
    public class NHibernateHelper
    {
        // Stores the NHibernate Session Factory.
        //
        // ISessionFactory is responsible for creating ISession objects.
        // We create the factory once and reuse it.
        //
        // private   = only this class can directly access it.
        // readonly  = after it has been assigned, we cannot replace it.
        private readonly ISessionFactory _sessionFactory;


        // This is the constructor of NHibernateHelper.
        //
        // ASP.NET Core will give us an IConfiguration object.
        // IConfiguration allows us to read settings from appsettings.json.
        public NHibernateHelper(IConfiguration configuration)
        {
            // Ask IConfiguration to find a connection string
            // called "DefaultConnection" from appsettings.json.
            string connectionString =
                configuration.GetConnectionString("DefaultConnection")

                // If DefaultConnection cannot be found,
                // stop the application and show this error.
                ?? throw new InvalidOperationException(
                    "DefaultConnection was not found.");


            // Creates a new NHibernate Configuration object.
            //
            // We will use this object to tell NHibernate:
            // - what database we use
            // - how to connect
            // - what SQL dialect to use
            // - what database driver to use
            var nhConfiguration = new Configuration();


            // Configure NHibernate's database connection.
            //
            // "db" represents the database configuration
            // that we are currently setting up.
            nhConfiguration.DataBaseIntegration(db =>
            {
                // Give NHibernate our connection string.
                //
                // This tells NHibernate WHERE our database is.
                db.ConnectionString = connectionString;


                // Tell NHibernate that the database is Microsoft SQL Server.
                //
                // A "Dialect" tells NHibernate how SQL should be generated
                // for a particular database system.
                db.Dialect<MsSql2012Dialect>();


                // Tell NHibernate which .NET SQL Server driver to use.
                //
                // MicrosoftDataSqlClientDriver works with
                // Microsoft.Data.SqlClient.
                db.Driver<MicrosoftDataSqlClientDriver>();
            });


            // Take all the NHibernate configuration above
            // and build an ISessionFactory.
            //
            // The factory will later create database sessions for us.
            _sessionFactory = nhConfiguration.BuildSessionFactory();
        }


        // Creates a method called OpenSession().
        //
        // Other parts of CakeStudio, such as controllers,
        // can call this method when they need to access the database.
        //
        // NHibernate.ISession means this is specifically
        // NHibernate's ISession, not ASP.NET Core's ISession.
        public NHibernate.ISession OpenSession()
        {
            // Ask the Session Factory to create a new database session.
            //
            // Through this session, we can later perform operations such as:
            // Get, Save, Update, Delete and Query.
            return _sessionFactory.OpenSession();
        }
    }
}