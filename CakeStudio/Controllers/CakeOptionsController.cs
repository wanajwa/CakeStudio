
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CakeStudio.Models;
using CakeStudio.Services.Interfaces;
using CakeStudio.ViewModels;
using CakeStudio.Resources;

namespace CakeStudio.Controllers
{
    [Authorize(Roles = "Baker")]
    public class CakeOptionsController : Controller
    {
        private readonly ICakeSizeService _cakeSizeService;
        private readonly ICakeFlavourService _cakeFlavourService;
        private readonly ICakeTierService _cakeTierService;
        public CakeOptionsController(ICakeSizeService cakeSizeService, ICakeFlavourService cakeFlavourService, ICakeTierService cakeTierService)
        {
            _cakeSizeService = cakeSizeService;
            _cakeFlavourService = cakeFlavourService;
            _cakeTierService = cakeTierService;
        }

        // READ: Display Cake Sizes, Cake Flavours, and Cake Tiers  
        [HttpGet]
        public IActionResult Index()
        {
            var model = new CakeOptionsViewModel
            {
                CakeSizes = _cakeSizeService.GetAll(),
                CakeFlavours = _cakeFlavourService.GetAll(),
                CakeTiers = _cakeTierService.GetAll()
            };

            return View(model);
        }

        // CREATE: Save new cake size
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateSize(CakeSize cakeSize, int? sizeValue)
        {
            // Validate size value
            if (!sizeValue.HasValue ||
                sizeValue.Value < 1 ||
                sizeValue.Value > 100)
            {
                TempData["ErrorMessage"] = Resource.InvalidCakeSizeValue;

                return RedirectToAction(nameof(Index), new { tab = "sizes" });
            }

            // Automatically append the unit
            cakeSize.SizeName = $"{sizeValue.Value} inch";

            // Clear validation state for the generated SizeName
            ModelState.Remove(nameof(CakeSize.SizeName));

            // Validate the completed CakeSize model
            if (!TryValidateModel(cakeSize))
            {
                TempData["ErrorMessage"] = Resource.InvalidCakeSize;
                return RedirectToAction(nameof(Index), new { tab = "sizes" });
            }

            try
            {
                _cakeSizeService.Create(cakeSize);

                TempData["SuccessMessage"] = Resource.CakeSizeAddedSuccess;
            }
            catch (InvalidOperationException)
            {
                TempData["ErrorMessage"] = Resource.DuplicateCakeSize;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = Resource.CakeSizeAddFailed;
            }

            return RedirectToAction(nameof(Index), new { tab = "sizes" });
        }

        // UPDATE: Save changes to an existing cake size
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditSize(CakeSize cakeSize, int? sizeValue)
        {
            // Validate size value
            if (!sizeValue.HasValue ||
                sizeValue.Value < 1 ||
                sizeValue.Value > 100)
            {
                TempData["ErrorMessage"] = Resource.InvalidCakeSizeValue;
                return RedirectToAction(nameof(Index), new { tab = "sizes" });
            }

            // Automatically append the unit
            cakeSize.SizeName = $"{sizeValue.Value} inch";

            // Clear validation state for the generated SizeName
            ModelState.Remove(nameof(CakeSize.SizeName));

            // Validate the completed CakeSize model
            if (!TryValidateModel(cakeSize))
            {
                TempData["ErrorMessage"] = Resource.InvalidCakeSize;

                return RedirectToAction(nameof(Index), new { tab = "sizes" });
            }

            try
            {
                // Check whether the cake size exists
                var existingSize = _cakeSizeService.GetById(cakeSize.SizeId);

                if (existingSize == null)
                {
                    return NotFound();
                }

                // Update cake size
                _cakeSizeService.Update(cakeSize);

                TempData["SuccessMessage"] = Resource.CakeSizeUpdatedSuccess;
            }
            catch (InvalidOperationException)
            {
                TempData["ErrorMessage"] = Resource.DuplicateCakeSize;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = Resource.CakeSizeUpdateFailed;
            }

            return RedirectToAction(nameof(Index), new { tab = "sizes" });
        }

