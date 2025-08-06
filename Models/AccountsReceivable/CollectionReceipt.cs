using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.AccountsReceivable
{
    public class CollectionReceipt : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CollectionReceiptId { get; set; }

        [Display(Name = "Collection Receipt No.")]
        [StringLength(13)]
        public string? CollectionReceiptNo { get; set; }

        //Sales Invoice Property
        public int? SalesInvoiceId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        [StringLength(13)]
        public string? SINo { get; set; }

        public int[]? MultipleSIId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        public string[]? MultipleSI { get; set; }

        [ForeignKey(nameof(SalesInvoiceId))]
        public SalesInvoice? SalesInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? SalesInvoices { get; set; }

        //Service Invoice Property
        public int? ServiceInvoiceId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        [StringLength(13)]
        public string? SVNo { get; set; }

        [ForeignKey(nameof(ServiceInvoiceId))]
        public ServiceInvoice? ServiceInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? ServiceInvoices { get; set; }

        //Customer Property
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int? CustomerId { get; set; }

        //COA Property

        [NotMapped]
        public List<SelectListItem>? ChartOfAccounts { get; set; }

        [Required]
        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        public DateOnly TransactionDate { get; set; }

        [Display(Name = "Reference No")]
        [Required]
        [StringLength(50)]
        public string ReferenceNo { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        //Cash
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CashAmount { get; set; }

        //Check
        public string? CheckDate { get; set; }

        [StringLength(50)]
        public string? CheckNo { get; set; }

        [StringLength(50)]
        public string? CheckBank { get; set; }

        [StringLength(50)]
        public string? CheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CheckAmount { get; set; }

        //Manager's Check
        [Column(TypeName = "date")]
        public DateOnly? ManagerCheckDate { get; set; }

        [StringLength(50)]
        public string? ManagerCheckNo { get; set; }

        [StringLength(50)]
        public string? ManagerCheckBank { get; set; }

        [StringLength(50)]
        public string? ManagerCheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal ManagerCheckAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal EWT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal WVAT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Total { get; set; }

        public bool IsCertificateUpload { get; set; }

        [StringLength(200)]
        public string? F2306FilePath { get; set; }

        [StringLength(200)]
        public string? F2307FilePath { get; set; }

        [Column(TypeName = "numeric(18,4)[]")]
        public decimal[]? SIMultipleAmount { get; set; }

        public DateOnly[]? MultipleTransactionDate { get; set; }

        //Ibs records
        public int? OriginalSalesInvoiceId { get; set; }
        public int? OriginalServiceInvoiceId { get; set; }
        public int? OriginalCustomerId { get; set; }
    }
}
