using Accounting_System.Models.AccountsPayable;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.ViewModels
{
    public class JournalVoucherViewModel
    {
        [Display(Name = "JV No")]
        public string? JVNo { get; set; }
        public int JVId { get; set; }

        [Display(Name = "Transaction Date")]
        public DateOnly TransactionDate { get; set; }

        public string? References { get; set; }

        [Display(Name = "CV Id")]
        public int? CVId { get; set; }

        public CheckVoucherHeader? CheckVoucherHeader { get; set; }

        [NotMapped]
        public List<SelectListItem>? CheckVoucherHeaders { get; set; }

        public string Particulars { get; set; }

        [Display(Name = "CR No")]
        public string? CRNo { get; set; }

        [Display(Name = "JV Reason")]
        public string JVReason { get; set; }

        [NotMapped]
        public List<SelectListItem>? COA { get; set; }

        public string[]? AccountNumber { get; set; } = null;

        public string[]? AccountTitle { get; set; } = null;

        public decimal[] Debit { get; set; } = new decimal[0];

        public decimal[] Credit { get; set; } = new decimal[0];

        //Ibs records
        public int? OriginalCVId { get; set; }
    }
}
