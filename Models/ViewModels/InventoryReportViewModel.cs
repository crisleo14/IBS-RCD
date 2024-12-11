using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models.ViewModels
{
    public class InventoryReportViewModel /*: IValidatableObject = Injecting for validation */
    {
        public List<SelectListItem>? Products { get; set; }

        [Required(ErrorMessage = "Product is required")]
        public int ProductId { get; set; }

        [Display(Name = "Date To")]
        [Required]
        public DateOnly DateTo { get; set; }

        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
        //    if (DateFrom > DateTo)
        //    {
        //        yield return new ValidationResult("Date From must be less than or equal Date To", new[] { nameof(DateFrom), nameof(DateTo) });
        //    }
        //}
    }
}
