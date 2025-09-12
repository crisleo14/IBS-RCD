using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class ImportExportLog
    {
        [Key]
        [Display(Name = "Code")]
        public Guid Id { get; set; }

        [Display(Name = "Original Document Id")]
        public int DocumentRecordId { get; set; } // reference id

        [StringLength(100)]
        public string Module { get; set; }

        [Display(Name = "Table Name")]
        [StringLength(200)]
        public string TableName { get; set; } // reference

        [Display(Name = "Column Name")]
        [StringLength(200)]
        public string ColumnName { get; set; } // description

        [Display(Name = "Original Value")]
        [StringLength(2000)]
        public string? OriginalValue { get; set; }

        [Display(Name = "Adjusted Value")]
        [StringLength(2000)]
        public string? AdjustedValue { get; set; }

        [Display(Name = "Time Stamp")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime TimeStamp { get; set; }

        [Display(Name = "Uploaded By")]
        [StringLength(100)]
        public string? UploadedBy { get; set; }

        [StringLength(200)]
        public string Action { get; set; }

        public bool Executed { get; set; }

        [StringLength(13)]
        public string DocumentNo { get; set; } = string.Empty;

        [StringLength(10)]
        public string DatabaseName { get; set; } = string.Empty;
    }
}
