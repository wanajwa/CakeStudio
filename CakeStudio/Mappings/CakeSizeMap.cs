using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class CakeSizeMap : ClassMap<CakeSize>
    {
        public CakeSizeMap()
        {
            Table("CakeSize");

            Id(x => x.SizeId)
                .GeneratedBy.Identity();

            Map(x => x.SizeName)
                .Length(100)
                .Not.Nullable();

            Map(x => x.AdditionalPrice)
                .Not.Nullable();

            Map(x => x.IsAvailable)
                .Not.Nullable();
        }
    }
}