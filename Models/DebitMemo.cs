using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class DebitMemo : BaseEntity
    {
        [Display(Name = "SI No")]
        public int SalesInvoiceId { get; set; }
        [ForeignKey("SalesInvoiceId")]
        public SalesInvoice? SalesInvoice { get; set; }
        [NotMapped]
        public List<SelectListItem>? SalesInvoices { get; set; }


        [Display(Name = "SOA No")]
        public int SOAId { get; set; }
        [ForeignKey("SOAId")]
        public StatementOfAccount? SOA { get; set; }
        [NotMapped]
        public List<SelectListItem>? StatementOfAccounts { get; set; }


        public string? DMNo { get; set; }
        public DateTime Date {  get; set; }
        [Display(Name = "Debit Amount")]
        public decimal DebitAmount {  get; set; }
        public string Description { get; set; }
        [Display(Name = "Vatable Sales")]
        public decimal VatableSales {  get; set; }
        public decimal VatAmount { get; set; }
        public decimal Amount { get; set; }
        public bool IsPrinted { get; set; }
        [Display(Name = "Adjusted Price")]
        public decimal AdjustedPrice { get; set; }
        public string Source { get; set; } = " ";

    }
}
