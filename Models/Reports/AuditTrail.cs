using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace Accounting_System.Models.Reports
{
    public class AuditTrail
    {
        public Guid Id { get; set; }

        [StringLength(100)]
        public string Username { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime Date { get; set; }

        [StringLength(500)]
        [Display(Name = "Machine Name")]
        public string MachineName { get; set; }

        [StringLength(2000)]
        public string Activity { get; set; }

        [StringLength(200)]
        [Display(Name = "Document Type")]
        public string DocumentType { get; set; }

        public AuditTrail()
        {

        }

        public AuditTrail(string username, string activity, string documentType, string ipAddress, DateTime? date = null)
        {
            Username = username;
            Date = date ?? DateTime.Now;

            // Attempt to resolve IP to hostname, fallback to IP if resolution fails
            try
            {
                var hostEntry = Dns.GetHostEntry(ipAddress);
                MachineName = hostEntry.HostName;
            }
            catch (Exception)
            {
                MachineName = ipAddress; // Fallback to IP address
            }

            Activity = activity;
            DocumentType = documentType;
        }
    }
}
