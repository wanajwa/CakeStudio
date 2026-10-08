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

            Map(x => x.OrderId)
                .Not.Nullable();

            Map(x => x.Amount)
                .Not.Nullable();

            Map(x => x.PaymentStatus)
                .Length(50)
                .Not.Nullable();
        }
    }
}