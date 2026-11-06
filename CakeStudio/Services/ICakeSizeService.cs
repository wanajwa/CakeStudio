using CakeStudio.Models;

namespace CakeStudio.Services
{
    public interface ICakeSizeService
    {
        List<CakeSize> GetAll();

        CakeSize? GetById(int id);

        void Create(CakeSize cakeSize);

        void Update(CakeSize cakeSize);

        void Delete(int id);
    }
}