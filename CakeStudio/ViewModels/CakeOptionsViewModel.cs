using CakeStudio.Models;

namespace CakeStudio.ViewModels
{
    public class CakeOptionsViewModel
    {
        // List of cake sizes
        public List<CakeSize> CakeSizes { get; set; } = new();

        // List of cake flavours
        public List<CakeFlavour> CakeFlavours { get; set; } = new();

        // List of cake tiers (for later)
        public List<CakeTier> CakeTiers { get; set; } = new();
    }
}