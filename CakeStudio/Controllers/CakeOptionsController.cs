
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CakeStudio.Models;
using CakeStudio.Services.Interfaces;
using CakeStudio.ViewModels;

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

        // READ: Display Cake Options page
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
        public IActionResult CreateSize(CakeSize cakeSize)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Invalid cake size information. Please check the fields.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _cakeSizeService.Create(cakeSize);

                TempData["SuccessMessage"] =
                    "Cake size added successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] =
                    "This cake size already exists.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Unable to add cake size. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // UPDATE: Save changes to an existing size
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditSize(CakeSize cakeSize)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Invalid cake size information. Please check the fields.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                var existingSize = _cakeSizeService.GetById(cakeSize.SizeId);

                if (existingSize == null)
                {
                    return NotFound();
                }

                _cakeSizeService.Update(cakeSize);

                TempData["SuccessMessage"] =
                    "Cake size updated successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Unable to update cake size. Please try again.";
            }

            return RedirectToAction(nameof(Index));
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

                TempData["SuccessMessage"] =
                    "Cake size deleted successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Unable to delete cake size. It may already be used in an order.";
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
                TempData["ErrorMessage"] =
                    "Invalid cake flavour information. Please check the fields.";

                return RedirectToAction(nameof(Index), new { tab = "flavours" });
            }

            try
            {
                _cakeFlavourService.Create(cakeFlavour);

                TempData["SuccessMessage"] =
                    "Cake flavour added successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] =
                    "This cake flavour already exists.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Unable to add cake flavour. Please try again.";
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
                TempData["ErrorMessage"] =
                    "Invalid cake flavour information. Please check the fields.";

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

                TempData["SuccessMessage"] =
                    "Cake flavour updated successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Unable to update cake flavour. Please try again.";
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

                TempData["SuccessMessage"] =
                    "Cake flavour deleted successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Unable to delete cake flavour. It may already be used in an order.";
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
                TempData["ErrorMessage"] =
                    "Invalid cake tier information. Please check the fields.";
                return RedirectToAction(nameof(Index), new { tab = "tiers" });
            }
            try
            {
                _cakeTierService.Create(cakeTier);
                TempData["SuccessMessage"] =
                    "Cake tier added successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] =
                    "This cake tier already exists.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Unable to add cake tier. Please try again.";
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
                TempData["ErrorMessage"] =
                    "Invalid cake tier information. Please check the fields.";
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
                TempData["SuccessMessage"] =
                    "Cake tier updated successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Unable to update cake tier. Please try again.";
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
                TempData["SuccessMessage"] =
                    "Cake tier deleted successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Unable to delete cake tier. It may already be used in an order.";
            }
            return RedirectToAction(nameof(Index), new { tab = "tiers" });
        }

    }
}
