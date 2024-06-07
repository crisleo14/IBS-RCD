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

        public InventoryRepo(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
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

        public async Task<bool> HasAlreadyBeginningInventory(int productId, CancellationToken cancellationToken)
        {
            return await _dbContext.Inventories
                .AnyAsync(i => i.ProductId == productId, cancellationToken);
        }

        public async Task AddBeginningInventory(BeginningInventoryViewModel viewModel, CancellationToken cancellationToken)
        {
            Inventory inventory = new()
            {
                Date = viewModel.Date,
                ProductId = viewModel.ProductId,
                Quantity = viewModel.Quantity,
                Cost = viewModel.Cost,
                Particular = "Beginning Balance",
                Total = viewModel.Quantity * viewModel.Cost,
                InventoryBalance = viewModel.Quantity,
                AverageCost = viewModel.Cost,
                TotalBalance = viewModel.Quantity * viewModel.Cost
            };

            await _dbContext.Inventories.AddAsync(inventory, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task AddPurchaseToInventoryAsync(ReceivingReport receivingReport, CancellationToken cancellationToken)
        {
            var previousInventory = await _dbContext.Inventories
                .OrderByDescending(i => i.Date)
                .FirstOrDefaultAsync(i => i.ProductId == receivingReport.PurchaseOrder.Product.Id, cancellationToken);

            if (previousInventory != null)
            {
                Inventory inventory = new()
                {
                    Date = receivingReport.Date,
                    ProductId = receivingReport.PurchaseOrder.ProductId,
                    Particular = "Purchases",
                    Reference = receivingReport.RRNo,
                    Quantity = receivingReport.QuantityReceived,
                    Cost = receivingReport.PurchaseOrder.Price / 1.12m
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
                throw new InvalidOperationException($"Beginning inventory for this product '{receivingReport.PurchaseOrder.Product.Id}' not found!");
            }
        }

        public async Task AddSalesToInventoryAsync(SalesInvoice salesInvoice, CancellationToken cancellationToken)
        {
            var previousInventory = await _dbContext.Inventories
                            .OrderByDescending(i => i.Date)
                            .ThenByDescending(i => i.Id)
                            .FirstOrDefaultAsync(i => i.ProductId == salesInvoice.Product.Id, cancellationToken);

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
                    Cost = previousInventory.AverageCost
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
                TotalBalance = totalBalance
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
                        Reference = "",
                        AccountNo = viewModel.AccountNumber[i],
                        AccountTitle = viewModel.AccountTitle[i],
                        Description = particular,
                        Debit = Math.Abs(viewModel.Debit[i]),
                        Credit = Math.Abs(viewModel.Credit[i]),
                        CreatedBy = _userManager.GetUserName(user),
                        CreatedDate = DateTime.Now,
                    });
            }

            await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledger, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            #endregion -- General Book Entry --
        }
    }
}