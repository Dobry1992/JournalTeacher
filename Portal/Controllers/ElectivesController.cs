using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models.Elective;

namespace Portal.Controllers
{
    public class ElectivesController : Controller
    {
        private readonly AcademyContext _context;

        public ElectivesController(AcademyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Electives.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var elective = await _context.Electives
                .FirstOrDefaultAsync(m => m.ElectiveID == id);
            if (elective == null)
            {
                return NotFound();
            }

            return View(elective);
        }

        public IActionResult Create(int? id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ElectiveID,Name,ShortName,DepartmentID")] Elective elective, int id)
        {
            if (ModelState.IsValid)
            {
                elective.DepartmentID = id;
                _context.Add(elective);
                await _context.SaveChangesAsync();
                return RedirectToAction("ChooseDepartment", "Departments");
            }
            return View(elective);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var elective = await _context.Electives.FindAsync(id);
            if (elective == null)
            {
                return NotFound();
            }
            return View(elective);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ElectiveID,Name,ShortName,DepartmentID")] Elective elective)
        {
            if (id != elective.ElectiveID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(elective);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ElectiveExists(elective.ElectiveID))
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
            return View(elective);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var elective = await _context.Electives
                .FirstOrDefaultAsync(m => m.ElectiveID == id);
            if (elective == null)
            {
                return NotFound();
            }

            return View(elective);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var elective = await _context.Electives.FindAsync(id);
            _context.Electives.Remove(elective);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ElectiveExists(int id)
        {
            return _context.Electives.Any(e => e.ElectiveID == id);
        }
    }
}
