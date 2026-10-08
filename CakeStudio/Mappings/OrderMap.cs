using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class OrderMap : ClassMap<Order>
    {
        public OrderMap()
        {
            // SQL Server table name
            Table("`Order`");

            // Primary key
            Id(x => x.OrderId)
                .GeneratedBy.Identity();

            // Customer ID (required)
            Map(x => x.CustId)
                .Not.Nullable();

            // Optional cake selection
            Map(x => x.CakeId)
                .Nullable();

            // Optional custom cake selections
            Map(x => x.FlavourId)
                .Nullable();

            Map(x => x.SizeId)
                .Nullable();

            Map(x => x.TierId)
                .Nullable();

            // Baker may be assigned later
            Map(x => x.BakerId)
                .Nullable();

            // Required date
            Map(x => x.RequiredDate)
                .Not.Nullable();

            // Collection method
            Map(x => x.CollectionMethod)
                .Length(50)
                .Not.Nullable();

            // Delivery address (optional)
            Map(x => x.DeliveryAddress)
                .Length(500)
                .Nullable();

            // Message on cake (optional)
            Map(x => x.MessageOnCakes)
                .Length(500)
                .Nullable();

            // Total price
            Map(x => x.TotalPrice)
                .Not.Nullable();

            // Order status
            Map(x => x.OrderStatus)
                .Length(50)
                .Not.Nullable();

            // Creation date
            Map(x => x.CreatedAt)
                .Not.Nullable();

            // Last update date (optional)
            Map(x => x.UpdatedAt)
                .Nullable();

            // User who updated the order
            Map(x => x.UpdatedId)
                .Nullable();

            // User who created the order
            Map(x => x.CreatedId)
                .Nullable();
        }
    }
}