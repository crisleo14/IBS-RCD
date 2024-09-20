using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class ReceiptRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public ReceiptRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<long> GetLastSeriesNumberCR(CancellationToken cancellationToken = default)
        {
            var lastInvoice = await _dbContext
                .CollectionReceipts
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

        public async Task<string> GenerateCRNo(CancellationToken cancellationToken = default)
        {
            var collectionReceipt = await _dbContext
                .CollectionReceipts
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (collectionReceipt != null)
            {
                var generatedCR = collectionReceipt.SeriesNumber + 1;
                return $"CR{generatedCR.ToString("D10")}";
            }
            else
            {
                return $"CR{1.ToString("D10")}";
            }
        }

        public async Task<List<CollectionReceipt>> GetCollectionReceiptsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .CollectionReceipts
                .Include(cr => cr.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .ToListAsync(cancellationToken);
        }

        public async Task<CollectionReceipt> FindCR(int id, CancellationToken cancellationToken = default)
        {
            var collectionReceipt = await _dbContext
                .CollectionReceipts
                .Include(cr => cr.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .FirstOrDefaultAsync(collectionReceipt => collectionReceipt.Id == id, cancellationToken);

            if (collectionReceipt != null)
            {
                return collectionReceipt;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<int> UpdateInvoice(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var si = await _dbContext
                .SalesInvoices
                .FirstOrDefaultAsync(si => si.Id == id, cancellationToken);

            if (si != null)
            {
                var total = paidAmount + offsetAmount;
                si.AmountPaid += total;
                si.Balance = si.NetDiscount - si.AmountPaid;

                if (si.Balance == 0 && si.AmountPaid == si.NetDiscount)
                {
                    si.IsPaid = true;
                    si.Status = "Paid";
                }
                else if (si.AmountPaid > si.NetDiscount)
                {
                    si.IsPaid = true;
                    si.Status = "OverPaid";
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }
        public async Task<int> UpdateMultipleInvoice(string[] siNo, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            if (siNo != null)
            {
                var salesInvoice = new SalesInvoice();
                for (int i = 0; i < siNo.Length; i++)
                {
                    var siValue = siNo[i];
                    salesInvoice = await _dbContext.SalesInvoices
                                .FirstOrDefaultAsync(p => p.SINo == siValue);

                    var amountPaid = salesInvoice.AmountPaid + paidAmount[i] + offsetAmount;

                    if (!salesInvoice.IsPaid)
                    {
                        salesInvoice.AmountPaid += salesInvoice.Amount >= amountPaid ? paidAmount[i] + offsetAmount : paidAmount[i];

                        salesInvoice.Balance = salesInvoice.NetDiscount - salesInvoice.AmountPaid;

                        if (salesInvoice.Balance == 0 && salesInvoice.AmountPaid == salesInvoice.NetDiscount)
                        {
                            salesInvoice.IsPaid = true;
                            salesInvoice.Status = "Paid";
                        }
                        else if (salesInvoice.AmountPaid > salesInvoice.NetDiscount)
                        {
                            salesInvoice.IsPaid = true;
                            salesInvoice.Status = "OverPaid";
                        }
                    }
                    else
                    {
                        continue;
                    }
                    if (salesInvoice.Amount >= amountPaid)
                    {
                        offsetAmount = 0;
                    }
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<int> RemoveSIPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var si = await _dbContext
                .SalesInvoices
                .FirstOrDefaultAsync(si => si.Id == id, cancellationToken);

            if (si != null)
            {
                var total = paidAmount + offsetAmount;
                si.AmountPaid -= total;
                si.Balance -= si.NetDiscount - total;

                if (si.IsPaid == true && si.Status == "Paid" || si.IsPaid == true && si.Status == "OverPaid")
                {
                    si.IsPaid = false;
                    si.Status = "Pending";
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }
        public async Task<int> RemoveSVPayment(int? id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var sv = await _dbContext
                .ServiceInvoices
                .FirstOrDefaultAsync(si => si.Id == id, cancellationToken);

            if (sv != null)
            {
                var total = paidAmount + offsetAmount;
                sv.AmountPaid -= total;
                sv.Balance -= (sv.Total - sv.Discount) - total;

                if (sv.IsPaid == true && sv.Status == "Paid" || sv.IsPaid == true && sv.Status == "OverPaid")
                {
                    sv.IsPaid = false;
                    sv.Status = "Pending";
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<int> UpdateSv(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var sv = await _dbContext
                .ServiceInvoices
                .FirstOrDefaultAsync(si => si.Id == id, cancellationToken);

            if (sv != null)
            {
                var total = paidAmount + offsetAmount;
                sv.AmountPaid += total;
                sv.Balance = (sv.Total - sv.Discount) - sv.AmountPaid;

                if (sv.Balance == 0 && sv.AmountPaid == (sv.Total - sv.Discount))
                {
                    sv.IsPaid = true;
                    sv.Status = "Paid";
                }
                else if (sv.AmountPaid > (sv.Total - sv.Discount))
                {
                    sv.IsPaid = true;
                    sv.Status = "OverPaid";
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<List<Offsetting>> GetOffsettingAsync(string source, string reference, CancellationToken cancellationToken = default)
        {
            var result = await _dbContext
                .Offsettings
                .Where(o => o.Source == source && o.Reference == reference)
                .ToListAsync(cancellationToken);

            if (result != null)
            {
                return result;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task RemoveMultipleSIPayment(int[] id, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var salesInvoices = await _dbContext
                .SalesInvoices
                .Where(si => id.Contains(si.Id))
                .ToListAsync(cancellationToken);

            if (salesInvoices != null)
            {
                for (int i = 0; i < paidAmount.Length; i++)
                {
                    var total = paidAmount[i] + offsetAmount;
                    salesInvoices[i].AmountPaid -= total;
                    salesInvoices[i].Balance += total;

                    if (salesInvoices[i].IsPaid == true && salesInvoices[i].Status == "Paid" || salesInvoices[i].IsPaid == true && salesInvoices[i].Status == "OverPaid")
                    {
                        salesInvoices[i].IsPaid = false;
                        salesInvoices[i].Status = "Pending";
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}