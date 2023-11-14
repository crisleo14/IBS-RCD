namespace Accounting_System.Models
{
    public class OfficialReceipt : BaseEntity
    {
        public int SOAId { get; set; }

        public string ORNo { get; set; }

        public string Date { get; set; }

        public string ReferenceNo { get; set; }

        public string FormOfPayment { get; set; }

        public int CheckNo { get; set; }

        public string CheckDate { get; set; }

        public decimal Amount { get; set; }

        public int SOADate { get; set; }

        public string SOANo { get; set; }

        public decimal SOAAmount { get; set; }

        public bool IsPrint { get; set; }
    }
}
