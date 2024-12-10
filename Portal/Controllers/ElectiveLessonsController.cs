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
    public class ElectiveLessonsController : Controller
    {
        private readonly AcademyContext _context;

        public ElectiveLessonsController(AcademyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var academyContext = _context.ElectiveLessons.Include(e => e.Theme).Include(e => e.Type);
            return View(await academyContext.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewData["ElectiveThemeID"] = new SelectList(_context.ElectiveThemes, "ElectiveThemeID", "Name");
            ViewData["ElectiveTypeID"] = new SelectList(_context.ElectiveTypes, "ElectiveTypeID", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ElectiveLessonID,Date,Comment,Signature,FlagF,DepartmentID,ElectiveThemeID,ElectiveTypeID")] ElectiveLesson electiveLesson)
        {
            if (ModelState.IsValid)
            {
                _context.Add(electiveLesson);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ElectiveThemeID"] = new SelectList(_context.ElectiveThemes, "ElectiveThemeID", "Name", electiveLesson.ElectiveThemeID);
            ViewData["ElectiveTypeID"] = new SelectList(_context.ElectiveTypes, "ElectiveTypeID", "Name", electiveLesson.ElectiveTypeID);
            return View(electiveLesson);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var electiveLesson = await _context.ElectiveLessons
                .Include(e => e.Theme)
                .Include(e => e.Type)
                .FirstOrDefaultAsync(m => m.ElectiveLessonID == id);
            if (electiveLesson == null)
            {
                return NotFound();
            }

            return View(electiveLesson);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var electiveLesson = await _context.ElectiveLessons.FindAsync(id);
            _context.ElectiveLessons.Remove(electiveLesson);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ElectiveLessonExists(int id)
        {
            return _context.ElectiveLessons.Any(e => e.ElectiveLessonID == id);
        }
    }
}
