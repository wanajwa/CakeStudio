using CakeStudio.Data;
using CakeStudio.Models;
using CakeStudio.Services.Interfaces;

namespace CakeStudio.Services
{
    public class CakeTierService : ICakeTierService
    {
        private readonly NHibernateHelper _nhibernateHelper;
        public CakeTierService(NHibernateHelper nhibernateHelper)
        {
            _nhibernateHelper = nhibernateHelper;
        }

        public List<CakeTier> GetAll()
        {
            using var session = _nhibernateHelper.OpenSession();
            return session.QueryOver<CakeTier>()
                .OrderBy(x => x.TierId).Asc
                .List()
                .ToList();
        }

        public CakeTier? GetById(int id)
        {
            using var session = _nhibernateHelper.OpenSession();
            return session.QueryOver<CakeTier>()
                .Where(x => x.TierId == id)
                .SingleOrDefault();
        }

        public void Create(CakeTier cakeTier)
        {
            using var session = _nhibernateHelper.OpenSession();
            cakeTier.TierName = cakeTier.TierName.Trim();
            // Check whether the tier already exists
            var existingTier = session.QueryOver<CakeTier>()
                .Where(x => x.TierName == cakeTier.TierName)
                .SingleOrDefault();

            if (existingTier != null)
            {
                throw new InvalidOperationException(
                    "This cake tier already exists."
                );
            }
            using var transaction = session.BeginTransaction();
            try
            {
                session.Save(cakeTier);
                transaction.Commit();
            }
            catch
            {
                if (transaction.IsActive)
                    transaction.Rollback();
                throw;
            }
        }

        public void Update(CakeTier cakeTier)
        {
            using var session = _nhibernateHelper.OpenSession();
            cakeTier.TierName = cakeTier.TierName.Trim();
            // Check whether the tier already exists
            var existingTier = session.QueryOver<CakeTier>()
                .Where(x => x.TierName == cakeTier.TierName && x.TierId != cakeTier.TierId  )
                .SingleOrDefault();

            if (existingTier != null)
            {
                throw new InvalidOperationException(
                    "This cake tier already exists."
                );
            }
            using var transaction = session.BeginTransaction();

            try
            {
                session.Update(cakeTier);
                transaction.Commit();
            }
            catch
            {
                if (transaction.IsActive)
                    transaction.Rollback();
                throw;
            }
        }

        public void Delete(int id)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transacton = session.BeginTransaction();
            try
            {
                var cakeTier = session.QueryOver<CakeTier>()
                    .Where(x => x.TierId == id)
                    .SingleOrDefault();
                if (cakeTier != null)
                {
                    session.Delete(cakeTier);
                    transacton.Commit();
                }
            }
            catch
            {
                if (transacton.IsActive)
                    transacton.Rollback();
                throw;
            }

        }
    }

}
