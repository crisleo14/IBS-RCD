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
                .Include(s => s.Product)
                .Include(c => c.Customer)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetLastSeriesNumber(CancellationToken cancellationToken = default)
        {
            var lastInvoice = await _dbContext
                .SalesInvoices
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
                .Include(s => s.Product)
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

        public DateOnly ComputeDueDateAsync(string customerTerms, DateOnly date, CancellationToken cancellationToken = default)
        {
            if (customerTerms != null)
            {
                DateOnly dueDate;

                switch (customerTerms)
                {
                    case "7D":
                        return dueDate = date.AddDays(7);

                    case "10D":
                        return dueDate = date.AddDays(7);

                    case "15D":
                        return dueDate = date.AddDays(15);

                    case "30D":
                        return dueDate = date.AddDays(30);

                    case "45D":
                    case "45PDC":
                        return dueDate = date.AddDays(45);

                    case "60D":
                    case "60PDC":
                        return dueDate = date.AddDays(60);

                    case "90D":
                        return dueDate = date.AddDays(90);

                    case "M15":
                        return dueDate = date.AddMonths(1).AddDays(15 - date.Day);

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

                    case "M29":
                        if (date.Month == 1)
                        {
                            dueDate = new DateOnly(date.Year, date.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(date.Year, date.Month, 1).AddMonths(2).AddDays(-1);

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
                        return dueDate = date;
                }
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }
    }
}