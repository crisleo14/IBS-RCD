using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class DebitMemo : BaseEntity
    {
        public int SalesInvoiceId { get; set; }

        [ForeignKey("SalesInvoiceId")]
        public SalesInvoice? SalesInvoice { get; set; }
        [NotMapped]
        public List<SelectListItem>? SalesInvoices { get; set; }

        public string? DMNo { get; set; }
        public string Date {  get; set; }
        public decimal DebitAmount {  get; set; }
        public string Description { get; set; }
        public decimal VatableSales {  get; set; }
        public decimal VatAmount { get; set; }
        public decimal Amount { get; set; }
        public bool IsPrinted { get; set; }
       
    }
}
