using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class CakeTierMap : ClassMap<CakeTier>
    {
        public CakeTierMap()
        {
            Table("CakeTier");
            Id(x => x.TierId)
                .GeneratedBy.Identity();
            Map(x => x.TierName)
                .Length(100)
                .Not.Nullable();
            Map(x => x.AdditionalPrice)
                .Not.Nullable();
            Map(x => x.IsAvailable)
                .Not.Nullable();
        }
    }
}
