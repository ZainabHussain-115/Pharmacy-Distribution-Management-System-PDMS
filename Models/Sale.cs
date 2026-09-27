using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDMS.Models
{
    [Table("Sale")]
    public class Sale
    {
        [Key]
        public int SaleID { get; set; }

        // Foreign Keys
        [Required]
        [Display(Name = "Customer")]
        public int CustomerID { get; set; }

        [Required]
        [Display(Name = "Created By")]
        public int CreatedBy { get; set; }

        [Display(Name = "Sale Date")]
        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, double.MaxValue)]
        [Display(Name = "Total Amount")]
        public decimal? TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Discount")]
        public decimal Discount { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Tax Amount")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, double.MaxValue)]
        [Display(Name = "Net Amount")]
        public decimal? NetAmount { get; set; }

        [StringLength(20)]
        [Display(Name = "Payment Status")]
        public string? PaymentStatus { get; set; }  // "Paid" or "Pending"

        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; }  // "Cash", "Credit", etc.

        // Navigation properties
        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        [ForeignKey("CreatedBy")]
        public User? User { get; set; }

        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    }
}