        // DELETE: Remove cake size
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteSize(int id)
        {
            try
            {
                var cakeSize = _cakeSizeService.GetById(id);

                if (cakeSize == null)
                {
                    return NotFound();
                }

                _cakeSizeService.Delete(id);

                TempData["SuccessMessage"] = Resource.CakeSizeDeletedSuccess;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = Resource.CakeSizeDeleteFailed;
            }

            return RedirectToAction(nameof(Index));
        }


        // CREATE: Save new cake flavour
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateFlavour(CakeFlavour cakeFlavour)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message));

                TempData["ErrorMessage"] = string.Join(" ", errors);

                return RedirectToAction(
                    nameof(Index),
                    new { tab = "flavours" });
            }
            try
            {
                _cakeFlavourService.Create(cakeFlavour);

                TempData["SuccessMessage"] = Resource.CakeFlavourAddedSuccess;
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = Resource.DuplicateCakeFlavour;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = Resource.CakeFlavourAddFailed;
            }

            return RedirectToAction(nameof(Index), new { tab = "flavours" });
        }

        // UPDATE: Save changes to cake flavour
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditFlavour(CakeFlavour cakeFlavour)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = Resource.InvalidCakeFlavour;

                return RedirectToAction(nameof(Index), new { tab = "flavours" });
            }

            try
            {
                var existingFlavour =
                    _cakeFlavourService.GetById(cakeFlavour.FlavourId);

                if (existingFlavour == null)
                {
                    return NotFound();
                }

                _cakeFlavourService.Update(cakeFlavour);

                TempData["SuccessMessage"] = Resource.CakeFlavourUpdatedSuccess;
            }
            catch (InvalidOperationException)
            {
                TempData["ErrorMessage"] = Resource.DuplicateCakeFlavour;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = Resource.CakeFlavourUpdateFailed;
            }

            return RedirectToAction(nameof(Index), new { tab = "flavours" });
        }

        // DELETE: Remove cake flavour
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFlavour(int id)
        {
            try
            {
                var cakeFlavour = _cakeFlavourService.GetById(id);

                if (cakeFlavour == null)
                {
                    return NotFound();
                }

                _cakeFlavourService.Delete(id);

                TempData["SuccessMessage"] = Resource.CakeFlavourDeletedSuccess;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = Resource.CakeFlavourDeleteFailed;
            }

            return RedirectToAction(nameof(Index), new { tab = "flavours" });
        }

        //create new cake tier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTier(CakeTier cakeTier)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message));

                TempData["ErrorMessage"] = string.Join(" ", errors);

                return RedirectToAction(
                    nameof(Index),
                    new { tab = "tiers" });
            }
            try
            {
                _cakeTierService.Create(cakeTier);
                TempData["SuccessMessage"] = Resource.CakeTierAddedSuccess;
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = Resource.DuplicateCakeTier;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = Resource.CakeTierAddFailed;
            }
            return RedirectToAction(nameof(Index), new { tab = "tiers" });
        }

        //UPDATE cake tier
        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult EditTier(CakeTier cakeTier)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = Resource.InvalidCakeTier;
                return RedirectToAction(nameof(Index), new { tab = "tiers" });
            }
            try
            {
                var existingTier = _cakeTierService.GetById(cakeTier.TierId);
                if (existingTier == null)
                {
                    return NotFound();
                }
                _cakeTierService.Update(cakeTier);
                TempData["SuccessMessage"] = Resource.CakeTierUpdatedSuccess;
            }
            catch (InvalidOperationException)
            {
                TempData["ErrorMessage"] = Resource.DuplicateCakeTier;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = Resource.CakeTierUpdateFailed;
            }
            return RedirectToAction(nameof(Index), new { tab = "tiers" });

        }

        //DELETE cake tier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTier(int id)
        {
            try
            {
                var cakeTier = _cakeTierService.GetById(id);
                if (cakeTier == null)
                {
                    return NotFound();
                }
                _cakeTierService.Delete(id);
                TempData["SuccessMessage"] = Resource.CakeTierDeletedSuccess;
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = Resource.CakeTierDeleteFailed;
            }
            return RedirectToAction(nameof(Index), new { tab = "tiers" });
        }

    }
}
