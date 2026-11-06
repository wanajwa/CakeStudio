using CakeStudio.Models;
namespace CakeStudio.Services
{
    public interface ICakeTierService
    {
        List<CakeTier> GetAll();
        CakeTier? GetById(int id);
        void Create(CakeTier cakeTier);
        void Update(CakeTier cakeTier);
        void Delete(int id);

    }
}
