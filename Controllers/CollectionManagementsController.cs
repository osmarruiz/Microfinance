using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microfinance.Data;
using Microfinance.Models.Business;

namespace Microfinance.Controllers
{
    public class CollectionManagementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CollectionManagementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CollectionManagements
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.CollectionManagements.Include(c => c.Collector).Include(c => c.Loan);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: CollectionManagements/Details/5
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
        public IActionResult Create()
        {
            ViewData["CollectorId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["LoanId"] = new SelectList(_context.Loans, "LoanId", "LoanId");
            return View();
        }

        // POST: CollectionManagements/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CollectionId,LoanId,CollectorId,ManagementDate,ManagementResult,Notes,IsDeleted")] CollectionManagement collectionManagement)
        {
            if (ModelState.IsValid)
            {
                _context.Add(collectionManagement);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CollectorId"] = new SelectList(_context.Users, "Id", "Id", collectionManagement.CollectorId);
            ViewData["LoanId"] = new SelectList(_context.Loans, "LoanId", "LoanId", collectionManagement.LoanId);
            return View(collectionManagement);
        }

        // GET: CollectionManagements/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var collectionManagement = await _context.CollectionManagements.FindAsync(id);
            if (collectionManagement == null)
            {
                return NotFound();
            }
            ViewData["CollectorId"] = new SelectList(_context.Users, "Id", "Id", collectionManagement.CollectorId);
            ViewData["LoanId"] = new SelectList(_context.Loans, "LoanId", "LoanId", collectionManagement.LoanId);
            return View(collectionManagement);
        }

        // POST: CollectionManagements/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CollectionId,LoanId,CollectorId,ManagementDate,ManagementResult,Notes,IsDeleted")] CollectionManagement collectionManagement)
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
