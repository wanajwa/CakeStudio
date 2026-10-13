using Microsoft.AspNetCore.Mvc;
using CakeStudio.Models;
using CakeStudio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using CakeStudio.Services.Interfaces;

namespace CakeStudio.Controllers
{
    [Authorize(Roles = "Baker")]
    public class CakeController : Controller
    {

        private readonly ICakeService _cakeService;
        private readonly ICakeFlavourService _cakeFlavourService;

        public CakeController(
            ICakeService cakeService,
            ICakeFlavourService cakeFlavourService)
        {
            _cakeService = cakeService;
            _cakeFlavourService = cakeFlavourService;
        }

        // Load available flavours for the dropdown.
        private void LoadFlavours(int? selectedFlavourId = null)
        {
            var availableFlavours = _cakeFlavourService.GetAll()
                .Where(flavour => flavour.IsAvailable)
                .ToList();

            ViewBag.Flavours = new SelectList(
                availableFlavours,
                "FlavourId",
                "FlavourName",
                selectedFlavourId
            );

        }

        // Validate the selected flavour and assign it to the cake.
        private void AssignFlavour(Cake cake, int? flavourId)
        {
            // The form submits flavourId, not a complete Flavour object.
            // Remove automatic validation for that navigation object.
            var flavourKeys = ModelState.Keys
                .Where(key => key == nameof(Cake.Flavour)
                    || key.StartsWith(nameof(Cake.Flavour) + "."))
                .ToList();

            foreach (var key in flavourKeys)
            {
                ModelState.Remove(key);
            }

            if (!flavourId.HasValue)
            {
                ModelState.AddModelError("flavourId", "Please select a flavour.");
                return;
            }

            var flavour = _cakeFlavourService.GetById(flavourId.Value);

            if (flavour == null || !flavour.IsAvailable)
            {
                ModelState.AddModelError(
                    "flavourId",
                    "Please select an available flavour.");

                return;
            }

            cake.Flavour = flavour;
        }

        public IActionResult Index()
        {
            var cakes = _cakeService.GetAll();
            return View(cakes);
        }

        //Get cake
        // GET: Display the Create Cake form.
        [HttpGet]
        public IActionResult Create()
        {
            LoadFlavours();

            return View();
        }

        //PostCake
        // receive submit save cake
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    Cake cake,
    int? flavourId,
    IFormFile? imageFile)
        {
            AssignFlavour(cake, flavourId);

            if (!ModelState.IsValid)
            {
                LoadFlavours(flavourId);
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
            LoadFlavours(cake.Flavour.FlavourId);
            // Send the existing Cake object to Edit.cshtml.
            return View(cake);
        }

        // POST: /Cake/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Cake cake, int? flavourId,IFormFile? imageFile)
        {
            AssignFlavour(cake, flavourId);

            if (!ModelState.IsValid)
            {
                LoadFlavours(flavourId);
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