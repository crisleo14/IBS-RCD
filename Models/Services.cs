using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class Services : BaseEntity
    {
        [Display(Name = "Service No")]
        public int Number { get; set; }

        [Display(Name = "Current and Previous")]
        [Column(TypeName = "varchar(50)")]
        public string CurrentAndPrevious { get; set; }

        [NotMapped]
        public List<SelectListItem>? CurrentAndPreviousTitles { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Unearned { get; set; }

        [NotMapped]
        public List<SelectListItem>? UnearnedTitle { get; set; }

        [Required]
        [Display(Name = "Service Name")]
        [Column(TypeName = "varchar(50)")]
        public string Name { get; set; }

        [Required]
        public int Percent { get; set; }
    }
}