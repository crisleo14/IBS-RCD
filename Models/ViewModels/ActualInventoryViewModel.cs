﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models.ViewModels
{
    public class ActualInventoryViewModel
    {
        [Required]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "The product field is required")]
        public int ProductId { get; set; }

        public List<SelectListItem>? ProductList { get; set; }

        [Display(Name = "Per Book")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal PerBook { get; set; }

        [Display(Name = "Per Book")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal AverageCost { get; set; }

        [Display(Name = "Per Book")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal TotalBalance { get; set; }

        [Required(ErrorMessage = "Cost must be greater than zero")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Actual Volume")]
        public decimal ActualVolume { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Variance { get; set; }

        public List<SelectListItem>? COA { get; set; }

        public string[] AccountNumber { get; set; }

        public string[] AccountTitle { get; set; }

        public decimal[] Debit { get; set; }

        public decimal[] Credit { get; set; }
    }
}