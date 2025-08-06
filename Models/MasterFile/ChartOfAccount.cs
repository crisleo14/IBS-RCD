using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class ChartOfAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AccountId { get; set; }

        public bool IsMain { get; set; }

        [Display(Name = "Account Number")]
        [StringLength(15)]
        public string? AccountNumber { get; set; }

        [Display(Name = "Account Name")]
        [StringLength(100)]
        public string AccountName { get; set; }

        [StringLength(25)]
        public string? AccountType { get; set; }

        [StringLength(20)]
        public string? NormalBalance { get; set; }

        public int Level { get; set; }

        // Change Parent to an int? (nullable) for FK reference
        public int? ParentAccountId { get; set; }

        [StringLength(15)]
        public string? Parent { get; set; }
        // Navigation property for Parent Account
        [ForeignKey(nameof(ParentAccountId))]
        public virtual ChartOfAccount? ParentAccount { get; set; }

        // Navigation property for Child Accounts
        public virtual ICollection<ChartOfAccount> Children { get; set; } = new List<ChartOfAccount>();

        [NotMapped]
        public List<SelectListItem>? Main { get; set; }

        [Display(Name = "Created By")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow.AddHours(8);

        [Display(Name = "Edited By")]
        [StringLength(50)]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime EditedDate { get; set; }

        public int? OriginalChartOfAccountId { get; set; }

        public bool HasChildren { get; set; }

        // Select List

        [NotMapped]
        public List<SelectListItem>? Accounts { get; set; }
    }
}
