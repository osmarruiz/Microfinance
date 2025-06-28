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
    
    public class CollectionManagementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CollectionManagementsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: CollectionManagements
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.CollectionManagements.Include(c => c.Collector).Include(c => c.Loan).Where(c => !c.IsDeleted);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: CollectionManagements/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var collectionManagement = await _context.CollectionManagements
                .Include(c => c.Collector)
                .Include(c => c.Loan)
                .FirstOrDefaultAsync(m => m.CollectionId == id);
            if (collectionManagement == null)
            {
                return NotFound();
            }

            return View(collectionManagement);
        }

        // GET: CollectionManagements/Create
        [Authorize(Roles = "Admin,Salesperson")]
        public async Task<IActionResult> Create(int loanId, int installmentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var installment = await _context.Installments.FindAsync(installmentId); // Asume que tienes acceso al contexto
    
            ViewData["CollectorId"] = currentUser?.Id ?? "No identificado";
            ViewData["LoanId"] = loanId;
            ViewData["InstallmentId"] = installmentId;
            ViewData["PaidAmount"] = installment?.PaidAmount ?? 0; // Pasa el valor a la vista
    
            return View();
        }

// POST: CollectionManagements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Salesperson" )]
        public async Task<IActionResult> Create(
            [Bind("CollectionId,LoanId,CollectorId,ManagementResult,Notes,IsDeleted")]
            CollectionManagement collectionManagement,
            int? installmentId) // Recibimos el installmentId del formulario
        {
            if (ModelState.IsValid)
            {
                // Establecer fecha actual si no viene definida
                collectionManagement.ManagementDate = DateTime.UtcNow;

                _context.Add(collectionManagement);
                await _context.SaveChangesAsync();

                // Redirigir a registro de pago si corresponde
                if (collectionManagement.ManagementResult == "Pago parcial" ||
                    collectionManagement.ManagementResult == "Pago completo")
                {
                    if (installmentId != null)
                    {
                        return RedirectToAction("Create", "Payments", new
                        {
                            installmentId = installmentId.Value
                        });
                    }
                }
                
                return RedirectToAction("Details", "Installments", new { id = installmentId });
            }

            // Si hay errores, recargar la vista con los datos necesarios
            ViewData["CollectorId"] = collectionManagement.CollectorId;
            ViewData["LoanId"] = collectionManagement.LoanId;
            ViewData["InstallmentId"] = installmentId;
            return View(collectionManagement);
        }

        // POST: CollectionManagements/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id,
            [Bind("CollectionId,LoanId,CollectorId,ManagementDate,ManagementResult,Notes,IsDeleted")]
            CollectionManagement collectionManagement)
        {
            if (id != collectionManagement.CollectionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(collectionManagement);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CollectionManagementExists(collectionManagement.CollectionId))
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

            ViewData["CollectorId"] = new SelectList(_context.Users, "Id", "Id", collectionManagement.CollectorId);
            ViewData["LoanId"] = new SelectList(_context.Loans, "LoanId", "LoanId", collectionManagement.LoanId);
            return View(collectionManagement);
        }

        // GET: CollectionManagements/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var collectionManagement = await _context.CollectionManagements
                .Include(c => c.Collector)
                .Include(c => c.Loan)
                .FirstOrDefaultAsync(m => m.CollectionId == id);
            if (collectionManagement == null)
            {
                return NotFound();
            }

            return View(collectionManagement);
        }

        // POST: CollectionManagements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var collectionManagement = await _context.CollectionManagements.FindAsync(id);
            if (collectionManagement != null)
            {
                _context.CollectionManagements.Remove(collectionManagement);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CollectionManagementExists(int id)
        {
            return _context.CollectionManagements.Any(e => e.CollectionId == id);
        }
    }
}