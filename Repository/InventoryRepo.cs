using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.Reports;
using Accounting_System.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Accounting_System.Repository
{
    public class InventoryRepo
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JournalVoucherRepo _journalVoucherRepo;
        private readonly GeneralRepo _generalRepo;

        public InventoryRepo(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, JournalVoucherRepo journalVoucherRepo, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _journalVoucherRepo = journalVoucherRepo;
            _generalRepo = generalRepo;
        }

        //public async Task UpdateQuantity(decimal sold, int productId, CancellationToken cancellationToken = default)
        //{
        //    var inventory = await _dbContext
        //        .Inventories
        //        .FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        //    if (inventory == null)
        //    {
        //        throw new ArgumentException("No product found !");
        //    }
        //    inventory.QuantityServe += sold;
        //    inventory.QuantityBalance -= sold;

        //    await _dbContext.SaveChangesAsync(cancellationToken);
        //}

        public async Task<bool> HasAlreadyBeginningInventory(int productId, int poId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Inventories
                .AnyAsync(i => i.ProductId == productId && i.POId == poId, cancellationToken);
        }

        public async Task AddBeginningInventory(BeginningInventoryViewModel viewModel, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            Inventory inventory = new()
            {
                Date = viewModel.Date,
                ProductId = viewModel.ProductId,
                POId = viewModel.POId,
                Quantity = viewModel.Quantity,
                Cost = viewModel.Cost,
                Particular = "Beginning Balance",
                Total = viewModel.Quantity * viewModel.Cost,
                InventoryBalance = viewModel.Quantity,
                AverageCost = viewModel.Cost,
                TotalBalance = viewModel.Quantity * viewModel.Cost,
                IsValidated = true,
                ValidatedBy = _userManager.GetUserName(user),
                ValidatedDate = DateTime.Now
            };

            await _dbContext.Inventories.AddAsync(inventory, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task AddPurchaseToInventoryAsync(ReceivingReport receivingReport, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _dbContext.Inventories
            .Where(i => i.ProductId == receivingReport.PurchaseOrder.Product.Id && i.POId == receivingReport.POId)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

            var lastIndex = sortedInventory.FindLastIndex(s => s.Date <= receivingReport.Date);
            if (lastIndex >= 0)
            {
                sortedInventory = sortedInventory.Skip(lastIndex).ToList();
            }

            var previousInventory = sortedInventory.FirstOrDefault();

            decimal total = receivingReport.QuantityReceived * Math.Round(receivingReport.PurchaseOrder.Price / 1.12m, 2);
            decimal inventoryBalance = lastIndex >= 0 ? previousInventory.InventoryBalance + receivingReport.QuantityReceived : receivingReport.QuantityReceived;
            decimal totalBalance = lastIndex >= 0 ? previousInventory.TotalBalance + total : total;
            decimal averageCost = totalBalance / inventoryBalance;

            Inventory inventory = new()
            {
                Date = receivingReport.Date,
                ProductId = receivingReport.PurchaseOrder.ProductId,
                POId = receivingReport.POId,
                Particular = "Purchases",
                Reference = receivingReport.RRNo,
                Quantity = receivingReport.QuantityReceived,
                Cost = receivingReport.PurchaseOrder.Price / 1.12m, //unit cost
                IsValidated = true,
                ValidatedBy = _userManager.GetUserName(user),
                ValidatedDate = DateTime.Now,
                Total = total,
                InventoryBalance = inventoryBalance,
                TotalBalance = totalBalance,
                AverageCost = averageCost
            };


            foreach (var transaction in sortedInventory.Skip(1))
            {
                var costOfGoodsSold = 0m;
                if (transaction.Particular == "Sales")
                {
                    transaction.Cost = averageCost;
                    transaction.Total = transaction.Quantity * averageCost;
                    transaction.TotalBalance = totalBalance - transaction.Total;
                    transaction.InventoryBalance = inventoryBalance - transaction.Quantity;
                    transaction.AverageCost = transaction.TotalBalance / transaction.InventoryBalance;
                    costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                    averageCost = transaction.AverageCost;
                    totalBalance = transaction.TotalBalance;
                    inventoryBalance = transaction.InventoryBalance;
                }
                else if (transaction.Particular == "Purchases")
                {
                    transaction.TotalBalance = totalBalance + transaction.Total;
                    transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                    transaction.AverageCost = transaction.TotalBalance / transaction.InventoryBalance;

                    averageCost = transaction.AverageCost;
                    totalBalance = transaction.TotalBalance;
                    inventoryBalance = transaction.InventoryBalance;
                }

                var journalEntries = await _dbContext.GeneralLedgerBooks
                        .Where(j => j.Reference == transaction.Reference &&
                                    (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                        .ToListAsync(cancellationToken);

                if (journalEntries.Count != 0)
                {
                    foreach (var journal in journalEntries)
                    {
                        if (journal.Debit != 0)
                        {
                            if (journal.Debit != costOfGoodsSold)
                            {
                                journal.Debit = costOfGoodsSold;
                                journal.Credit = 0;
                            }
                        }
                        else
                        {
                            if (journal.Credit != costOfGoodsSold)
                            {
                                journal.Credit = costOfGoodsSold;
                                journal.Debit = 0;
                            }
                        }
                    }
                }

                _dbContext.GeneralLedgerBooks.UpdateRange(journalEntries);
            }

            _dbContext.Inventories.UpdateRange(sortedInventory);

            await _dbContext.AddAsync(inventory, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task AddSalesToInventoryAsync(SalesInvoice salesInvoice, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _dbContext.Inventories
            .Where(i => i.ProductId == salesInvoice.Product.Id && i.POId == salesInvoice.POId)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

            var lastIndex = sortedInventory.FindLastIndex(s => s.Date <= salesInvoice.TransactionDate);
            if (lastIndex >= 0)
            {
                sortedInventory = sortedInventory.Skip(lastIndex).ToList();
            }
            else
            {
                throw new ArgumentException($"Beginning inventory for {salesInvoice.Product.Name} not found!");
            }

            var previousInventory = sortedInventory.FirstOrDefault();

            if (previousInventory != null)
            {
                if (previousInventory.InventoryBalance < salesInvoice.Quantity)
                {
                    throw new InvalidOperationException($"The quantity exceeds the available inventory of '{salesInvoice.Product.Name}'.");
                }

                decimal total = salesInvoice.Quantity * previousInventory.AverageCost;
                decimal inventoryBalance = previousInventory.InventoryBalance - salesInvoice.Quantity;
                decimal totalBalance = previousInventory.TotalBalance - total;
                decimal averageCost = inventoryBalance == 0 && totalBalance == 0 ? previousInventory.AverageCost : totalBalance / inventoryBalance;

                Inventory inventory = new()
                {
                    Date = salesInvoice.TransactionDate,
                    ProductId = salesInvoice.Product.Id,
                    Particular = "Sales",
                    Reference = salesInvoice.SINo,
                    Quantity = salesInvoice.Quantity,
                    Cost = previousInventory.AverageCost,
                    POId = salesInvoice.POId,
                    IsValidated = true,
                    ValidatedBy = _userManager.GetUserName(user),
                    ValidatedDate = DateTime.Now,
                    Total = total,
                    InventoryBalance = inventoryBalance,
                    TotalBalance = totalBalance,
                    AverageCost = averageCost
                };


                foreach (var transaction in sortedInventory.Skip(1))
                {
                    var costOfGoodsSold = 0m;
                    if (transaction.Particular == "Sales")
                    {
                        transaction.Cost = averageCost;
                        transaction.Total = transaction.Quantity * averageCost;
                        transaction.TotalBalance = totalBalance - transaction.Total;
                        transaction.InventoryBalance = inventoryBalance - transaction.Quantity;
                        transaction.AverageCost = transaction.TotalBalance == 0 && transaction.InventoryBalance == 0 ? previousInventory.AverageCost : transaction.TotalBalance / transaction.InventoryBalance;
                        costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                        averageCost = transaction.AverageCost;
                        totalBalance = transaction.TotalBalance;
                        inventoryBalance = transaction.InventoryBalance;
                    }
                    else if (transaction.Particular == "Purchases")
                    {
                        transaction.TotalBalance = totalBalance + transaction.Total;
                        transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                        transaction.AverageCost = transaction.TotalBalance / transaction.InventoryBalance;

                        averageCost = transaction.AverageCost;
                        totalBalance = transaction.TotalBalance;
                        inventoryBalance = transaction.InventoryBalance;
                    }

                    var journalEntries = await _dbContext.GeneralLedgerBooks
                            .Where(j => j.Reference == transaction.Reference &&
                                        (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                            .ToListAsync(cancellationToken);

                    if (journalEntries.Count != 0)
                    {
                        foreach (var journal in journalEntries)
                        {
                            if (journal.Debit != 0)
                            {
                                if (journal.Debit != costOfGoodsSold)
                                {
                                    journal.Debit = costOfGoodsSold;
                                    journal.Credit = 0;
                                }
                            }
                            else
                            {
                                if (journal.Credit != costOfGoodsSold)
                                {
                                    journal.Credit = costOfGoodsSold;
                                    journal.Debit = 0;
                                }
                            }
                        }
                    }

                    _dbContext.GeneralLedgerBooks.UpdateRange(journalEntries);
                }

                var ledgers = new List<GeneralLedgerBook>
                {
                    new GeneralLedgerBook
                    {
                        Date = salesInvoice.TransactionDate,
                        Reference = salesInvoice.SINo,
                        Description = salesInvoice.Product.Name,
                        AccountNo = salesInvoice.Product.Code == "PET001" ? "5010101" : salesInvoice.Product.Code == "PET002" ? "5010102" : "5010103",
                        AccountTitle = salesInvoice.Product.Code == "PET001" ? "Cost of Goods Sold - Biodiesel" : salesInvoice.Product.Code == "PET002" ? "Cost of Goods Sold - Econogas" : "Cost of Goods Sold - Envirogas",
                        Debit = inventory.Total,
                        Credit = 0,
                        CreatedBy = salesInvoice.CreatedBy,
                        CreatedDate = salesInvoice.CreatedDate
                    },
                    new GeneralLedgerBook
                    {
                        Date = salesInvoice.TransactionDate,
                        Reference = salesInvoice.SINo,
                        Description = salesInvoice.Product.Name,
                        AccountNo = salesInvoice.Product.Code == "PET001" ? "1010401" : salesInvoice.Product.Code == "PET002" ? "1010402" : "1010403",
                        AccountTitle = salesInvoice.Product.Code == "PET001" ? "Inventory - Biodiesel" : salesInvoice.Product.Code == "PET002" ? "Inventory - Econogas" : "Inventory - Envirogas",
                        Debit = 0,
                        Credit = inventory.Total,
                        CreatedBy = salesInvoice.CreatedBy,
                        CreatedDate = salesInvoice.CreatedDate
                    }
                };

                _dbContext.Inventories.UpdateRange(sortedInventory);

                if (_generalRepo.IsDebitCreditBalanced(ledgers))
                {
                    await _dbContext.Inventories.AddAsync(inventory, cancellationToken);
                    await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }


            }
            else
            {
                throw new InvalidOperationException($"Beginning inventory for this product '{salesInvoice.Product.Name}' not found!");
            }
        }

        public async Task AddActualInventory(ActualInventoryViewModel viewModel, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            #region -- Actual Inventory Entry --

            var total = viewModel.Variance * viewModel.AverageCost;
            var inventoryBalance = viewModel.Variance + viewModel.PerBook;
            var totalBalance = viewModel.TotalBalance + total;
            var particular = viewModel.Variance < 0 ? "Actual Inventory(Loss)" : "Actual Inventory(Gain)";

            Inventory inventory = new()
            {
                Date = viewModel.Date,
                ProductId = viewModel.ProductId,
                Quantity = Math.Abs(viewModel.Variance),
                Cost = viewModel.AverageCost,
                Particular = particular,
                Total = Math.Abs(total),
                InventoryBalance = inventoryBalance,
                AverageCost = totalBalance / inventoryBalance,
                TotalBalance = totalBalance,
                POId = viewModel.POId,
            };
            await _dbContext.Inventories.AddAsync(inventory, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            #endregion -- Actual Inventory Entry --

            #region -- General Book Entry --

            var ledger = new List<GeneralLedgerBook>();
            for (int i = 0; i < viewModel.AccountNumber.Length; i++)
            {
                ledger.Add(
                    new GeneralLedgerBook
                    {
                        Date = viewModel.Date,
                        Reference = inventory.Id.ToString(),
                        AccountNo = viewModel.AccountNumber[i],
                        AccountTitle = viewModel.AccountTitle[i],
                        Description = particular,
                        Debit = Math.Abs(viewModel.Debit[i]),
                        Credit = Math.Abs(viewModel.Credit[i]),
                        CreatedBy = _userManager.GetUserName(user),
                        CreatedDate = DateTime.Now,
                        IsPosted = false
                    });
            }

            await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledger, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            #endregion -- General Book Entry --
        }

        public async Task ChangePriceToInventoryAsync(PurchaseChangePriceViewModel purchaseChangePriceViewModel, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            var existingPO = await _dbContext.PurchaseOrders
            .Include(po => po.Supplier)
            .FirstOrDefaultAsync(po => po.Id == purchaseChangePriceViewModel.POId, cancellationToken);


            var previousInventory = await _dbContext.Inventories
                .Where(i => i.ProductId == existingPO.ProductId && i.POId == purchaseChangePriceViewModel.POId)
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.Id)
                .Include(i => i.Product)
                .FirstOrDefaultAsync(cancellationToken);

            var previousInventoryList = await _dbContext.Inventories
                .Where(i => i.ProductId == existingPO.ProductId && i.POId == purchaseChangePriceViewModel.POId)
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            var findRR = await _dbContext.ReceivingReports
                .Where(rr => rr.POId == previousInventory.POId)
                .ToListAsync(cancellationToken);
            if (previousInventory != null && previousInventoryList.Any())
            {
                #region -- Inventory Entry --

                var generateJVNo = await _journalVoucherRepo.GenerateJVNo(cancellationToken);
                var getLastSeriesNumber = await _journalVoucherRepo.GetLastSeriesNumberJV(cancellationToken);

                Inventory inventory = new()
                {
                    Date = DateOnly.FromDateTime(DateTime.Now),
                    ProductId = existingPO.ProductId,
                    POId = purchaseChangePriceViewModel.POId,
                    Particular = "Change Price",
                    Reference = generateJVNo,
                    Quantity = 0,
                    Cost = 0,
                    IsValidated = true,
                    ValidatedBy = _userManager.GetUserName(user),
                    ValidatedDate = DateTime.Now
                };

                purchaseChangePriceViewModel.FinalPrice /= 1.12m;
                var newTotal = (purchaseChangePriceViewModel.FinalPrice - previousInventory.AverageCost) * previousInventory.InventoryBalance;

                inventory.Total = newTotal;
                inventory.InventoryBalance = previousInventory.InventoryBalance;
                inventory.TotalBalance = previousInventory.TotalBalance + newTotal;
                inventory.AverageCost = inventory.TotalBalance / inventory.InventoryBalance;

                decimal computeRRTotalAmount = findRR.Sum(rr => rr.Amount);
                decimal productAmount = newTotal < 0 ? newTotal / 1.12m : (computeRRTotalAmount + newTotal) / 1.12m;
                decimal vatInput = productAmount * 0.12m;
                decimal wht = productAmount * 0.01m;
                decimal apTradePayable = newTotal < 0 ? newTotal - wht : (computeRRTotalAmount + newTotal) - wht;

                #endregion -- Inventory Entry --

                #region -- Journal Voucher Entry --

                var journalVoucherHeader = new List<JournalVoucherHeader>
                {
                    new JournalVoucherHeader
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now),
                        JVNo = generateJVNo,
                        SeriesNumber = getLastSeriesNumber,
                        References = "",
                        Particulars = $"Change price of {existingPO.PONo} from {existingPO.Price} to {existingPO.FinalPrice }",
                        CRNo = "",
                        JVReason = "Change Price",
                        CreatedBy = _userManager.GetUserName(user),
                        CreatedDate = DateTime.Now,
                        IsPosted = true,
                        PostedDate = DateTime.Now
                    },
                };

                #region -- JV Detail Entry --

                if (inventory.Total > 0)
                {
                    #region -- JV Detail Entry --

                    var journalVoucherDetail = new List<JournalVoucherDetail>
                    {
                        new JournalVoucherDetail
                        {
                            AccountNo = previousInventory.Product.Code == "PET001" ? "1010401" : previousInventory.Product.Code == "PET002" ? "1010402" : "1010403",
                            AccountName = previousInventory.Product.Code == "PET001" ? "Inventory - Biodiesel" : previousInventory.Product.Code == "PET002" ? "Inventory - Econogas" : "Inventory - Envirogas",
                            TransactionNo = generateJVNo,
                            Debit = Math.Abs(productAmount),
                            Credit = 0
                        },
                        new JournalVoucherDetail
                        {
                            AccountNo = "1010602",
                            AccountName = "Vat Input",
                            TransactionNo = generateJVNo,
                            Debit = Math.Abs(vatInput),
                            Credit = 0
                        },
                        new JournalVoucherDetail
                        {
                            AccountNo = "2010302",
                            AccountName = "Expanded Witholding Tax 1%",
                            TransactionNo = generateJVNo,
                            Debit = 0,
                            Credit = Math.Abs(wht)
                        },
                        new JournalVoucherDetail
                        {
                            AccountNo = "2010101",
                            AccountName = "AP-Trade Payable",
                            TransactionNo = generateJVNo,
                            Debit = 0,
                            Credit = Math.Abs(apTradePayable)
                        }
                    };

                    await _dbContext.AddRangeAsync(journalVoucherDetail, cancellationToken);

                    #endregion -- JV Detail Entry --
                }
                else
                {
                    #region -- JV Detail Entry --

                    var journalVoucherDetail = new List<JournalVoucherDetail>
                    {
                        new JournalVoucherDetail
                        {
                            AccountNo = previousInventory.Product.Code == "PET001" ? "1010401" : previousInventory.Product.Code == "PET002" ? "1010402" : "1010403",
                            AccountName = previousInventory.Product.Code == "PET001" ? "Inventory - Biodiesel" : previousInventory.Product.Code == "PET002" ? "Inventory - Econogas" : "Inventory - Envirogas",
                            TransactionNo = generateJVNo,
                            Debit = 0,
                            Credit = Math.Abs(productAmount)
                        },
                        new JournalVoucherDetail
                        {
                            AccountNo = "1010602",
                            AccountName = "Vat Input",
                            TransactionNo = generateJVNo,
                            Debit = 0,
                            Credit = Math.Abs(vatInput)
                        },
                        new JournalVoucherDetail
                        {
                            AccountNo = "2010302",
                            AccountName = "Expanded Witholding Tax 1%",
                            TransactionNo = generateJVNo,
                            Debit = Math.Abs(wht),
                            Credit = 0
                        },
                        new JournalVoucherDetail
                        {
                            AccountNo = "2010101",
                            AccountName = "AP-Trade Payable",
                            TransactionNo = generateJVNo,
                            Debit = Math.Abs(apTradePayable),
                            Credit = 0
                        }
                    };

                    await _dbContext.AddRangeAsync(journalVoucherDetail, cancellationToken);

                    #endregion -- JV Detail Entry --
                }

                #endregion -- JV Detail Entry --

                await _dbContext.AddRangeAsync(journalVoucherHeader, cancellationToken);

                #endregion -- Journal Voucher Entry --

                #region -- Journal Book Entry --

                if (inventory.Total > 0)
                {
                    #region -- Journal Book Entry --

                    var journalBook = new List<JournalBook>
                    {
                        new JournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = previousInventory.Product.Code == "PET001" ? "1010401 Inventory - Biodiesel" : previousInventory.Product.Code == "PET002" ? "1010402 Inventory - Econogas" : "1010403 Inventory - Envirogas",
                            Debit = Math.Abs(productAmount),
                            Credit = 0,
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new JournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "1010602 Vat Input",
                            Debit = Math.Abs(vatInput),
                            Credit = 0,
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new JournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "2010302 Expanded Witholding Tax 1%",
                            Debit = 0,
                            Credit = Math.Abs(wht),
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new JournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "2010101 AP-Trade Payable",
                            Debit = 0,
                            Credit = Math.Abs(apTradePayable),
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        }
                    };
                    await _dbContext.AddRangeAsync(journalBook, cancellationToken);

                    #endregion -- Journal Book Entry --
                }
                else
                {
                    #region -- Journal Book Entry --

                    var journalBook = new List<JournalBook>
                    {
                        new JournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = previousInventory.Product.Code == "PET001" ? "1010401 Inventory - Biodiesel" : previousInventory.Product.Code == "PET002" ? "1010402 Inventory - Econogas" : "1010403 Inventory - Envirogas",
                            Debit = 0,
                            Credit = Math.Abs(productAmount),
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new JournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "1010602 Vat Input",
                            Debit = 0,
                            Credit = Math.Abs(vatInput),
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new JournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "2010302 Expanded Witholding Tax 1%",
                            Debit = Math.Abs(wht),
                            Credit = 0,
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new JournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "2010101 AP-Trade Payable",
                            Debit = Math.Abs(apTradePayable),
                            Credit = 0,
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        }
                    };
                    await _dbContext.AddRangeAsync(journalBook, cancellationToken);

                    #endregion -- Journal Book Entry --
                }

                #endregion -- Journal Book Entry --

                #region -- Purchase Book Entry --

                var purchaseBook = new List<PurchaseJournalBook>
                    {
                        new PurchaseJournalBook
                        {
                            Date = existingPO.Date,
                            SupplierName = existingPO.Supplier.Name,
                            SupplierTin = existingPO.Supplier.TinNo,
                            SupplierAddress = existingPO.Supplier.Address,
                            DocumentNo = "",
                            Description = existingPO.Product.Name,
                            Discount = 0,
                            VatAmount = inventory.Total * 0.12m,
                            Amount = newTotal,
                            WhtAmount = inventory.Total * 0.01m,
                            NetPurchases = inventory.Total,
                            PONo = existingPO.PONo,
                            DueDate = DateOnly.FromDateTime(DateTime.MinValue),
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        }
                    };

                #endregion -- Purchase Book Entry --

                #region -- General Book Entry --

                if (inventory.Total > 0)
                {
                    #region -- General Book Entry --

                    var ledgers = new List<GeneralLedgerBook>
                    {
                        new GeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = previousInventory.Product.Code == "PET001" ? "1010401" : previousInventory.Product.Code == "PET002" ? "1010402" : "1010403",
                            AccountTitle = previousInventory.Product.Code == "PET001" ? "Inventory - Biodiesel" : previousInventory.Product.Code == "PET002" ? "Inventory - Econogas" : "Inventory - Envirogas",
                            Debit = Math.Abs(productAmount),
                            Credit = 0,
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new GeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "1010602",
                            AccountTitle = "Vat Input",
                            Debit = Math.Abs(vatInput),
                            Credit = 0,
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new GeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "2010302",
                            AccountTitle = "Expanded Witholding Tax 1%",
                            Debit = 0,
                            Credit = Math.Abs(wht),
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new GeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "2010101",
                            AccountTitle = "AP-Trade Payable",
                            Debit = 0,
                            Credit = Math.Abs(apTradePayable),
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        }
                    };
                    await _dbContext.AddRangeAsync(ledgers, cancellationToken);

                    #endregion -- General Book Entry --
                }
                else
                {
                    #region -- General Book Entry --

                    var ledgers = new List<GeneralLedgerBook>
                    {
                        new GeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = previousInventory.Product.Code == "PET001" ? "1010401" : previousInventory.Product.Code == "PET002" ? "1010402" : "1010403",
                            AccountTitle = previousInventory.Product.Code == "PET001" ? "Inventory - Biodiesel" : previousInventory.Product.Code == "PET002" ? "Inventory - Econogas" : "Inventory - Envirogas",
                            Debit = 0,
                            Credit = Math.Abs(productAmount),
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new GeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "1010602",
                            AccountTitle = "Vat Input",
                            Debit = 0,
                            Credit = Math.Abs(vatInput),
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new GeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "2010302",
                            AccountTitle = "Expanded Witholding Tax 1%",
                            Debit = Math.Abs(wht),
                            Credit = 0,
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        },
                        new GeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "2010101",
                            AccountTitle = "AP-Trade Payable",
                            Debit = Math.Abs(apTradePayable),
                            Credit = 0,
                            CreatedBy = _userManager.GetUserName(user),
                            CreatedDate = DateTime.Now
                        }
                    };
                    await _dbContext.AddRangeAsync(ledgers, cancellationToken);

                    #endregion -- General Book Entry --
                }

                await _dbContext.AddRangeAsync(purchaseBook, cancellationToken);
                await _dbContext.AddAsync(inventory, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion -- General Book Entry --
            }
            else
            {
                throw new InvalidOperationException($"Beginning inventory for this product '{existingPO.Product.Name}' not found!");
            }
        }

        public async Task VoidInventory(Inventory model, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _dbContext.Inventories
            .Where(i => i.ProductId == model.ProductId && i.POId == model.POId && i.Date >= model.Date)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

            sortedInventory.Remove(model);

            if (sortedInventory.Count != 0)
            {
                var previousInventory = sortedInventory.FirstOrDefault();

                decimal total = previousInventory.Total;
                decimal inventoryBalance = previousInventory.InventoryBalance;
                decimal totalBalance = previousInventory.TotalBalance;
                decimal averageCost = inventoryBalance == 0 && totalBalance == 0 ? previousInventory.AverageCost : totalBalance / inventoryBalance;

                foreach (var transaction in sortedInventory.Skip(1))
                {
                    var costOfGoodsSold = 0m;
                    if (transaction.Particular == "Sales")
                    {
                        transaction.Cost = averageCost;
                        transaction.Total = transaction.Quantity * averageCost;
                        transaction.TotalBalance = totalBalance - transaction.Total;
                        transaction.InventoryBalance = inventoryBalance - transaction.Quantity;
                        transaction.AverageCost = transaction.TotalBalance == 0 && transaction.InventoryBalance == 0 ? previousInventory.AverageCost : transaction.TotalBalance / transaction.InventoryBalance;
                        costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                        averageCost = transaction.AverageCost;
                        totalBalance = transaction.TotalBalance;
                        inventoryBalance = transaction.InventoryBalance;

                        var journalEntries = await _dbContext.GeneralLedgerBooks
                            .Where(j => j.Reference == transaction.Reference &&
                                        (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                            .ToListAsync(cancellationToken);

                        if (journalEntries.Count != 0)
                        {
                            foreach (var journal in journalEntries)
                            {
                                if (journal.Debit != 0)
                                {
                                    if (journal.Debit != costOfGoodsSold)
                                    {
                                        journal.Debit = costOfGoodsSold;
                                        journal.Credit = 0;
                                    }
                                }
                                else
                                {
                                    if (journal.Credit != costOfGoodsSold)
                                    {
                                        journal.Credit = costOfGoodsSold;
                                        journal.Debit = 0;
                                    }
                                }
                            }
                        }

                        _dbContext.GeneralLedgerBooks.UpdateRange(journalEntries);

                    }
                    else if (transaction.Particular == "Purchases")
                    {
                        transaction.TotalBalance = totalBalance + transaction.Total;
                        transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                        transaction.AverageCost = transaction.TotalBalance / transaction.InventoryBalance;

                        averageCost = transaction.AverageCost;
                        totalBalance = transaction.TotalBalance;
                        inventoryBalance = transaction.InventoryBalance;
                    }
                }

                _dbContext.Inventories.UpdateRange(sortedInventory);
                _dbContext.Inventories.Remove(model);

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}