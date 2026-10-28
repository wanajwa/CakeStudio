using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class OrderItemMap : ClassMap<OrderItem>
    {
        public OrderItemMap()
        {
            Table("OrderItem");

            Id(x => x.OrderItemId)
                .GeneratedBy.Identity();

            Map(x => x.OrderId)
                .Not.Nullable();

            References(x => x.Order)
                .Column("OrderId")
                .ReadOnly();

            Map(x => x.CakeId)
                .Not.Nullable();

            References(x => x.Cake)
                .Column("CakeId")
                .ReadOnly();

            Map(x => x.FlavourId)
                .Nullable();

            References(x => x.Flavour)
                .Column("FlavourId")
                .Nullable()
                .ReadOnly();

            Map(x => x.SizeId)
                .Nullable();

            References(x => x.Size)
                .Column("SizeId")
                .Nullable()
                .ReadOnly();

            Map(x => x.TierId)
                .Nullable();

            References(x => x.Tier)
                .Column("TierId")
                .Nullable()
                .ReadOnly();

            Map(x => x.Quantity)
                .Not.Nullable();

            Map(x => x.UnitPrice)
                .Precision(18)
                .Scale(2)
                .Not.Nullable();

            Map(x => x.MessageOnCakes)
                .Length(500)
                .Nullable();
        }
    }
}