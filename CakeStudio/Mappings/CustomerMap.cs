using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class CustomerMap : SubclassMap<Customer>
    {
        public CustomerMap()
        {
            DiscriminatorValue("Customer");
        }
    }
}