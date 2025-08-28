using Accounting_System.Data;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<AasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AASConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireUppercase = false;
});

builder.Services.AddScoped<SalesInvoiceRepo>();
builder.Services.AddScoped<CustomerRepo>();
builder.Services.AddScoped<ReportRepo>();
builder.Services.AddScoped<InventoryRepo>();
builder.Services.AddScoped<ReceiptRepo>();
builder.Services.AddScoped<ServiceInvoiceRepo>();
builder.Services.AddScoped<PurchaseOrderRepo>();
builder.Services.AddScoped<DebitMemoRepo>();
builder.Services.AddScoped<ServiceRepo>();
builder.Services.AddScoped<ReceivingReportRepo>();
builder.Services.AddScoped<CreditMemoRepo>();
builder.Services.AddScoped<SupplierRepo>();
builder.Services.AddScoped<CheckVoucherRepo>();
builder.Services.AddScoped<ChartOfAccountRepo>();
builder.Services.AddScoped<GeneralRepo>();
builder.Services.AddScoped<BankAccountRepo>();
builder.Services.AddScoped<JournalVoucherRepo>();
builder.Services.AddHostedService<AutomatedEntries>();
builder.Services.AddScoped<ProductRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
