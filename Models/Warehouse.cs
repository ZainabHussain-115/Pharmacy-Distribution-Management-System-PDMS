using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDMS.Models
{
    [Table("Warehouse")]
    public class Warehouse
    {
        [Key]
        public int WarehouseID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [Range(0, int.MaxValue)]
        public int? Capacity { get; set; }

        [StringLength(100)]
        [Display(Name = "Manager Name")]
        public string? ManagerName { get; set; }

        [StringLength(20)]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        // Navigation property
        public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    }
}
