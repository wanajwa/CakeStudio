using CakeStudio.Data;
using CakeStudio.Models;
using CakeStudio.Resources;

namespace CakeStudio.Services
{
    public class CakeSizeService : ICakeSizeService
    {
        private readonly NHibernateHelper _nhibernateHelper;

        public CakeSizeService(NHibernateHelper nhibernateHelper)
        {
            _nhibernateHelper = nhibernateHelper;
        }

        // READ: Get all cake sizes
        public List<CakeSize> GetAll()
        {
            using var session = _nhibernateHelper.OpenSession();

            return session.QueryOver<CakeSize>()
                .OrderBy(x => x.SizeId).Asc
                .List()
                .ToList();
        }

        // READ: Get one cake size by ID
        public CakeSize? GetById(int id)
        {
            using var session = _nhibernateHelper.OpenSession();

            return session.QueryOver<CakeSize>()
                .Where(x => x.SizeId == id)
                .SingleOrDefault();
        }

        // CREATE: Add a new cake size
        public void Create(CakeSize cakeSize)
        {
            using var session = _nhibernateHelper.OpenSession();
            cakeSize.SizeName = cakeSize.SizeName.Trim();
            // Check whether the size already exists
            var existingSize = session.QueryOver<CakeSize>()
                .Where(x => x.SizeName == cakeSize.SizeName)
                .SingleOrDefault();

            if (existingSize != null)
            {
                throw new InvalidOperationException(Resource.DuplicateCakeSize);
            }
            using var transaction = session.BeginTransaction();

            try
            {
                session.Save(cakeSize);
                transaction.Commit();
            }
            catch
            {
                if (transaction.IsActive)
                    transaction.Rollback();

                throw;
            }
        }

        // UPDATE: Edit an existing cake size
        public void Update(CakeSize cakeSize)
        {
            using var session = _nhibernateHelper.OpenSession();
            cakeSize.SizeName = cakeSize.SizeName.Trim();
            // Check whether the size already exists
            var existingSize = session.QueryOver<CakeSize>()
                .Where(x => x.SizeName == cakeSize.SizeName && x.SizeId != cakeSize.SizeId)
                .SingleOrDefault();

            if (existingSize != null)
            {
                throw new InvalidOperationException(Resource.DuplicateCakeSize );
            }
            using var transaction = session.BeginTransaction();

            try
            {
                session.Update(cakeSize);
                transaction.Commit();
            }
            catch
            {
                if (transaction.IsActive)
                    transaction.Rollback();

                throw;
            }
        }

        // DELETE: Remove a cake size
        public void Delete(int id)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction(System.Data.IsolationLevel.Serializable);

            try
            {
                var cakeSize = session.QueryOver<CakeSize>()
                    .Where(x => x.SizeId == id)
                    .SingleOrDefault();

                if (cakeSize == null)
                    return;

                if (session.QueryOver<OrderItem>().Where(item => item.SizeId == id).RowCount() > 0)
                {
                    throw new InvalidOperationException(Resource.CatalogueItemUsedInOrder);
                }

                session.Delete(cakeSize);
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