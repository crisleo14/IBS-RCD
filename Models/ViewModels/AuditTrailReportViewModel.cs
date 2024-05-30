using Accounting_System.Models.Reports;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class AuditTrailReportViewModel
    {
        public string TaxpayerName { get; set; }
        public string TIN { get; set; }
        public string Address { get; set; }
        public string AccountingSystem { get; set; }
        public string ControlNo { get; set; }
        public string DateIssued { get; set; }
        public List<AuditTrail> Records { get; set; }
        public string SoftwareName { get; set; }
        public string Version { get; set; }
        public string PrintedBy { get; set; }
        public string DateTimePrinted { get; set; }
    }
}