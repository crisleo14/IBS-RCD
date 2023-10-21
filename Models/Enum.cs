using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models
{
    public enum CustomerType
    {
        [Display(Name = "VATable")]
        Vatable,

        [Display(Name = "Exempt")]
        Excempt,

        [Display(Name = "Zero Rated")]
        ZeroRated
    }

    public enum CategoryType
    {
        Debit,
        Credit
    }
}