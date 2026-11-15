
using CakeStudio.Data;
using CakeStudio.Models;
using CakeStudio.Services;
using System.Globalization;
using CakeStudio.Resources;
namespace CakeStudio.Services
{
    public class CakeFlavourService : ICakeFlavourService
    {
        private readonly NHibernateHelper _nhibernateHelper;

        public CakeFlavourService(NHibernateHelper nhibernateHelper)
        {
            _nhibernateHelper = nhibernateHelper;
        }

        // READ: Retrieve all cake flavours
        public List<CakeFlavour> GetAll()
        {
            using var session = _nhibernateHelper.OpenSession();

            return session.QueryOver<CakeFlavour>()
                .OrderBy(x => x.FlavourId).Asc
                .List()
                .ToList();
        }

        // READ: Retrieve one cake flavour by ID
        public CakeFlavour? GetById(int id)
        {
            using var session = _nhibernateHelper.OpenSession();

            return session.QueryOver<CakeFlavour>()
                .Where(x => x.FlavourId == id)
                .SingleOrDefault();
        }

        // CREATE: Add a new cake flavour
        public void Create(CakeFlavour cakeFlavour)
        {
            using var session = _nhibernateHelper.OpenSession();

            cakeFlavour.FlavourName = CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(cakeFlavour.FlavourName.Trim().ToLower());

            // Check whether the flavour already exists
            var existingFlavour = session.QueryOver<CakeFlavour>()
                .Where(x => x.FlavourName == cakeFlavour.FlavourName)
                .SingleOrDefault();

            if (existingFlavour != null)
            {
                throw new InvalidOperationException(Resource.DuplicateCakeFlavour);
            }

            using var transaction = session.BeginTransaction();

            try
            {
                session.Save(cakeFlavour);
                transaction.Commit();
            }
            catch
            {
                if (transaction.IsActive)
                    transaction.Rollback();

                throw;
            }
        }

        // UPDATE: Edit an existing cake flavour
        public void Update(CakeFlavour cakeFlavour)
        {
            using var session = _nhibernateHelper.OpenSession();
            cakeFlavour.FlavourName = CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(cakeFlavour.FlavourName.Trim().ToLower());
            var existingFlavour = session.QueryOver<CakeFlavour>()
                .Where(x => x.FlavourName == cakeFlavour.FlavourName
                && x.FlavourId != cakeFlavour.FlavourId)
                .SingleOrDefault();

            if (existingFlavour != null)
            {
                throw new InvalidOperationException( Resource.DuplicateCakeFlavour);
            }
            using var transaction = session.BeginTransaction();

            try
            {
                session.Update(cakeFlavour);
                transaction.Commit();
            }
            catch
            {
                if (transaction.IsActive)
                    transaction.Rollback();

                throw;
            }
        }

        // DELETE: Remove a cake flavour
        public void Delete(int id)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction(System.Data.IsolationLevel.Serializable);

            try
            {
                var cakeFlavour = session.QueryOver<CakeFlavour>()
                    .Where(x => x.FlavourId == id)
                    .SingleOrDefault();

                if (cakeFlavour == null)
                    return;

                if (session.QueryOver<OrderItem>()
                    .Where(item => item.FlavourId == id)
                    .RowCount() > 0)
                {
                    throw new InvalidOperationException(Resource.CatalogueItemUsedInOrder);
                }

                if (session.QueryOver<Cake>()
                    .JoinQueryOver(cake => cake.Flavour)
                    .Where(flavour => flavour.FlavourId == id)
                    .RowCount() > 0)
                {
                    throw new InvalidOperationException(Resource.FlavourUsedAsCakeDefault);
                }

                session.Delete(cakeFlavour);
                transaction.Commit();
            }
            catch
            {
                if (transaction.IsActive)
                    transaction.Rollback();

                throw;
            }
        }
    }
}
