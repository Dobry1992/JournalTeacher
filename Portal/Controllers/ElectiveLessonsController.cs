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
    public class ElectiveLessonsController : Controller
    {
        private readonly AcademyContext _context;

        public ElectiveLessonsController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var academyContext = _context.ElectiveLessons.Include(e => e.Theme).Include(e => e.Type);
            return View(await academyContext.ToListAsync());
        }

        public IActionResult Create(int? electiveID)
        {
            ViewData["ElectiveThemeID"] = new SelectList(_context.ElectiveThemes.Where(t => t.ElectiveID == electiveID), "ElectiveThemeID", "Name");
            ViewData["ElectiveTypeID"] = new SelectList(_context.ElectiveTypes, "ElectiveTypeID", "Name");
            ViewBag.ElectiveID = electiveID;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ElectiveLessonID,Date,Comment,Signature,FlagF,DepartmentID,ElectiveThemeID,ElectiveTypeID")] ElectiveLesson electiveLesson, int electiveID)
        {
            if (ModelState.IsValid)
            {
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

                Elective elective = await _context.Electives.FindAsync(electiveID);
                Department department = await _context.Departments.FindAsync(elective.DepartmentID);
                electiveLesson.DepartmentID = department.DepartmentID;
                electiveLesson.Signature = teacher;
                _context.Add(electiveLesson);
                await _context.SaveChangesAsync();

                List<El_Stud_Link> links = new();
                links = await _context.El_Stud_Links.Where(l => l.ElectiveID == electiveID).ToListAsync();
                foreach (var link in links)
                {
                    Student student = _context.Students.Find(link.StudentID);
                    ElectiveMark mark = new ElectiveMark()
                    {
                        Value = "",
                        Date = electiveLesson.Date,
                        FlagF = 0,
                        ElectiveLessonID = electiveLesson.ElectiveLessonID,
                        StudentID = student.StudentID
                    };
                    _context.Add(mark);
                }
                await _context.SaveChangesAsync();

                return RedirectToAction("ElectiveJournal", "Journals", new { electiveID });
            }
            ViewData["ElectiveThemeID"] = new SelectList(_context.ElectiveThemes.Where(t => t.ElectiveID == electiveID), "ElectiveThemeID", "Name", electiveLesson.ElectiveThemeID);
            ViewData["ElectiveTypeID"] = new SelectList(_context.ElectiveTypes, "ElectiveTypeID", "Name", electiveLesson.ElectiveTypeID);
            ViewBag.ElectiveID = electiveID;
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
            var theme = await _context.ElectiveThemes.FindAsync(electiveLesson.ElectiveThemeID);
            int electiveID = theme.ElectiveID;
            _context.ElectiveLessons.Remove(electiveLesson);
            await _context.SaveChangesAsync();
            return RedirectToAction("ElectiveJournal", "Journals", new { electiveID });
        }

        private bool ElectiveLessonExists(int id)
        {
            return _context.ElectiveLessons.Any(e => e.ElectiveLessonID == id);
        }
    }
}
