using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Accounting_System.Models.MasterFile
{
    public class Supplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierId { get; set; }

        [Display(Name = "Supplier No")]
        public int Number { get; set; }

        [StringLength(100)]
        public string SupplierName { get; set; }

        [StringLength(200)]
        public string SupplierAddress { get; set; }

        [StringLength(20)]
        [Display(Name = "Tin No")]
        [RegularExpression(@"\d{3}-\d{3}-\d{3}-\d{5}", ErrorMessage = "Invalid TIN number format.")]
        public string SupplierTin { get; set; }

        [StringLength(10)]
        public string SupplierTerms { get; set; }

        [StringLength(50)]
        [Display(Name = "VAT Type")]
        public string VatType { get; set; }

        [StringLength(50)]
        [Display(Name = "TAX Type")]
        public string TaxType { get; set; }

        [StringLength(1024)]
        public string? ProofOfRegistrationFilePath { get; set; }

        [Display(Name = "Reason")]
        [StringLength(100)]
        public string? ReasonOfExemption { get; set; }

        [StringLength(20)]
        public string? Validity { get; set; }

        [Display(Name = "Validity Date")]
        [Column(TypeName = "date")]
        public DateTime? ValidityDate { get; set; }

        [StringLength(2000)]
        public string? ProofOfExemptionFilePath { get; set; }

        [Display(Name = "Created By")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string Category { get; set; }

        [StringLength(255)]
        public string? TradeName { get; set; }

        [StringLength(20)]
        public string Branch { get; set; }

        [StringLength(100)]
        public string? DefaultExpenseNumber { get; set; }

        [NotMapped]
        public List<SelectListItem>? DefaultExpenses { get; set; }

        public int? WithholdingTaxPercent { get; set; }

        [StringLength(100)]
        public string? WithholdingTaxtitle { get; set; }

        [NotMapped]
        public List<SelectListItem>? WithholdingTaxList { get; set; }

        public int? OriginalSupplierId { get; set; }

        [Required]
        [Display(Name = "Zip Code")]
        [StringLength(10)]
        public string ZipCode { get; set; } = String.Empty;
    }
}
