using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class ReceivingReportRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public ReceivingReportRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> GenerateRRNo()
        {
            var receivingReport = await _dbContext
                .ReceivingReports
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (receivingReport != null)
            {
                var generatedRR = receivingReport.Id + 1;
                return $"RR{generatedRR.ToString("D10")}";
            }
            else
            {
                return $"RR{1.ToString("D10")}";
            }
        }
    }
}