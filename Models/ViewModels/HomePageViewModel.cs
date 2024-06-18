namespace Accounting_System.Models.ViewModels
{
    public class HomePageViewModel
    {
        //Data store to show in Graph
        public List<int> SalesInvoice { get; set; }

        public List<int> ServiceInvoice { get; set; }

        public List<int> CollectionReceipt { get; set; }

        public List<int> DebitMemo { get; set; }

        public List<int> CreditMemo { get; set; }

        public List<int> PurchaseOrder { get; set; }

        public List<int> ReceivingReport { get; set; }

        public List<int> CheckVoucher { get; set; }

        public List<int> JournalVoucher { get; set; }

        //Data store to change the range of graph
        public int OverallMaxValue { get; set; }


        //Data strore to get the count of how many records each master file
        public int Customers { get; set; }

        public int Products { get; set; }

        public int Services { get; set; }

        public int Suppliers { get; set; }

        public int BankAccounts { get; set; }

        public int ChartOfAccount { get; set; }
    }
}