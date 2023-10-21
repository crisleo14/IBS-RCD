namespace Accounting_System.Models
{
    public class SalesInvoiceVM
    {
        public IEnumerable<SalesInvoice> InvoiceViewModel { get; set; }
        public IEnumerable<Customer> CustomerViewModel { get; set; }
    }
}