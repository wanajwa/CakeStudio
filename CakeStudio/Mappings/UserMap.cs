using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class UserMap : ClassMap<User>
    {
        public UserMap()
        {
            Table("`User`");

            Id(x => x.UserId)
                .GeneratedBy.Identity();

            Map(x => x.Name)
                .Length(100)
                .Not.Nullable();

            Map(x => x.Email)
                .Length(255)
                .Not.Nullable()
                .Unique();

            Map(x => x.PasswordHash)
                .Length(255)
                .Not.Nullable();

            Map(x => x.PhoneNumber)
                .Length(20)
                .Nullable();

            Map(x => x.Address)
                .Length(500)
                .Nullable();

            Map(x => x.Role)
                .Length(20)
                .Not.Nullable();
        }
    }
}