using System.ComponentModel.DataAnnotations;

namespace CakeStudio.Models
{
    public class User
    {
        public virtual int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public virtual string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public virtual string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public virtual string PasswordHash { get; set; } = string.Empty;

        [StringLength(20)]
        public virtual string? PhoneNumber { get; set; }

        [StringLength(500)]
        public virtual string? Address { get; set; }

        [Required]
        [StringLength(20)]
        public virtual string Role { get; set; } = string.Empty;
    }
}