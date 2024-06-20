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

        public InventoryRepo(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, JournalVoucherRepo journalVoucherRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _journalVoucherRepo = journalVoucherRepo;
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

        public async Task<bool> HasAlreadyBeginningInventory(int productId, int poId, CancellationToken cancellationToken)
        {
            return await _dbContext.Inventories
                .AnyAsync(i => i.ProductId == productId && i.POId == poId, cancellationToken);
        }

        public async Task AddBeginningInventory(BeginningInventoryViewModel viewModel, ClaimsPrincipal user, CancellationToken cancellationToken)
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

        public async Task AddPurchaseToInventoryAsync(ReceivingReport receivingReport, ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            var previousInventory = await _dbContext.Inventories
            .Where(i => i.ProductId == receivingReport.PurchaseOrder.Product.Id && i.POId == receivingReport.POId)
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .FirstOrDefaultAsync(cancellationToken);

            if (previousInventory != null)
            {
                Inventory inventory = new()
                {
                    Date = receivingReport.Date,
                    ProductId = receivingReport.PurchaseOrder.ProductId,
                    POId = receivingReport.POId,
                    Particular = "Purchases",
                    Reference = receivingReport.RRNo,
                    Quantity = receivingReport.QuantityReceived,
                    Cost = receivingReport.PurchaseOrder.Supplier.TaxType == "Vatable" ? receivingReport.PurchaseOrder.Price / 1.12m : receivingReport.PurchaseOrder.Price,
                    IsValidated = true,
                    ValidatedBy = _userManager.GetUserName(user),
                    ValidatedDate = DateTime.Now
                };

                inventory.Total = inventory.Quantity * inventory.Cost;
                inventory.InventoryBalance = previousInventory.InventoryBalance + inventory.Quantity;
                inventory.TotalBalance = previousInventory.TotalBalance + inventory.Total;
                inventory.AverageCost = inventory.TotalBalance / inventory.InventoryBalance;

                await _dbContext.AddAsync(inventory, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                Inventory inventory = new()
                {
                    Date = receivingReport.Date,
                    ProductId = receivingReport.PurchaseOrder.ProductId,
                    POId = receivingReport.POId,
                    Particular = "Purchases",
                    Reference = receivingReport.RRNo,
                    Quantity = receivingReport.QuantityReceived,
                    Cost = receivingReport.PurchaseOrder.Supplier.TaxType == "Vatable" ? receivingReport.PurchaseOrder.Price / 1.12m : receivingReport.PurchaseOrder.Price,
                    IsValidated = true,
                    ValidatedBy = _userManager.GetUserName(user),
                    ValidatedDate = DateTime.Now
                };

                inventory.Total = inventory.Quantity * inventory.Cost;
                inventory.InventoryBalance = inventory.Quantity;
                inventory.TotalBalance = inventory.Total;
                inventory.AverageCost = inventory.TotalBalance / inventory.InventoryBalance;

                await _dbContext.AddAsync(inventory, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task AddSalesToInventoryAsync(SalesInvoice salesInvoice, ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            var previousInventory = await _dbContext.Inventories
            .Where(i => i.ProductId == salesInvoice.Product.Id && i.POId == salesInvoice.POId)
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .FirstOrDefaultAsync(cancellationToken);

            if (previousInventory != null)
            {
                if (previousInventory.InventoryBalance < salesInvoice.Quantity)
                {
                    throw new InvalidOperationException($"The quantity exceeds the available inventory of '{salesInvoice.Product.Name}'.");
                }

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
                    ValidatedDate = DateTime.Now
                };

                inventory.Total = inventory.Quantity * inventory.Cost;
                inventory.InventoryBalance = previousInventory.InventoryBalance - inventory.Quantity;
                inventory.TotalBalance = previousInventory.TotalBalance - inventory.Total;
                inventory.AverageCost = inventory.TotalBalance / inventory.InventoryBalance;

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

                await _dbContext.Inventories.AddAsync(inventory, cancellationToken);
                await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"Beginning inventory for this product '{salesInvoice.Product.Name}' not found!");
            }
        }

        public async Task AddActualInventory(ActualInventoryViewModel viewModel, ClaimsPrincipal user, CancellationToken cancellationToken)
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

        public async Task ChangePriceToInventoryAsync(PurchaseChangePriceViewModel purchaseChangePriceViewModel, ClaimsPrincipal user, CancellationToken cancellationToken)
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

                decimal totalQuantity = previousInventoryList.Where(i => i.Reference != null).Sum(i => i.Quantity);
                decimal computeTotalQuantityWithPrice = totalQuantity * (purchaseChangePriceViewModel.FinalPrice - existingPO.Price);

                inventory.Total = computeTotalQuantityWithPrice / 1.12m;
                inventory.InventoryBalance = previousInventory.InventoryBalance + inventory.Quantity;
                inventory.TotalBalance = previousInventory.TotalBalance + inventory.Total;
                inventory.AverageCost = inventory.TotalBalance / inventory.InventoryBalance;

                decimal computeRRTotalAmount = findRR.Sum(rr => rr.Amount);
                decimal productAmount = computeTotalQuantityWithPrice < 0 ? computeTotalQuantityWithPrice / 1.12m : (computeRRTotalAmount + computeTotalQuantityWithPrice) / 1.12m;
                decimal vatInput = productAmount * 0.12m;
                decimal wht = productAmount * 0.01m;
                decimal apTradePayable = computeTotalQuantityWithPrice < 0 ? computeTotalQuantityWithPrice - wht : (computeRRTotalAmount + computeTotalQuantityWithPrice) - wht;

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
                        Particulars = "Change Price",
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
                            Amount = computeTotalQuantityWithPrice,
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
    }
}