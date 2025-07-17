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

namespace Microfinance.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            var isAdmin = User.IsInRole("Admin");

            ViewData["IsAdmin"] = isAdmin;
            var customers = await _context.Customers
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(m => m.Loans)
                .FirstOrDefaultAsync(m => m.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        [Authorize(Roles = "Admin, Salesperson")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Salesperson")]
        public async Task<IActionResult> Create(
            [Bind("CustomerId,FullName,IdCard,PhoneNumber,Address,Email,IsActive,IsDeleted")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("CustomerId,FullName,IdCard,PhoneNumber,Address,Email,IsActive")]
            Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Obtener el cliente existente SIN rastreo (AsNoTracking)
                    var existingCustomer = await _context.Customers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CustomerId == id);

                    if (existingCustomer == null)
                    {
                        return NotFound();
                    }

                    // Adjuntar el cliente modificado al contexto
                    _context.Attach(customer);

                    // Marcar como modificado SOLO las propiedades que queremos actualizar
                    _context.Entry(customer).Property(x => x.FullName).IsModified = true;
                    _context.Entry(customer).Property(x => x.IdCard).IsModified = true;
                    _context.Entry(customer).Property(x => x.PhoneNumber).IsModified = true;
                    _context.Entry(customer).Property(x => x.Address).IsModified = true;
                    _context.Entry(customer).Property(x => x.Email).IsModified = true;
                    _context.Entry(customer).Property(x => x.IsActive).IsModified = true;

                    // IsDeleted NO se marca como modificado, por lo que no se actualizará

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(customer);
        }

        // GET: Customers/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.Loans.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            // Pasar información sobre préstamos activos a la vista
            ViewBag.HasActiveLoans = customer.Loans.Any();
            ViewBag.ActiveLoansCount = customer.Loans.Count;
            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Añadido para consistencia con el otro método
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Loans.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.Installments)
                .ThenInclude(i => i.Payments)
                .Include(c => c.Loans.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.CollectionManagements)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null || customer.IsDeleted)
            {
                return NotFound();
            }

            if (customer.Loans.Any(l => l.LoanStatus != LoanStatusEnum.Cancelado))
            {
                int activeLoansCount = customer.Loans.Count(l => l.LoanStatus != LoanStatusEnum.Cancelado);
                TempData["ErrorMessage"] =
                    $"No se puede eliminar el cliente porque tiene {activeLoansCount} préstamo(s) no cancelado(s). " +
                    "Primero debe cancelar todos sus préstamos.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Soft delete del cliente
            customer.IsDeleted = true;

            // Soft delete en cascada de los préstamos y sus relaciones
            foreach (var loan in customer.Loans)
            {
                loan.IsDeleted = true;

                foreach (var installment in loan.Installments)
                {
                    installment.IsDeleted = true;

                    foreach (var payment in installment.Payments)
                    {
                        payment.IsDeleted = true;
                    }
                }

                foreach (var collection in loan.CollectionManagements)
                {
                    collection.IsDeleted = true;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cliente eliminado correctamente (soft delete).";
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}