using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class ReceivingReport : BaseEntity
    {
        [Display(Name = "RR No")]
        [Column(TypeName = "varchar(12)")]
        public string? RRNo { get; set; }

        public long SeriesNumber { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DueDate { get; set; }

        [Display(Name = "PO No")]
        [Required]
        public int POId { get; set; }

        [ForeignKey("POId")]
        public PurchaseOrder? PurchaseOrder { get; set; }

        [NotMapped]
        public List<SelectListItem>? PurchaseOrders { get; set; }

        [Display(Name = "PO No")]
        [Column(TypeName = "varchar(12)")]
        public string? PONo { get; set; }

        [Display(Name = "Supplier Invoice#/Date")]
        [Column(TypeName = "varchar(100)")]
        public string InvoiceOrDate { get; set; }

        [Required]
        [Display(Name = "Truck/Vessels")]
        [Column(TypeName = "varchar(100)")]
        public string TruckOrVessels { get; set; }

        [Required]
        [Display(Name = "Qty Delivered")]
        public int QuantityDelivered { get; set; }

        [Required]
        [Display(Name = "Qty Received")]
        public int QuantityReceived { get; set; }

        [Display(Name = "Gain/Loss")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal? GainOrLoss { get; set; }

        [Display(Name = "Other Reference")]
        [Column(TypeName = "varchar(100)")]
        public string OtherRef { get; set; }

        [Required]
        public string Remarks { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal AmountPaid { get; set; }

        public bool IsPaid { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime PaidDate { get; set; }
    }
}