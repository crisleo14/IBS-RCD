using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace Accounting_System.Controllers
{
    public class JournalVoucherController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public JournalVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var headers = await _dbContext.journalVoucherHeaders
                .OrderByDescending(cv => cv.Id)
                .ToListAsync(cancellationToken);

            // Create a list to store CheckVoucherVM objectssw
            var journalVoucherVMs = new List<JournalVoucherVM>();

            // Retrieve details for each header
            foreach (var header in headers)
            {
                var headerJVNo = header.JVNo;
                var headerDetails = await _dbContext.journalVoucherDetails.Where(d => d.TransactionNo == headerJVNo).ToListAsync(cancellationToken);

                // Create a new CheckVoucherVM object for each header and its associated details
                var journalVoucherVM = new JournalVoucherVM
                {
                    Header = header,
                    Details = headerDetails
                };

                // Add the CheckVoucherVM object to the list
                journalVoucherVMs.Add(journalVoucherVM);
            }

            return View(journalVoucherVMs);
        }
        public IActionResult Create()
        {
            return View();
        }
        public IActionResult Print()
        {
            return View();
        }
    }
}
