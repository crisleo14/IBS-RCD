using System;
using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models
{
    public class Customer : BaseEntity
    {
        [Display(Name = "Customer Name")]
        public string Name { get; set; }

        [Display(Name = "Customer Address")]
        public string Address { get; set; }

        [Display(Name = "TIN No")]
        public string TinNo { get; set; }

        [Display(Name = "Business Style")]
        public string BusinessStyle { get; set; }

        [Display(Name = "Payment Terms")]
        public string Terms { get; set; }

        public CustomerType CustomerType { get; set; }

        public Customer()
        {
            // You can set default values here if needed
        }
    }

    public enum CustomerType
    {
        [Display(Name = "VATable")]
        Vatable,

        [Display(Name = "Exempt")]
        Excempt,

        [Display(Name = "Zero Rated")]
        ZeroRated
    }
}