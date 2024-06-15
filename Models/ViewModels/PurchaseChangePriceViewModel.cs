using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models.ViewModels
{
    public class PurchaseChangePriceViewModel
    {
        public int POId { get; set; }

        public List<SelectListItem>? PO { get; set; }

        public decimal FinalPrice { get; set; }
    }
}
