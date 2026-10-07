using System.ComponentModel.DataAnnotations;

namespace CakeStudio.Models
{
    public class Image
    {
        public virtual int ImageId { get; set; }

        [Required]
        [StringLength(100)]
        public virtual string ImageType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public virtual string FileName { get; set; } = string.Empty;

        [Required]
        public virtual byte[] ImageContent { get; set; } = Array.Empty<byte>();
    }
}