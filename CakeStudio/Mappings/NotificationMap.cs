using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class NotificationMap : ClassMap<Notification>
    {
        public NotificationMap()
        {
            Table("Notification");

            Id(x => x.NotificationId)
                .GeneratedBy.Identity();

            Map(x => x.UserId)
                .Not.Nullable();

            Map(x => x.OrderId)
                .Not.Nullable();

            Map(x => x.Message)
                .Length(500)
                .Not.Nullable();

            Map(x => x.IsRead)
                .Not.Nullable();

            Map(x => x.CreatedAt)
                .Not.Nullable();
        }
    }
}