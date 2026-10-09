using CakeStudio.Models;

namespace CakeStudio.ViewModels
{
    public class CustomerCakeDetailsViewModel
    {
        // Selected signature cake
        public Cake Cake { get; set; } = new Cake();

        // Available cake sizes
        public List<CakeSize> CakeSizes { get; set; } = new();

        // Available cake flavours
        public List<CakeFlavour> CakeFlavours { get; set; } = new();

        // Available cake tiers
        public List<CakeTier> CakeTiers { get; set; } = new();
    }
}