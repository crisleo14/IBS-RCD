using Accounting_System.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.AccountsPayable
{
    public class PurchaseOrder : BaseEntity
    {
        [Display(Name = "PO No")]
        [Column(TypeName = "varchar(12)")]
        public string? PONo { get; set; }

        public long SeriesNumber { get; set; }

        [Required]
        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        public DateOnly Date { get; set; }

        [Required]
        [Display(Name = "Supplier Name")]
        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }

        [NotMapped]
        public List<SelectListItem>? Suppliers { get; set; }

        public int SupplierNo { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        public string? ProductNo { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string Terms { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Quantity { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Price { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal? FinalPrice { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal QuantityReceived { get; set; }

        public bool IsReceived { get; set; }

        public DateTime ReceivedDate { get; set; }

        [Required]
        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        public bool IsClosed { get; set; }

        [NotMapped]
        public List<ReceivingReport>? RrList { get; set; }
    }
}