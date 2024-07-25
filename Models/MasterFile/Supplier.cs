using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.MasterFile
{
    public class Supplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Supplier No")]
        public int Number { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Name { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Address { get; set; }

        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Tin No")]
        public string TinNo { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string Terms { get; set; }

        [Column(TypeName = "varchar(20)")]
        [Display(Name = "VAT Type")]
        public string VatType { get; set; }

        [Column(TypeName = "varchar(20)")]
        [Display(Name = "TAX Type")]
        public string TaxType { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? ProofOfRegistrationFilePath { get; set; }

        [Display(Name = "Reason")]
        [Column(TypeName = "varchar(100)")]
        public string? ReasonOfExemption { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? Validity { get; set; }

        [Display(Name = "Validity Date")]
        [Column(TypeName = "date")]
        public DateTime? ValidityDate { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? ProofOfExemptionFilePath { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string Category { get; set; }

        public string? TradeName { get; set; }

        public string Branch { get; set; }

        public string? DefaultExpenseNumber { get; set; }

        [NotMapped]
        public List<SelectListItem>? DefaultExpenses { get; set; }

        public int? WithholdingTaxPercent { get; set; }

        [NotMapped]
        public List<SelectListItem>? WithholdingTaxList { get; set; }
    }
}