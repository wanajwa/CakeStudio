using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class PaymentMap : ClassMap<Payment>
    {
        public PaymentMap()
        {
            Table("Payment");

            Id(x => x.PaymentId)
                .GeneratedBy.Identity();

            References(x => x.Order)
                .Column("OrderId")
                .Not.Nullable()
                .Unique();

            Map(x => x.Amount)
                .Precision(18)
                .Scale(2)
                .Not.Nullable();

            Map(x => x.PaymentMethod)
                .Length(50)
                .Not.Nullable();

            Map(x => x.PaymentStatus)
                .Length(50)
                .Not.Nullable();

            Map(x => x.PaidAt)
                .Nullable();

            Map(x => x.RefundedAt)
                .Nullable();
        }
    }
}