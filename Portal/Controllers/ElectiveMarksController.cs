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
using Portal.ViewModel.Elective;

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
        public async Task<IActionResult> Index(DateTime date)
        {
            List<ElectiveStudentMark> electiveStudentMarks = new();

            if (date.ToShortDateString() == "01.01.0001")
            {
                var marks = await _context.ElectiveMarks
               .Where(l => l.Date.Year == DateTime.Now.Year && l.Date.Month == DateTime.Now.Month && l.Date.Day == DateTime.Now.Day && l.Value != "")
               .Include(l => l.ElectiveLesson)
                   .ThenInclude(l => l.Theme)
                       .ThenInclude(t => t.Elective)
               .ToListAsync();

                foreach (var mark in marks)
                {
                    Student student = _context.Students.Find(mark.StudentID);
                    ElectiveStudentMark electiveStudentMark = new ElectiveStudentMark()
                    {
                        Student = student,
                        ElectiveMark = mark,
                        Group = _context.Groups.Find(student.GroupID)
                    };
                    electiveStudentMarks.Add(electiveStudentMark);
                }
            }

            if (date.ToShortDateString() != "01.01.0001")
            {
                var marks = _context.ElectiveMarks
                 .Where(l => l.Date.Year == date.Year && l.Date.Month == date.Month && l.Date.Day == date.Day && l.Value != "")
                 .Include(l => l.ElectiveLesson)
                    .ThenInclude(l => l.Theme)
                        .ThenInclude(t => t.Elective)
                 .ToList();

                foreach (var mark in marks)
                {
                    Student student = _context.Students.Find(mark.StudentID);
                    ElectiveStudentMark electiveStudentMark = new ElectiveStudentMark()
                    {
                        Student = student,
                        ElectiveMark = mark,
                        Group = _context.Groups.Find(student.GroupID)
                    };
                    electiveStudentMarks.Add(electiveStudentMark);
                }
            }

            return View(electiveStudentMarks);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

            var electiveMark = await _context.ElectiveMarks.FindAsync(id);
            var lesson = await _context.ElectiveLessons.FindAsync(electiveMark.ElectiveLessonID);
            var theme = await _context.ElectiveThemes.FindAsync(lesson.ElectiveThemeID);
            var elective = await _context.Electives.FindAsync(theme.ElectiveID);
            ViewBag.TeachersNoPC = await _context.TeacherNoPCs.ToListAsync();
            ViewBag.Teachers = await _context.Teachers.ToListAsync();
            ViewBag.UserName = teacher;

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
