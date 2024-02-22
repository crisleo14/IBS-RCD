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

        public async Task<long> GetLastSeriesNumber(CancellationToken cancellationToken = default)
        {
            var lastInvoice = await _dbContext
                .ReceivingReports
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastInvoice != null)
            {
                // Increment the last serial by one and return it
                return lastInvoice.SeriesNumber + 1;
            }
            else
            {
                // If there are no existing records, you can start with a default value like 1
                return 1;
            }
        }

        public async Task<string> GenerateRRNo(CancellationToken cancellationToken = default)
        {
            var receivingReport = await _dbContext
                .ReceivingReports
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (receivingReport != null)
            {
                var generatedRR = receivingReport.SeriesNumber + 1;
                return $"RR{generatedRR.ToString("D10")}";
            }
            else
            {
                return $"RR{1.ToString("D10")}";
            }
        }

        public async Task<string> GetPONoAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id != 0)
            {
                var po = await _dbContext
                                .PurchaseOrders
                                .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);
                return po.PONo;
            }
            else
            {
                throw new ArgumentException("No record found in supplier.");
            }
        }

        public async Task<int> UpdatePOAsync(int id, int quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext.PurchaseOrders
                    .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

            if (po != null)
            {
                po.QuantityReceived += quantityReceived;

                if (po.QuantityReceived >= po.Quantity)
                {
                    po.IsReceived = true;
                    po.ReceivedDate = DateTime.Now;
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public async Task<DateTime> ComputeDueDateAsync(int poId, DateTime rrDate, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext
                .PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == poId, cancellationToken);

            if (po != null)
            {
                DateTime dueDate;

                switch (po.Terms)
                {
                    case "7D":
                        return dueDate = rrDate.AddDays(7);

                    case "15D":
                        return dueDate = rrDate.AddDays(15);

                    case "30D":
                        return dueDate = rrDate.AddDays(30);

                    case "M15":
                        return dueDate = rrDate.AddMonths(1).AddDays(15 - rrDate.Day);

                    case "M30":
                        if (rrDate.Month == 1)
                        {
                            dueDate = new DateTime(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateTime(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-1);
                            }
                        }
                        return dueDate;

                    default:
                        return dueDate = rrDate;
                }
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public async Task<ReceivingReport> FindRR(int id, CancellationToken cancellationToken = default)
        {
            var rr = await _dbContext
                .ReceivingReports
                .Include(rr => rr.PurchaseOrder)
                .ThenInclude(po => po.Product)
                .Include(rr => rr.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
                .FirstOrDefaultAsync(rr => rr.Id == id, cancellationToken);

            if (rr != null)
            {
                return rr;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<PurchaseOrder> GetPurchaseOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext
                .PurchaseOrders
                .Include(po => po.Product)
                .Include(po => po.Supplier)
                .FirstOrDefaultAsync (po => po.Id == id, cancellationToken);

            if (po != null)
            {
                return po;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }

        }
    }
}