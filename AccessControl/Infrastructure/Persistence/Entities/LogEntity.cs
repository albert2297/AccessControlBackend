using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AccessControl.Infrastructure.Persistence.Entities.Common;

namespace AccessControl.Infrastructure.Persistence.Entities
{
    [Table("Logs")]
    public class LogEntity : BaseEntity
    {
        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(100, ErrorMessage = "Event name cannot exceed 100 characters.")]
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Details are required.")]
        [StringLength(1000, ErrorMessage = "Details cannot exceed 1000 characters.")]
        public string Details { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public string Email { get; set; } = null!;

        [ForeignKey("UserId")]
        public UserEntity? UserEntity { get; set; }

        public Guid? UserId { get; set; }

    }
}
