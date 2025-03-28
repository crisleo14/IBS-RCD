using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.MasterFile;

namespace Accounting_System.Models
{
    public class CheckVoucherDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [NotMapped]
        public List<SelectListItem>? DefaultExpenses { get; set; }

        public string AccountNo { get; set; } = " ";
        public string AccountName { get; set; } = " ";

        public string TransactionNo { get; set; } = " ";

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Debit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Credit { get; set; }

        public int? CVHeaderId { get; set; }

        [ForeignKey("CVHeaderId")]
        public CheckVoucherHeader? Header { get; set; }

        public int? OriginalDocumentId { get; set; }

        public int? SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        public bool IsVatable { get; set; }

        public decimal EwtPercent { get; set; }

        public bool IsUserSelected { get; set; }
    }
}
