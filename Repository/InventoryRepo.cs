using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class InventoryRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public InventoryRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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

        }

    }
}