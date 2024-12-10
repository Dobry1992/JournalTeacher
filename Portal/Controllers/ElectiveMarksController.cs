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
    public class ElectiveMarksController : Controller
    {
        private readonly AcademyContext _context;

        public ElectiveMarksController(AcademyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var academyContext = _context.ElectiveMarks.Include(e => e.ElectiveLesson);
            return View(await academyContext.ToListAsync());
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var electiveMark = await _context.ElectiveMarks.FindAsync(id);
            if (electiveMark == null)
            {
                return NotFound();
            }
            ViewData["ElectiveLessonID"] = new SelectList(_context.ElectiveLessons, "ElectiveLessonID", "ElectiveLessonID", electiveMark.ElectiveLessonID);
            return View(electiveMark);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ElectiveMarkID,Value,Date,Comment,SignatureOfTeacher,HistoryOfMark,FlagF,ElectiveLessonID,StudentID")] ElectiveMark electiveMark)
        {
            if (id != electiveMark.ElectiveMarkID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(electiveMark);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ElectiveMarkExists(electiveMark.ElectiveMarkID))
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
            ViewData["ElectiveLessonID"] = new SelectList(_context.ElectiveLessons, "ElectiveLessonID", "ElectiveLessonID", electiveMark.ElectiveLessonID);
            return View(electiveMark);
        }

        private bool ElectiveMarkExists(int id)
        {
            return _context.ElectiveMarks.Any(e => e.ElectiveMarkID == id);
        }
    }
}
