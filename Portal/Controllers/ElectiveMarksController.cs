using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Models.Elective;
using Portal.Repository;

namespace Portal.Controllers
{
    public class ElectiveMarksController : Controller
    {
        private readonly AcademyContext _context;

        public ElectiveMarksController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
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
            var lesson = await _context.ElectiveLessons.FindAsync(electiveMark.ElectiveLessonID);
            var theme = await _context.ElectiveThemes.FindAsync(lesson.ElectiveThemeID);
            var elective = await _context.Electives.FindAsync(theme.ElectiveID);

            if (electiveMark == null)
            {
                return NotFound();
            }

            ViewBag.ElectiveID = elective.ElectiveID;
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

            var lesson = await _context.ElectiveLessons.FindAsync(electiveMark.ElectiveLessonID);
            var theme = await _context.ElectiveThemes.FindAsync(lesson.ElectiveThemeID);
            var elective = await _context.Electives.FindAsync(theme.ElectiveID);

            string teacher = "";
            var username = User.Identity.Name;
            using (var context = new PrincipalContext(ContextType.Domain, AD.root))
            {
                try
                {
                    var user = UserPrincipal.FindByIdentity(context, username);

                    if (user != null)
                    {
                        teacher = user.DisplayName;
                    }
                }
                catch
                {
                    teacher = username;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (User.IsInRole("ICDA-writer") || User.IsInRole("K-8Writer"))
                    {
                        electiveMark.HistoryOfMark += electiveMark.Value + " - " + DateTime.Now.ToShortDateString() + " - " + electiveMark.SignatureOfTeacher + "</br>";
                        _context.ElectiveMarks.Update(electiveMark);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        electiveMark.SignatureOfTeacher = teacher;
                        electiveMark.HistoryOfMark += electiveMark.Value + " - " + DateTime.Now.ToShortDateString() + " - " + teacher + "</br>";
                        _context.ElectiveMarks.Update(electiveMark);
                        await _context.SaveChangesAsync();
                    }
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
                return RedirectToAction("ElectiveJournal", "Journals", new { electiveID = elective.ElectiveID });
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
