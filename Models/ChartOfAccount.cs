using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class ChartOfAccount : BaseEntity
    {
        public bool IsMain { get; set; }

        [Display(Name = "Account Number")]
        public string? Number { get; set; }

        [Display(Name = "Account Name")]
        public string Name { get; set; }

        public string? Type { get; set; }
        public string? Category { get; set; }
        public string? Level { get; set; } = string.Empty;

        public string? Parent { get; set; }

        [NotMapped]
        public List<SelectListItem>? Main { get; set; }
    }
}