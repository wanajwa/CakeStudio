using CakeStudio.Models;

namespace CakeStudio.Services
{
    public interface ICakeService
    {
        List<Cake> GetAll();
        List<Cake> GetAvailableCakes();
        Cake? GetById(int id);
        void Create(Cake cake);
        void Update(Cake cake);
        void Delete(int id);
    }
}