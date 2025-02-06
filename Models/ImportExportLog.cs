using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models
{
    public class ImportExportLog
    {
        [Key]
        public Guid Id { get; set; }

        public int DocumentRecordId { get; set; } // reference id

        public string Module { get; set; }

        public string TableName { get; set; } // reference

        public string ColumnName { get; set; } // description

        public string? OriginalValue { get; set; }

        public string? AdjustedValue { get; set; }

        public DateTime TimeStamp { get; set; }

        public string? UploadedBy { get; set; }

        public string Action { get; set; }

        public bool Executed { get; set; }
    }
}
