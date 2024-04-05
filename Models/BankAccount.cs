using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class BankAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Bank { get; set; }
        public string Branch { get; set; }

        public string? BankCode { get; set; }

        [Display(Name = "Account No")]
        public int AccountNo { get; set; }

        [Display(Name = "Acoount Name")]
        public string AccountName { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; } = "";

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
