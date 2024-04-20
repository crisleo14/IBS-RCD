using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class CheckVoucherHeader : BaseEntity
    {
        [Display(Name = "CV No")]
        public string? CVNo { get; set; }

        public long SeriesNumber { get; set; }

        public DateTime Date { get; set; }

        [Display(Name = "RR No")]
        [Column(TypeName = "varchar[]")]
        public string[]? RRNo { get; set; }

        [Display(Name = "SI No")]
        [Column(TypeName = "varchar[]")]
        public string[]? SINo { get; set; }

        [NotMapped]
        public List<SelectListItem>? RR { get; set; }

        [Display(Name = "PO No")]
        [Column(TypeName = "varchar[]")]
        public string[]? PONo { get; set; }

        [NotMapped]
        public List<SelectListItem>? PO { get; set; }

        [Display(Name = "Supplier Id")]
        public int? SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set;}

        [NotMapped]
        public List<SelectListItem>? Suppliers { get; set; }

        [Display(Name = "Total Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Amount in Words")]
        public string? AmountInWords { get; set; }

        public string Particulars { get; set; }

        [Display(Name = "Bank Account Name")]
        public int BankId { get; set; }
        [ForeignKey("BankId")]
        public BankAccount? BankAccount { get; set; }

        [Display(Name = "Check #")]
        [Required(ErrorMessage = "The field is required")]
        [RegularExpression(@"^(?:\d{10,}|DM\d{10})$", ErrorMessage = "Invalid format. Please enter either a 'DM' followed by a 10-digits or CV number minimum 10 digits.")]
        public string CheckNo { get; set; }

        public string Category { get; set; }

        [Display(Name = "Payee")]
        public string Payee { get; set; }

        [NotMapped]
        public List<SelectListItem>? BankAccounts { get; set; }

        [NotMapped]
        public List<SelectListItem>? COA { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalDebit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalCredit { get; set; }
    }
}