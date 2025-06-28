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
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PaymentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Payments
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Payments.Include(p => p.Collector).Include(p => p.Installment).Where(p => !p.IsDeleted);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Payments/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Collector)
                .Include(p => p.Installment)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create(int installmentId)
        {
            var installment = _context.Installments
                .FirstOrDefault(i => i.InstallmentId == installmentId);

            if (installment == null)
            {
                return NotFound();
            }

            var collectorId = _userManager.GetUserId(User);
    
            ViewData["CollectorId"] = collectorId;
            ViewBag.Installment = installment; 
    
            return View();
        }

        // POST: Payments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Salesperson")]
        public async Task<IActionResult> Create(
            [Bind("PaymentId,InstallmentId,PaidAmount,Reference,CollectorId,IsDeleted")] Payment payment,
            int installmentId)
        {
            payment.PaymentDate = DateTime.UtcNow;
            if (ModelState.IsValid)
            {
                _context.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Installments", new { id = payment.InstallmentId });
            }

            var installment = _context.Installments
                .FirstOrDefault(i => i.InstallmentId == installmentId);
            
            var collectorId = _userManager.GetUserId(User);
    
            ViewData["CollectorId"] = collectorId;
            ViewBag.Installment = installment; 
            return View(payment);
        }

        // GET: Payments/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            ViewData["CollectorId"] = new SelectList(_context.Users, "Id", "Id", payment.CollectorId);
            ViewData["InstallmentId"] = new SelectList(_context.Installments, "InstallmentId", "InstallmentId",
                payment.InstallmentId);
            return View(payment);
        }

        // POST: Payments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id,
            [Bind("PaymentId,InstallmentId,PaymentDate,PaidAmount,Reference,CollectorId,IsDeleted")] Payment payment)
        {
            if (id != payment.PaymentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.PaymentId))
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

            ViewData["CollectorId"] = new SelectList(_context.Users, "Id", "Id", payment.CollectorId);
            ViewData["InstallmentId"] = new SelectList(_context.Installments, "InstallmentId", "InstallmentId",
                payment.InstallmentId);
            return View(payment);
        }

        // GET: Payments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Collector)
                .Include(p => p.Installment)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }
    }
}