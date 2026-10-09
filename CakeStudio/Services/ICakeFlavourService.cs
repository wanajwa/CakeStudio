using CakeStudio.Models;

namespace CakeStudio.Services.Interfaces
{
    public interface ICakeFlavourService
    {
        // READ: Retrieve all cake flavours
        List<CakeFlavour> GetAll();

        // READ: Retrieve one cake flavour by ID
        CakeFlavour? GetById(int id);

        // CREATE: Add a new cake flavour
        void Create(CakeFlavour cakeFlavour);

        // UPDATE: Edit an existing cake flavour
        void Update(CakeFlavour cakeFlavour);

        // DELETE: Remove a cake flavour
        void Delete(int id);
    }
}