﻿using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.MasterFile;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class Inventory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        public int? ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Particular { get; set; } // Beginning Inventory, Sales, Purchases

        [Column(TypeName = "varchar(12)")]
        public string? Reference { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Cost { get; set; }

        /// <summary>
        ///
        /// Formula : Quantity * Cost
        ///
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Total { get; set; }

        /// <summary>
        /// To compute inventory balance
        ///
        /// <para>Formula:</para>
        /// <para>
        ///     Purchases: PreviousInventoryBalance + InventoryBalance
        /// </para>
        /// <para>
        ///     Sales: PreviousInventoryBalance - InventoryBalance
        /// </para>
        ///
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Inventory Balance")]
        public decimal InventoryBalance { get; set; }

        /// <summary>
        ///
        /// <para>Formula: TotalBalance / InventoryBalance</para>
        ///
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Average Cost")]
        public decimal AverageCost { get; set; }

        /// <summary>
        /// To compute TotalBalance
        ///
        ///
        /// <para>Formula:</para>
        ///         <para>Purchases: PreviousTotalBalance + TotalBalance</para>
        ///         <para>Sales: PreviousTotalBalance - TotalBalance</para>
        ///
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Total Balance")]
        public decimal TotalBalance { get; set; }

        [Column(TypeName = "varchar(2)")]
        public string Unit { get; set; } = "L";

        public bool IsValidated { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? ValidatedBy { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? ValidatedDate { get; set; }

        public int? POId { get; set; }

        [ForeignKey("POId")]
        public PurchaseOrder? PurchaseOrder { get; set; }

        //Ibs records
        public int? OriginalProductId { get; set; }
        public int? OriginalPOId { get; set; }
    }
}