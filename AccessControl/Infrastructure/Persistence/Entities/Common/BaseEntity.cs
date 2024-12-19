using System.ComponentModel.DataAnnotations;

namespace AccessControl.Infrastructure.Persistence.Entities.Common
{
    public class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime UpdatedDate { get; set; }
    }
}
