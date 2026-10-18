using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class BakerMap : SubclassMap<Baker>
    {
        public BakerMap()
        {
            DiscriminatorValue("Baker");
        }
    }
}