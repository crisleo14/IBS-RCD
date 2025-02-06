using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.AccountsReceivable
{
    public class ServiceInvoice : BaseEntity
    {
        [Column(TypeName = "varchar(12)")]
        public string? SVNo { get; set; }

        [Display(Name = "Customer")]
        [Required(ErrorMessage = "The Customer is required.")]
        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [Required(ErrorMessage = "The Service is required.")]
        [Display(Name = "Particulars")]
        public int? ServicesId { get; set; }

        [ForeignKey("ServicesId")]
        public Services? Service { get; set; }

        public int ServiceNo { get; set; }

        [NotMapped]
        public List<SelectListItem>? Services { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateOnly Period { get; set; }

        [Required(ErrorMessage = "The Amount is required.")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Total { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Discount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CurrentAndPreviousAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal UnearnedAmount { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "Pending";

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Balance { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? Instructions { get; set; }

        public bool IsPaid { get; set; }

        //Ibs records
        public int? OriginalCustomerId { get; set; }
        public int? OriginalServicesId { get; set; }
    }
}
