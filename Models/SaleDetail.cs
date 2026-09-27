using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDMS.Models
{
    [Table("SaleDetail")]
    public class SaleDetail
    {
        [Key]
        public int SaleDetailID { get; set; }

        // Foreign Keys
        [Required]
        public int SaleID { get; set; }

        [Required]
        [Display(Name = "Batch")]
        public int BatchID { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Quantity Sold")]
        public int? QuantitySold { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, double.MaxValue)]
        [Display(Name = "Unit Price")]
        public decimal? UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, double.MaxValue)]
        [Display(Name = "Sub Total")]
        public decimal? SubTotal { get; set; }

        // Navigation properties
        [ForeignKey("SaleID")]
        public Sale? Sale { get; set; }

        [ForeignKey("BatchID")]
        public Batch? Batch { get; set; }
    }
}
