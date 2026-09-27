using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDMS.Models
{
    [Table("Medicine")]
    public class Medicine
    {
        [Key]
        public int MedicineID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        [Display(Name = "Generic Name")]
        public string? GenericName { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(50)]
        [Display(Name = "Unit Type")]
        public string? UnitType { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, double.MaxValue)]
        [Display(Name = "Purchase Price")]
        public decimal? PurchasePrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, double.MaxValue)]
        [Display(Name = "Selling Price")]
        public decimal? SellingPrice { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Reorder Level")]
        public int? ReorderLevel { get; set; }

        // Foreign Key
        [Required]
        [Display(Name = "Manufacturer")]
        public int ManufacturerID { get; set; }

        // Navigation properties
        [ForeignKey("ManufacturerID")]
        public Manufacturer? Manufacturer { get; set; }

        public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    }
}
