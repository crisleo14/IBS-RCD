using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class OfficialReceipt : BaseEntity
    {
        public string? ORNo { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerNo { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [Required(ErrorMessage = "SOA is required.")]
        public int SOAId { get; set; }

        [Display(Name = "Statement Of Account No.")]
        [Column(TypeName = "varchar(13)")]
        public string? SOANo { get; set; }

        [ForeignKey("SOAId")]
        public ServiceInvoice? StatementOfAccount { get; set; }

        [NotMapped]
        public List<SelectListItem>? StatementOfAccounts { get; set; }

        [NotMapped]
        public List<SelectListItem>? ChartOfAccounts { get; set; }

        public DateTime Date { get; set; }

        public string ReferenceNo { get; set; }

        public string? Remarks { get; set; }

        //Cash
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal CashAmount { get; set; }

        //Check
        public DateTime? CheckDate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? CheckNo { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal CheckAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal EWT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal WVAT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Total { get; set; }

        public long SeriesNumber { get; set; }

        public bool IsCertificateUpload { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? F2306FilePath { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? F2307FilePath { get; set; }
    }
}