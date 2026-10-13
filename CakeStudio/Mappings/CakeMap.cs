using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class CakeMap : ClassMap<Cake>
    {
        public CakeMap()
        {
            Table("Cake");

            Id(x => x.CakeId)
                .GeneratedBy.Identity();

            Map(x => x.CakeName)
                .Length(100)
                .Not.Nullable();

            Map(x => x.Description)
                .Length(500)
                .Nullable();

            Map(x => x.BasePrice)
                .Not.Nullable();

            References(x => x.Image)
                .Column("ImageId")
                .Nullable();    

            Map(x => x.IsAvailable)
                .Not.Nullable();
            References(x => x.Flavour)
            .Column("CakeFlavourId")
            .Not.Nullable();

        }
    }
}