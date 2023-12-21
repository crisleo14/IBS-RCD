namespace Accounting_System.Models
{
    public class Offsetting : BaseEntity
    {
        public string AccountNo { get; set; }

        public string Source { get; set; }

        public string? Reference { get; set; }
    }
}