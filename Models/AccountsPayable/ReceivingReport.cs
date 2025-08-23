using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.AccountsPayable
{
    public class ReceivingReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReceivingReportId { get; set; }

        [Display(Name = "RR No")]
        [StringLength(13)]
        public string? ReceivingReportNo { get; set; }

        [Required]
        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }

        [Display(Name = "PO No.")]
        [Required]
        public int? POId { get; set; }

        [ForeignKey(nameof(POId))]
        public PurchaseOrder? PurchaseOrder { get; set; }

        [NotMapped]
        public List<SelectListItem>? PurchaseOrders { get; set; }

        [Display(Name = "PO No")]
        [StringLength(13)]
        public string? PONo { get; set; }

        [Display(Name = "Supplier Invoice#")]
        [StringLength(100)]
        public string? SupplierInvoiceNumber { get; set; }

        [Display(Name = "Supplier Invoice Date")]
        public string? SupplierInvoiceDate { get; set; }

        [Required]
        [Display(Name = "Truck/Vessels")]
        [StringLength(100)]
        public string TruckOrVessels { get; set; }

        [Required]
        [Display(Name = "Qty Delivered")]
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal QuantityDelivered { get; set; }

        [Required]
        [Display(Name = "Qty Received")]
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal QuantityReceived { get; set; }

        [Display(Name = "Gain/Loss")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal GainOrLoss { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Display(Name = "Other Reference")]
        [StringLength(1000)]
        public string? OtherRef { get; set; }

        [Required]
        [StringLength(1000)]
        public string Remarks { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        public bool IsPaid { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime PaidDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CanceledQuantity { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? ReceivedDate { get; set; }

        //Ibs records
        public int? OriginalPOId { get; set; }
    }
}
