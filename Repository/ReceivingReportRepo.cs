using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Utility;
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

        public async Task<string> GenerateRRNo(CancellationToken cancellationToken = default)
        {
            var receivingReport = await _dbContext
                .ReceivingReports
                .Where(rr => !rr.RRNo.StartsWith("RRBEG"))
                .OrderByDescending(s => s.RRNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (receivingReport != null)
            {
                string lastSeries = receivingReport.RRNo ?? throw new InvalidOperationException("RRNo is null pls Contact MIS Enterprise");
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0,2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return $"RR{1.ToString("D10")}";
            }
        }

        public async Task<string> GetPONoAsync(int? id, CancellationToken cancellationToken = default)
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

        public async Task UpdatePOAsync(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext.PurchaseOrders
                    .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

            if (po != null)
            {
                po.QuantityReceived += quantityReceived;

                if (po.QuantityReceived == po.Quantity)
                {
                    po.IsReceived = true;
                    po.ReceivedDate = DateTime.Now;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                if (po.QuantityReceived > po.Quantity)
                {
                    throw new ArgumentException("Input is exceed to remaining quantity received");
                }
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public async Task<int> RemoveQuantityReceived(int? id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext.PurchaseOrders
                    .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

            if (po != null)
            {
                po.QuantityReceived -= quantityReceived;

                if (po.IsReceived == true)
                {
                    po.IsReceived = false;
                    po.ReceivedDate = DateTime.MaxValue;
                }
                if (po.QuantityReceived > po.Quantity)
                {
                    throw new ArgumentException("Input is exceed to remaining quantity received");
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public async Task<DateOnly> ComputeDueDateAsync(int? poId, DateOnly rrDate, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext
                .PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == poId, cancellationToken);

            if (po != null)
            {
                DateOnly dueDate;

                switch (po.Terms)
                {
                    case "7D":
                        return dueDate = rrDate.AddDays(7);

                    case "10D":
                        return dueDate = rrDate.AddDays(7);

                    case "15D":
                        return dueDate = rrDate.AddDays(15);

                    case "30D":
                        return dueDate = rrDate.AddDays(30);

                    case "45D":
                    case "45PDC":
                        return dueDate = rrDate.AddDays(45);

                    case "60D":
                    case "60PDC":
                        return dueDate = rrDate.AddDays(60);

                    case "90D":
                        return dueDate = rrDate.AddDays(90);

                    case "M15":
                        return dueDate = rrDate.AddMonths(1).AddDays(15 - rrDate.Day);

                    case "M30":
                        if (rrDate.Month == 1)
                        {
                            dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-1);
                            }
                        }
                        return dueDate;

                    case "M29":
                        if (rrDate.Month == 1)
                        {
                            dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-2);
                            }
                            else if (dueDate.Day == 30)
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
                .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

            if (po != null)
            {
                return po;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<List<ReceivingReport>> GetReceivingReportsAsync(CancellationToken cancellationToken = default)
        {
            var rr = await _dbContext.ReceivingReports
                .Include(p => p.PurchaseOrder)
                .ThenInclude(s => s.Supplier)
                .Include(p => p.PurchaseOrder)
                .ThenInclude(prod => prod.Product)
                .ToListAsync(cancellationToken);

            if (rr != null)
            {
                return rr;
            }
            else
            {
                throw new ArgumentException("Error in get data of rr's.");
            }
        }

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy)
        {
            foreach (var change in changes)
            {
                var logReport = new ImportExportLog()
                {
                    Id = Guid.NewGuid(),
                    TableName = nameof(DynamicView.ReceivingReport),
                    DocumentRecordId = id,
                    ColumnName = change.Key,
                    Module = "Receiving Report",
                    OriginalValue = change.Value.OriginalValue,
                    AdjustedValue = change.Value.NewValue,
                    TimeStamp = DateTime.UtcNow.AddHours(8),
                    UploadedBy = modifiedBy
                };
                await _dbContext.AddAsync(logReport);
            }
        }
    }
}
