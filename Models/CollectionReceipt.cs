

namespace Accounting_System.Models
{
    public class CollectionReceipt : BaseEntity
    {
        public int SalesInvoiceId { get; set; }

        public string CRNo { get; set; }

        public string Date { get; set; }

        public string ReferenceNo { get; set; }

        public string FormOfPayment { get; set; }

        public string CheckDate { get; set; }

        public int Check { get; set; }

        public string Bank { get; set; }

        public string Branch { get; set;}

        public decimal Amount { get; set;}

        public decimal EWT { get; set;}

        public decimal Total { get; set;}

        public bool IsPrint { get; set;}
    }
}
