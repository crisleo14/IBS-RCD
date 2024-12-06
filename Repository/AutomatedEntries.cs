using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class AutomatedEntries : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;

        public AutomatedEntries(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1)); // Change interval as needed
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            if (31 == DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var _journalVoucherRepo = scope.ServiceProvider.GetRequiredService<JournalVoucherRepo>();

                    var cvList = await _dbContext.CheckVoucherHeaders
                            .Where(cv => cv.StartDate <= DateOnly.FromDateTime(DateTime.Now) && (cv.LastCreatedDate < DateTime.Now || cv.LastCreatedDate == null) && !cv.IsComplete && cv.IsPosted)
                            .ToListAsync();

                    if (cvList.Count != 0)
                    {
                        foreach (var cv in cvList)
                        {
                            var accountEntries = await _dbContext.CheckVoucherDetails
                                .Where(cvd => cvd.TransactionNo == cv.CVNo && (cvd.AccountNo.StartsWith("10201") || cvd.AccountNo.StartsWith("10105")))
                                .ToListAsync();

                            foreach (var accountEntry in accountEntries)
                            {
                                if (accountEntry.AccountNo.StartsWith("10201"))
                                {
                                    cv.NumberOfMonthsCreated++;
                                    cv.LastCreatedDate = DateTime.Now;

                                    if (cv.NumberOfMonths == cv.NumberOfMonthsCreated)
                                    {
                                        cv.IsComplete = true;
                                    }

                                    var header = new JournalVoucherHeader
                                    {
                                        JVNo = await _journalVoucherRepo.GenerateJVNo(),
                                        CVId = cv.Id,
                                        JVReason = "Depreciation",
                                        Particulars = $"Depreciation of : CV Particulars {cv.Particulars} for the month of {DateTime.Now:MMMM yyyy}.",
                                        Date = DateOnly.FromDateTime(DateTime.Now),
                                        CreatedBy = cv.CreatedBy,
                                        CreatedDate = DateTime.Now
                                    };

                                    var details = new List<JournalVoucherDetail>();

                                    details.Add(new JournalVoucherDetail
                                    {
                                        AccountNo = "5020107",
                                        AccountName = "Depreciation Expense",
                                        TransactionNo = header.JVNo,
                                        Debit = cv.AmountPerMonth,
                                        Credit = 0
                                    });

                                    details.Add(new JournalVoucherDetail
                                    {
                                        AccountNo = accountEntry.AccountName.Contains("Building") ? "1020104" : "1020105",
                                        AccountName = accountEntry.AccountName.Contains("Building") ? "Accummulated Depreciation - Building and improvements" : "Accummulated Depreciation - Equipment",
                                        TransactionNo = header.JVNo,
                                        Debit = 0,
                                        Credit = cv.AmountPerMonth
                                    });

                                    await _dbContext.AddAsync(header);
                                    await _dbContext.AddRangeAsync(details);
                                    await _dbContext.SaveChangesAsync();
                                }
                                else if (accountEntry.AccountNo.StartsWith("10105"))
                                {
                                    //Prepaid
                                    cv.NumberOfMonthsCreated++;
                                    cv.LastCreatedDate = DateTime.Now;

                                    if (cv.NumberOfMonths == cv.NumberOfMonthsCreated)
                                    {
                                        cv.IsComplete = true;
                                    }

                                    var header = new JournalVoucherHeader
                                    {
                                        JVNo = await _journalVoucherRepo.GenerateJVNo(),
                                        CVId = cv.Id,
                                        JVReason = "Prepaid",
                                        Particulars = $"Prepaid: CV Particulars {cv.Particulars} for the month of {DateTime.Now:MMMM yyyy}.",
                                        Date = DateOnly.FromDateTime(DateTime.Now),
                                        CreatedBy = cv.CreatedBy,
                                        CreatedDate = DateTime.Now
                                    };

                                    var details = new List<JournalVoucherDetail>();

                                    details.Add(new JournalVoucherDetail
                                    {
                                        AccountNo = "5020115",
                                        AccountName = "Rental Expense",
                                        TransactionNo = header.JVNo,
                                        Debit = cv.AmountPerMonth,
                                        Credit = 0
                                    });

                                    details.Add(new JournalVoucherDetail
                                    {
                                        AccountNo = "1010501",
                                        AccountName = "Prepaid Expenses - Rental",
                                        TransactionNo = header.JVNo,
                                        Debit = 0,
                                        Credit = cv.AmountPerMonth
                                    });

                                    await _dbContext.AddAsync(header);
                                    await _dbContext.AddRangeAsync(details);
                                    await _dbContext.SaveChangesAsync();
                                }
                                else
                                {
                                    //Accrued
                                }
                            }
                        }
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}