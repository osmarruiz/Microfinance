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
    [Authorize(Roles = "Admin")]  // Asegúrate de que el usuario tenga los roles necesarios
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AuditLogs
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.AuditLogs.Include(a => a.User);
            return View(await applicationDbContext.ToListAsync());
        }
    }
}
