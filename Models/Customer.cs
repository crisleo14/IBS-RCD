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

        public string CustomerType { get; set; }
    }
}