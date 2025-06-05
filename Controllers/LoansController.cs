using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microfinance.Data;
using Microfinance.Models.Business;
using Microsoft.AspNetCore.Identity;

namespace Microfinance.Controllers
{
    public class LoansController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LoansController(ApplicationDbContext context , UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Loans
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Loans.Include(l => l.Customer).Include(l => l.Seller);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Loans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .Include(l => l.Customer)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(m => m.LoanId == id);
            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        // GET: Loans/Create
        public async Task<IActionResult> Create()
        {
            // Obtener el usuario actual
            var currentUser = await _userManager.GetUserAsync(User);
    
            var model = new Loan
            {
                DueDate = DateTimeOffset.UtcNow.AddMonths(1),
                LoanStatus = LoanStatusEnum.Activo,
                Amount = 4000.00m,  // Monto predeterminado
                IsDeleted = false,
                InterestRate = 15.0m,
            };

            ViewData["CustomerId"] = new SelectList(_context.Customers.OrderBy(c => c.FullName), "CustomerId", "FullName");
    
            // Pasar el nombre del vendedor a la vista
            ViewData["CurrentSellerName"] = currentUser?.UserName ?? "No identificado";
            ViewData["CurrentSellerId"] = currentUser?.Id ?? "No identificado";

            return View(model);
        }

        // POST: Loans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("LoanId,CustomerId,SellerId,Amount,CurrentBalance,InterestRate,TermMonths,StartDate,DueDate,PaymentFrequency")] Loan loan)
        {
            var currentUser = await _userManager.GetUserAsync(User);
    
            // Asignar valores automáticos
            loan.LoanStatus = LoanStatusEnum.Activo;
            loan.IsDeleted = false;
    
            loan.SellerId = currentUser?.Id ?? string.Empty; // Asignar el ID del vendedor actual
            
            if (ModelState.IsValid)
            {
                try
                {
                    loan.CurrentBalance = loan.Amount; 
                    loan.DueDate = loan.DueDate.ToUniversalTime();
                    _context.Add(loan);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "Error al guardar: " + ex.InnerException?.Message);
                }
            }

            // Recargar datos para la vista
            ViewData["CustomerId"] = new SelectList(
                _context.Customers.OrderBy(c => c.FullName), 
                "CustomerId", 
                "FullName", 
                loan.CustomerId
            );
            ViewData["CurrentSellerName"] = currentUser?.UserName ?? "No identificado";
            ViewData["CurrentSellerId"] = currentUser?.Id ?? "No identificado";
    
            return View(loan);
        }

        // GET: Loans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans.FindAsync(id);
            if (loan == null)
            {
                return NotFound();
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "CustomerId", loan.CustomerId);
            ViewData["SellerId"] = new SelectList(_context.Users, "Id", "Id", loan.SellerId);
            return View(loan);
        }

        // POST: Loans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LoanId,CustomerId,SellerId,Amount,CurrentBalance,InterestRate,TermMonths,StartDate,DueDate,PaymentFrequency,LoanStatus,IsDeleted")] Loan loan)
        {
            if (id != loan.LoanId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanExists(loan.LoanId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "CustomerId", loan.CustomerId);
            ViewData["SellerId"] = new SelectList(_context.Users, "Id", "Id", loan.SellerId);
            return View(loan);
        }

        // GET: Loans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .Include(l => l.Customer)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(m => m.LoanId == id);
            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        // POST: Loans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loan = await _context.Loans.FindAsync(id);
            if (loan != null)
            {
                _context.Loans.Remove(loan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LoanExists(int id)
        {
            return _context.Loans.Any(e => e.LoanId == id);
        }
    }
}
