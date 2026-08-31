using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Repository;
using Portal.Services;
using System;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;

namespace Portal
{
    public class StatementLessonsController : Controller
    {
        private readonly AcademyContext _context;
        private readonly UserNameService _userNameService;

        public StatementLessonsController(AcademyContext context, UserNameService userNameService)
        {
            _context = context;
            _userNameService = userNameService;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index(DateTime date)
        {
            var lessons = _context.StatementLessons
                .Where(l => l.Date.Year == DateTime.Now.Year && l.Date.Month == DateTime.Now.Month && l.Date.Day == DateTime.Now.Day)
                .OrderBy(l => l.Date)
                .Include(l => l.Group)
                .Include(l => l.TypeOfExercise);

            if (date.ToShortDateString() != "01.01.0001")
            {
                lessons = _context.StatementLessons
                 .Where(l => l.Date.Year == date.Year && l.Date.Month == date.Month && l.Date.Day == date.Day)
                 .OrderBy(l => l.Date)
                 .Include(l => l.Group)
                 .Include(l => l.TypeOfExercise);
            }

            return View(await lessons.ToListAsync());
        }

        public IActionResult Create(int? GroupID)
        {
            ViewData["GroupID"] = new SelectList(_context.Groups.Where(g => g.GroupID == GroupID), "GroupID", "GroupID");
            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => t.Name == "Дипломная работа" || t.Name == "Дипломный проект" || t.Name == "Магистерская работа"
                || t.Name == "Государственный экзамен" || t.Name == "Стажировка" || t.Name == "Производственная практика" || t.Name == "Учебная практика")
                .OrderBy(t => t.Name), "TypeOfExerciseID", "Name");
            ViewBag.GroupID = GroupID;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int GroupID, [Bind("StatementLessonID,Date,Comment,Signature,TypeOfExerciseID,GroupID")] StatementLesson statementLesson)
        {
            if (statementLesson.Date > DateTime.Now)
            {
                ModelState.AddModelError("", "Невозможно создать занятие в будующем!");
            }

            string teacher = _userNameService.GetDisplayName();


            if (ModelState.IsValid)
            {
                statementLesson.Signature = teacher;
                _context.Add(statementLesson);
                await _context.SaveChangesAsync();

                var group = await _context.Groups.FindAsync(GroupID);
                var students = _context.Students.Where(s => s.GroupID == GroupID);
                foreach(var student in students)
                {
                    StatementMark statementMark = new();
                    statementMark.Date = statementLesson.Date;
                    statementMark.Value = "";
                    statementMark.InstituteID = group.InstituteID;
                    statementMark.SpecialityID = group.SpecialityID;
                    statementMark.GroupID = GroupID;
                    statementMark.TypeOfExerciseID = statementLesson.TypeOfExerciseID;
                    statementMark.StatementLessonID = statementLesson.StatementLessonID;
                    statementMark.StudentID = student.StudentID;
                    _context.Add(statementMark);
                }
                await _context.SaveChangesAsync();

                return RedirectToAction("Statement", "Journals", new { GroupID });
            }
            ViewData["GroupID"] = new SelectList(_context.Groups.Where(g => g.GroupID == GroupID), "GroupID", "GroupID");
            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => t.Name == "Дипломная работа" || t.Name == "Дипломный проект" || t.Name == "Магистерская работа"
                || t.Name == "Государственный экзамен" || t.Name == "Стажировка" || t.Name == "Производственная практика" || t.Name == "Учебная практика"), "TypeOfExerciseID", "Name");
            ViewBag.GroupID = GroupID;
            return View(statementLesson);
        }

        public async Task<IActionResult> Delete(int? id, int? GroupID)
        {
            if (id == null)
            {
                return NotFound();
            }

            var statementLesson = await _context.StatementLessons
                .Include(s => s.Group)
                .Include(s => s.TypeOfExercise)
                .FirstOrDefaultAsync(m => m.StatementLessonID == id);
            if (statementLesson == null)
            {
                return NotFound();
            }
            ViewBag.GroupID = GroupID;
            return View(statementLesson);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int GroupID)
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

            var statementLesson = await _context.StatementLessons.FindAsync(id);
            var statementMarks = _context.StatementMarks.Where(m => m.StatementLessonID == id);
            foreach(var mark in statementMarks)
            {
                _context.StatementMarks.Remove(mark);
            }
            _context.StatementLessons.Remove(statementLesson);
            await _context.SaveChangesAsync();

            var type = await _context.Types.FindAsync(statementLesson.TypeOfExerciseID);
            var group = await _context.Groups.FindAsync(GroupID);
            Event e = new();
            e.Date = DateTime.Now;
            e.Teacher = teacher;
            e.Log = "Удалено занятие от: " + statementLesson.Date.ToShortDateString() + ", тип: " + type.Name + ", группа: " + group.Name;
            _context.Events.Update(e);
            await _context.SaveChangesAsync();
            return RedirectToAction("Statement", "Journals", new { GroupID });
        }

        private bool StatementLessonExists(int id)
        {
            return _context.StatementLessons.Any(e => e.StatementLessonID == id);
        }
    }
}
