using Accounting_System.Data;
using Accounting_System.Models.AccountsReceivable;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class SalesInvoiceRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public SalesInvoiceRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SalesInvoice>> GetSalesInvoicesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .SalesInvoices
                .Include(c => c.Customer)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetLastSeriesNumber(CancellationToken cancellationToken = default)
        {
            var lastInvoice = await _dbContext
                .SalesInvoices
                .Include(c => c.Customer)
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

        public async Task<string> GenerateSINo(CancellationToken cancellationToken = default)
        {
            var salesInvoice = await _dbContext
                .SalesInvoices
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (salesInvoice != null)
            {
                var generatedSI = salesInvoice.SeriesNumber + 1;
                return $"SI{generatedSI.ToString("D10")}";
            }
            else
            {
                return $"SI{1.ToString("D10")}";
            }
        }

        public async Task<SalesInvoice> FindSalesInvoice(int? id, CancellationToken cancellationToken = default)
        {
            var invoice = await _dbContext
                .SalesInvoices
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(invoice => invoice.Id == id);

            if (invoice != null)
            {
                return invoice;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<DateOnly> ComputeDueDateAsync(SalesInvoice model, DateOnly date)
        {

            if (model != null)
            {
                DateOnly dueDate;

                switch (model.Terms)
                {
                    case "7D":
                        return date.AddDays(7);

                    case "15D":
                        return date.AddDays(15);

                    case "30D":
                        return date.AddDays(30);

                    case "M15":
                        return date.AddMonths(1).AddDays(15 - date.Day);

                    case "M30":
                        if (date.Month == 1)
                        {
                            dueDate = new DateOnly(date.Year, date.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(date.Year, date.Month, 1).AddMonths(2).AddDays(-1);

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-1);
                            }
                        }
                        return dueDate;

                    default:
                        return date;
                }
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }
    }
}