
using CakeStudio.Services;
using CakeStudio.Services.Interfaces;
using CakeStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CakeStudio.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerCakeController : Controller
    {
        private readonly ICakeService _cakeService;
        private readonly ICakeSizeService _cakeSizeService;
        private readonly ICakeFlavourService _cakeFlavourService;
        private readonly ICakeTierService _cakeTierService;

        // Inject existing services
        public CustomerCakeController(
            ICakeService cakeService,
            ICakeSizeService cakeSizeService,
            ICakeFlavourService cakeFlavourService,
            ICakeTierService cakeTierService)
        {
            _cakeService = cakeService;
            _cakeSizeService = cakeSizeService;
            _cakeFlavourService = cakeFlavourService;
            _cakeTierService = cakeTierService;
        }

        // Display available signature cakes
        [HttpGet]
        public IActionResult Index()
        {
            var cakes = _cakeService.GetAvailableCakes();

            return View(cakes);
        }

        // Display cake image
        [HttpGet]
        public IActionResult Image(int id)
        {
            var cake = _cakeService.GetById(id);

            if (cake == null || !cake.IsAvailable)
            {
                return NotFound();
            }

            if (cake.Image == null ||
                cake.Image.ImageContent == null ||
                cake.Image.ImageContent.Length == 0)
            {
                return NotFound();
            }

            return File(
                cake.Image.ImageContent,
                cake.Image.ImageType
            );
        }

        // Display cake details and available options
        [HttpGet]
        public IActionResult Details(int id)
        {
            // Retrieve selected cake
            var cake = _cakeService.GetById(id);

            if (cake == null || !cake.IsAvailable)
            {
                return NotFound();
            }

            // Retrieve only available options
            var sizes = _cakeSizeService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            var flavours = _cakeFlavourService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            var tiers = _cakeTierService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            // Combine cake and options into one ViewModel
            var viewModel = new CustomerCakeDetailsViewModel
            {
                Cake = cake,
                CakeSizes = sizes,
                CakeFlavours = flavours,
                CakeTiers = tiers
            };

            return View(viewModel);
        }

        // Return cake details as a Partial View
        [HttpGet]
        public IActionResult DetailsPanel(int id)
        {
            var cake = _cakeService.GetById(id);

            if (cake == null || !cake.IsAvailable)
            {
                return NotFound();
            }

            var sizes = _cakeSizeService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            var flavours = _cakeFlavourService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            var tiers = _cakeTierService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            var viewModel = new CustomerCakeDetailsViewModel
            {
                Cake = cake,
                CakeSizes = sizes,
                CakeFlavours = flavours,
                CakeTiers = tiers
            };

            return PartialView("_CakeDetailsPanel", viewModel);
        }
    }
}
