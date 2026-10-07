using CakeStudio.Models;
using FluentNHibernate.Mapping;

namespace CakeStudio.Mappings
{
    public class ImageMap : ClassMap<Image>
    {
        public ImageMap()
        {
            Table("Image");

            Id(x => x.ImageId)
                .GeneratedBy.Identity();

            Map(x => x.ImageType)
                .Length(100)
                .Not.Nullable();

            Map(x => x.FileName)
                .Length(255)
                .Not.Nullable();

            Map(x => x.ImageContent)
    .Length(int.MaxValue)
    .CustomSqlType("varbinary(max)")
    .Not.Nullable();
        }
    }
}