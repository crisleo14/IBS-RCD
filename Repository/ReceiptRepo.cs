﻿using Accounting_System.Data;
using Accounting_System.Models;
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

        public async Task<long> GetLastSeriesNumberCR()
        {
            var lastInvoice = await _dbContext
                .CollectionReceipts
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

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

        public async Task<long> GetLastSeriesNumberOR()
        {
            var lastInvoice = await _dbContext
                .OfficialReceipts
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

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

        public async Task<string> GenerateCRNo()
        {
            var collectionReceipt = await _dbContext
                .CollectionReceipts
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

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

        public async Task<string> GenerateORNo()
        {
            var officialReceipt = await _dbContext
                .OfficialReceipts
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (officialReceipt != null)
            {
                var generatedCR = officialReceipt.SeriesNumber + 1;
                return $"OR{generatedCR.ToString("D10")}";
            }
            else
            {
                return $"OR{1.ToString("D10")}";
            }
        }

        public async Task<List<CollectionReceipt>> GetCRAsync()
        {
            return await _dbContext
                .CollectionReceipts
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .OrderByDescending(cr => cr.Id)
                .ToListAsync();
        }

        public async Task<List<OfficialReceipt>> GetORAsync()
        {
            return await _dbContext
                .OfficialReceipts
                .Include(soa => soa.StatementOfAccount)
                .ThenInclude(s => s.Service)
                .Include(soa => soa.StatementOfAccount)
                .ThenInclude(c => c.Customer)
                .OrderByDescending(cr => cr.Id)
                .ToListAsync();
        }

        public async Task<CollectionReceipt> FindCR(int id)
        {
            var collectionReceipt = await _dbContext
                .CollectionReceipts
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .FirstOrDefaultAsync(collectionReceipt => collectionReceipt.Id == id);

            if (collectionReceipt != null)
            {
                return collectionReceipt;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<OfficialReceipt> FindOR(int id)
        {
            var officialReceipt = await _dbContext
                .OfficialReceipts
                .Include(or => or.StatementOfAccount)
                .ThenInclude(soa => soa.Customer)
                .Include(or => or.StatementOfAccount)
                .ThenInclude(soa => soa.Service)
                .FirstOrDefaultAsync(collectionReceipt => collectionReceipt.Id == id);

            if (officialReceipt != null)
            {
                return officialReceipt;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<int> UpdateInvoice(int id, decimal paidAmount, decimal offsetAmount)
        {
            var si = await _dbContext
                .SalesInvoices
                .FirstOrDefaultAsync(si => si.Id == id);

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

                return await _dbContext.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<int> UpdateSoa(int id, decimal paidAmount, decimal offsetAmount)
        {
            var soa = await _dbContext
                .StatementOfAccounts
                .FirstOrDefaultAsync(si => si.Id == id);

            if (soa != null)
            {
                var total = paidAmount + offsetAmount;
                soa.AmountPaid += total;
                soa.Balance = (soa.Total - soa.Discount) - soa.AmountPaid;

                if (soa.Balance == 0 && soa.AmountPaid == (soa.Total - soa.Discount))
                {
                    soa.IsPaid = true;
                    soa.Status = "Paid";
                }
                else if (soa.AmountPaid > (soa.Total - soa.Discount))
                {
                    soa.IsPaid = true;
                    soa.Status = "OverPaid";
                }

                return await _dbContext.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<List<Offsetting>> GetOffsettingAsync(string source, string reference)
        {
            var result = await _dbContext
                .Offsettings
                .Where(o => o.Source == source && o.Reference == reference)
                .ToListAsync();

            if (result != null)
            {
                return result;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }
    }
}