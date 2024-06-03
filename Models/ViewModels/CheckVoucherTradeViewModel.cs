using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models.ViewModels
{
    public class CheckVoucherTradeViewModel
    {
        public string CVNo { get; set; }

        [Required]
        public List<SelectListItem> Suppliers { get; set; }

        [Required]
        public string Payee { get; set; }

        [Required]
        public string SupplierAddress { get; set; }

        [Required]
        [Display(Name = "Tin Number")]
        public string SupplierTinNo { get; set; }

        [Required]
        public int SupplierId { get; set; }

        [Required]
        public List<SelectListItem> PONo { get; set; }

        [Required]
        public int[] POId { get; set; }

        [Required]
        public string[] POSeries { get; set; }

        [Required]
        public List<SelectListItem> RR { get; set; }

        [Required]
        public int[] RRId { get; set; }

        [Required]
        public string[] RRSeries { get; set; }

        public decimal[] Amount { get; set; }

        [Required]
        public DateOnly TransactionDate { get; set; }

        [Required]
        public List<SelectListItem> BankAccounts { get; set; }

        [Required]
        public int BankId { get; set; }

        [Required]
        [Display(Name = "Check #")]
        [RegularExpression(@"^(?:\d{10,}|DM\d{10})$", ErrorMessage = "Invalid format. Please enter either a 'DM' followed by a 10-digits or CV number minimum 10 digits.")]
        public string CheckNo { get; set; }

        [Required]
        public DateOnly CheckDate { get; set; }

        public string Particulars { get; set; }

        [Required]
        public List<SelectListItem> COA { get; set; }

        [Required]
        public string[] AccountNumber { get; set; }

        [Required]
        public string[] AccountTitle { get; set; }

        [Required]
        public decimal[] Debit { get; set; }

        [Required]
        public decimal[] Credit { get; set; }

        //others
        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public int NumberOfMonths { get; set; }

        public decimal AmountPerMonth { get; set; }

        public string CreatedBy { get; set; }
    }
}
