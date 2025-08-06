using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class Services
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ServiceId { get; set; }

        [Display(Name = "Service No")]
        public int ServiceNo { get; set; }

        [StringLength(20)]
        public string? CurrentAndPreviousNo { get; set; }

        [Display(Name = "Current and Previous")]
        [StringLength(50)]
        public string? CurrentAndPreviousTitle { get; set; }

        [NotMapped]
        public List<SelectListItem>? CurrentAndPreviousTitles { get; set; }

        [NotMapped]
        public int CurrentAndPreviousId { get; set; }

        [NotMapped]
        public int UnearnedId { get; set; }

        [StringLength(50)]
        public string? UnearnedTitle { get; set; }

        [StringLength(20)]
        public string? UnearnedNo { get; set; }

        [NotMapped]
        public List<SelectListItem>? UnearnedTitles { get; set; }

        [Required]
        [Display(Name = "Service Name")]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        public int Percent { get; set; }

        [Display(Name = "Created By")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? OriginalServiceId { get; set; }
    }
}
