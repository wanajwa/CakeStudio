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
            References(x => x.Customer)
                .Column("CustId")
                .ReadOnly();

            Map(x => x.BakerId)
                .Nullable();

            Map(x => x.RejectionReason)
                .Length(500)
                .Nullable();

            Map(x => x.CancellationReason)
                .Length(500)
                .Nullable();

            // Required date
            Map(x => x.RequiredDate)
                .Not.Nullable();

            Map(x => x.TotalPrice)
                .Precision(18)
                .Scale(2)
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

            HasMany(x => x.OrderItems)
                .KeyColumn("OrderId")
                .Inverse()
                .Cascade.None();

            Map(x => x.CheckoutRequestId).Nullable();
        }
    }
}