using System.ComponentModel.DataAnnotations;

namespace CakeStudio.Models
{
    public class Image
    {
        public virtual int ImageId { get; set; }

        [Required(ErrorMessage = "Image type is required.")]
        [StringLength(100)]
        public virtual string ImageType { get; set; } = string.Empty;

        [Required(ErrorMessage = "File name is required.")]
        [StringLength(255)]
        public virtual string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Image content is required.")]
        public virtual byte[] ImageContent { get; set; } = Array.Empty<byte>();
    }
}