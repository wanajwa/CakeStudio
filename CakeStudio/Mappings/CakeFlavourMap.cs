using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings

{
    public class CakeFlavourMap : ClassMap<CakeFlavour>
    {
        public CakeFlavourMap()
        {
            Table ("CakeFlavour");
            Id (x => x.FlavourId)
                .GeneratedBy.Identity();
            Map(x => x.FlavourName)
                .Length(100)
                .Not.Nullable()
                .Unique();
            Map(x => x.AdditionalPrice)
                .Not.Nullable();
            Map(x => x.IsAvailable)
                .Not.Nullable();

        }
    }
}
