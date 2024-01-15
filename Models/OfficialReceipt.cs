using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class OfficialReceipt : BaseEntity
    {
        public int SOAId { get; set; }

        [ForeignKey("SOAId")]
        public StatementOfAccount? StatementOfAccount { get; set; }

        public string? ORNo { get; set; }

        [NotMapped]
        public List<SelectListItem>? SOANo { get; set; }

        //COA Property

        [NotMapped]
        public List<SelectListItem>? ChartOfAccounts { get; set; }

        public long SeriesNumber { get; set; }

        public DateTime Date { get; set; }

        public string ReferenceNo { get; set; }

        public string? Remarks { get; set; }

        //Cash
        public decimal CashAmount { get; set; }

        //Check
        public DateTime CheckDate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? CheckNo { get; set; }


        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal CheckAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal EWT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal WVAT { get; set; }
    }
}