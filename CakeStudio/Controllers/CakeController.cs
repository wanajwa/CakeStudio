using Microsoft.AspNetCore.Mvc;
using CakeStudio.Models;
using CakeStudio.Services;

namespace CakeStudio.Controllers
{
    public class CakeController : Controller
    {
      
        private readonly ICakeService _cakeService;
        public CakeController(ICakeService cakeService)
        {
            _cakeService = cakeService; 
        }

        public IActionResult Index()
        {
            var cakes = _cakeService.GetAll();
            return View(cakes);
        }

        //Get cake
        public IActionResult Create()
        {
            return View();
        }

        //PostCake
        // receive submit save cake
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cake cake, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(cake);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();

                await imageFile.CopyToAsync(memoryStream);

                cake.Image = new CakeStudio.Models.Image
                {
                    FileName = imageFile.FileName,
                    ImageType = imageFile.ContentType,
                    ImageContent = memoryStream.ToArray()
                };
            }

            _cakeService.Create(cake);

            return RedirectToAction("Index");
        }

        // GET: /Cake/Edit/3
        // Display the existing cake information.
        public IActionResult Edit(int id)
        {

            // Find the Cake using its CakeId.
            var cake = _cakeService.GetById(id);

            // If the CakeId does not exist.
            if (cake == null)
            {
                return NotFound();
            }

            // Send the existing Cake object to Edit.cshtml.
            return View(cake);
        }

        // POST: /Cake/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Cake cake, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(cake);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();

                await imageFile.CopyToAsync(memoryStream);

                cake.Image = new CakeStudio.Models.Image
                {
                    FileName = imageFile.FileName,
                    ImageType = imageFile.ContentType,
                    ImageContent = memoryStream.ToArray()
                };
            }

            _cakeService.Update(cake);

            return RedirectToAction("Index");
        }

        // GET: /Cake/Delete/3
        // Display the cake before deleting it.
        public IActionResult Delete(int id)
        {
            
            // Find the cake using its CakeId.
            var cake = _cakeService.GetById(id);

            // Cake does not exist.
            if (cake == null)
            {
                return NotFound();
            }

            // Send the cake to the Delete confirmation page.
            return View(cake);
        }


        // POST: /Cake/DeleteConfirmed/3
        // Actually delete the Cake from the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
               _cakeService.Delete(id);
            // Return to the Cake list.
            return RedirectToAction("Index");
        }
    }
}