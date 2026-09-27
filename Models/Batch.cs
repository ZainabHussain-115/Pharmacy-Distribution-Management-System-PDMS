using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDMS.Models
{
    [Table("Batch")]
    public class Batch
    {
        [Key]
        public int BatchID { get; set; }

        // Foreign Keys
        [Required]
        [Display(Name = "Medicine")]
        public int MedicineID { get; set; }

        [Required]
        [Display(Name = "Warehouse")]
        public int WarehouseID { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Batch Number")]
        public string BatchNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Manufacturing Date")]
        public DateTime ManufacturingDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Quantity Received")]
        public int? QuantityReceived { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Quantity Available")]
        public int? QuantityAvailable { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Purchase Date")]
        public DateTime? PurchaseDate { get; set; } = DateTime.Today;

        // Computed helper (not stored in DB)
        [NotMapped]
        public bool IsExpired => ExpiryDate < DateTime.Today;

        [NotMapped]
        public bool IsLowStock => QuantityAvailable <= 20; // adjust threshold as needed

        // Navigation properties
        [ForeignKey("MedicineID")]
        public Medicine? Medicine { get; set; }

        [ForeignKey("WarehouseID")]
        public Warehouse? Warehouse { get; set; }

        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    }
}
