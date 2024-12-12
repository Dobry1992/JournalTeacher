using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models.Elective;

namespace Portal.Controllers
{
    public class ElectiveTypesController : Controller
    {
        private readonly AcademyContext _context;

        public ElectiveTypesController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.ElectiveTypes.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ElectiveTypeID,Name,Archive")] ElectiveType electiveType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(electiveType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(electiveType);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var electiveType = await _context.ElectiveTypes.FindAsync(id);
            if (electiveType == null)
            {
                return NotFound();
            }
            return View(electiveType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ElectiveTypeID,Name,Archive")] ElectiveType electiveType)
        {
            if (id != electiveType.ElectiveTypeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(electiveType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ElectiveTypeExists(electiveType.ElectiveTypeID))
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
            return View(electiveType);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var electiveType = await _context.ElectiveTypes
                .FirstOrDefaultAsync(m => m.ElectiveTypeID == id);
            if (electiveType == null)
            {
                return NotFound();
            }

            return View(electiveType);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var electiveType = await _context.ElectiveTypes.FindAsync(id);
            _context.ElectiveTypes.Remove(electiveType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ElectiveTypeExists(int id)
        {
            return _context.ElectiveTypes.Any(e => e.ElectiveTypeID == id);
        }
    }
}
