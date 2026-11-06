using Microsoft.AspNetCore.Mvc;
using CakeStudio.Models;
using CakeStudio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using CakeStudio.Resources;

namespace CakeStudio.Controllers
{
    [Authorize(Roles = "Baker")]
    public class CakeController : Controller
    {

        private readonly ICakeService _cakeService;
        private readonly ICakeFlavourService _cakeFlavourService;

        public CakeController(ICakeService cakeService, ICakeFlavourService cakeFlavourService)
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
                ModelState.AddModelError("flavourId", Resource.FlavourRequired);
                return;
            }

            var flavour = _cakeFlavourService.GetById(flavourId.Value);

            if (flavour == null || !flavour.IsAvailable)
            {
                ModelState.AddModelError(
                    "flavourId",
                    Resource.AvailableFlavourRequired);

                return;
            }

            cake.Flavour = flavour;
        }

        private bool ValidateImage(IFormFile? imageFile)
        {
            if (imageFile == null)
                return true; // No image uploaded (optional)

            if (imageFile.Length == 0)
            {
                ModelState.AddModelError(
                    "imageFile",
                    Resource.ImageEmpty);

                return false;
            }
            const long maxFileSize = 5 * 1024 * 1024;

            if(imageFile.Length > maxFileSize)
            {
                ModelState.AddModelError("imageFile",Resource.ImageSizeExceeded);
                return false;
            }
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(imageFile.FileName)
                .ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("imageFile",Resource.InvalidImageExtension);
                return false;
            }
            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png"
            };
            
            if (!allowedContentTypes.Contains(
                imageFile.ContentType.ToLowerInvariant()))
            {
                ModelState.AddModelError("imageFile",Resource.InvalidImageType);
                return false;
            }
            return true;
        }


        public IActionResult Index()
        {
            var cakes = _cakeService.GetAll();
            return View(cakes);
        }

        // GET: Display the Create Cake form.
        [HttpGet]
        public IActionResult Create()
        {
            LoadFlavours();

            return View();
        }

        // receive submit save cake
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cake cake, int? flavourId, IFormFile? imageFile)
        {
            AssignFlavour(cake, flavourId);
            ValidateImage(imageFile);

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
            TempData["SuccessMessage"] = Resource.CakeCreatedSuccess;
            return RedirectToAction("Index");
        }


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
            ValidateImage(imageFile);
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
            TempData["SuccessMessage"] = Resource.CakeUpdatedSuccess;
            return RedirectToAction("Index");
        }

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


        // Actually delete the Cake from the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
               _cakeService.Delete(id);
            TempData["SuccessMessage"] = Resource.CakeDeletedSuccess;

            return RedirectToAction("Index");
        }
    }
}