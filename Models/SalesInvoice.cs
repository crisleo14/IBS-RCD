using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models
{
    public class SalesInvoice : BaseEntity
    {
        private static int lastSerialNo = 0;

        [Display(Name = "Serial No")]
        public long SerialNo { get; set; }

        [Display(Name = "Customer No")]
        public int CustomerId { get; set; }

        [Display(Name = "Sold To")]
        public string SoldTo { get; set; }

        public string Address { get; set; }

        [Display(Name = "Tin#")]
        public string TinNo { get; set; }

        [Display(Name = "Business Style")]
        public string BusinessStyle { get; set; }

        [Display(Name = "Transaction Date")]
        public string TransactionDate { get; set; }

        public string Terms { get; set; }

        [Display(Name = "Ref Dr No")]
        public string RefDrNo { get; set; }

        [Display(Name = "P.O No")]
        public string PoNo { get; set; }

        [Display(Name = "Product No")]
        public string ProductNo { get; set; }

        public decimal Quantity { get; set; }

        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        public decimal Amount { get; set; }

        public string Remarks { get; set; }

        public SalesInvoice() => SerialNo = ++lastSerialNo;
    }
}