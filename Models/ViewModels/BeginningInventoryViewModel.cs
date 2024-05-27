using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models.ViewModels
{
    public class BeginningInventoryViewModel
    {
        [Required]t
        public string Date { get; set; }

        [Required]
        public string Particular { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Cost { get; set; }

        [Required]
        public int ProductId { get; set; }

        public List<SelectListItem>? ProductList { get; set; }

    }
}
