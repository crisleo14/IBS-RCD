using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Accounting_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(200)]
        public string FirstName { get; set; }

        [StringLength(200)]
        public string LastName { get; set; }
    }
}
