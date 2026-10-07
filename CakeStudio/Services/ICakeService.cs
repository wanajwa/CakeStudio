using CakeStudio.Models;

namespace CakeStudio.Services
{
    public interface ICakeService
    {
        List<Cake> GetAll();
        Cake? GetById(int id);
        void Create(Cake cake);
        void Update(Cake cake);
        void Delete(int id);
    }
}