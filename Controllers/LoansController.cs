using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microfinance.Data;
using Microfinance.Models.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Microfinance.Controllers
{
    public class LoansController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LoansController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Loans
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isConsultant = await _userManager.IsInRoleAsync(user, "Consultant");
            
            ViewData["IsAdmin"] = isAdmin;

            IQueryable<Loan> loansQuery = _context.Loans
                .Include(l => l.Customer)
                .Include(l => l.Seller)
                .Where(l => !l.IsDeleted);

            if (!isAdmin && !isConsultant)
            {
                // Si no es administrador o consultor, filtrar por el vendedor actual
                loansQuery = loansQuery.Where(l => l.SellerId == user.Id);
            }

            return View(await loansQuery.ToListAsync());
        }

        // GET: Loans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var isAdmin = User.IsInRole("Admin");
            ViewData["IsAdmin"] = isAdmin;

            var loan = await _context.Loans
                .Include(l => l.Customer)
                .Include(l => l.Seller)
                .Include(l => l.Installments)
                .Include(l => l.CollectionManagements)
                .FirstOrDefaultAsync(m => m.LoanId == id);
            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        // GET: Loans/Create
        [Authorize(Roles = "Admin, Salesperson")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var model = new Loan
            {
                DueDate = DateTimeOffset.UtcNow.AddMonths(1),
                PrincipalAmount = 4000.00m,
                MonthlyInterestRate = 15.0m,
                SellerId = currentUser?.Id
            };

            ViewData["CustomerId"] = new SelectList(_context.Customers.Where(c => c.IsActive).OrderBy(c => c.FullName),
                "CustomerId", "FullName");
            ViewData["CurrentSellerName"] = currentUser?.UserName ?? "No identificado";

            return View(model);
        }

        // POST: Loans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Salesperson")]
        public async Task<IActionResult> Create(Loan loan)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            loan.SellerId = currentUser?.Id;

            if (ModelState.IsValid)
            {
                try
                {
                    // Calcular intereses normales
                    loan.NormalInterestAmount =
                        loan.PrincipalAmount * (loan.MonthlyInterestRate / 100) * loan.TermMonths;

                    // Asignar valores automáticos
                    loan.StartDate = DateTimeOffset.UtcNow;
                    loan.LoanStatus = "Activo";
                    loan.IsDeleted = false;
                    loan.LateInterestAmount = 0;

                    _context.Add(loan);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "Error al guardar: " + ex.InnerException?.Message);
                }
            }

            ViewData["CustomerId"] = new SelectList(
                _context.Customers.Where(c => c.IsActive).OrderBy(c => c.FullName),
                "CustomerId",
                "FullName",
                loan.CustomerId
            );
            ViewData["CurrentSellerName"] = currentUser?.UserName ?? "No identificado";

            return View(loan);
        }

        // GET: Loans/Edit/5
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id,
            [Bind(
                "LoanId,CustomerId,SellerId,Amount,CurrentBalance,InterestRate,TermMonths,StartDate,DueDate,PaymentFrequency,LoanStatus,IsDeleted")]
            Loan loan)
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
        [Authorize(Roles = "Admin")]
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

        // POST: Loans/Delete/5[HttpPost, ActionName("Delete")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loan = await _context.Loans
                .Include(l => l.Installments)
                .ThenInclude(i => i.Payments)
                .Include(l => l.CollectionManagements)
                .FirstOrDefaultAsync(l => l.LoanId == id);

            if (loan == null || loan.IsDeleted)
            {
                return NotFound();
            }

            // Solo permitir soft delete si el préstamo está cancelado
            if (loan.LoanStatus == LoanStatusEnum.Cancelado)
            {
                // Soft delete del préstamo
                loan.IsDeleted = true;

                // Soft delete en cascada de las cuotas relacionadas y sus pagos
                foreach (var installment in loan.Installments)
                {
                    installment.IsDeleted = true;

                    foreach (var payment in installment.Payments)
                    {
                        payment.IsDeleted = true;
                    }
                }

                // Soft delete en cascada de los registros de cobro
                foreach (var collection in loan.CollectionManagements)
                {
                    collection.IsDeleted = true;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si no está cancelado, mostrar error en la vista
            ViewData["ErrorMessage"] = "Solo se pueden eliminar préstamos con estado 'Cancelado'";
            return View("Delete", loan);
        }

        private bool LoanExists(int id)
        {
            return _context.Loans.Any(e => e.LoanId == id);
        }
    }
}