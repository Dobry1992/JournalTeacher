using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Models.Model;
using Portal.ViewModel;

namespace Portal
{
    public class JournalsController : Controller
    {
        private readonly AcademyContext _context;

        public JournalsController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var journals = _context.Journals
                .Include(j => j.Group)
                .Include(j => j.Subject);
            return View(await journals.ToListAsync());
        }

        public IActionResult Final(int GroupID, int SubjectID)
        {
            var students = _context.Students.Where(s => s.GroupID == GroupID).AsNoTracking();
            List<ResultStudent> resultsStudent = new();
            foreach (var student in students)
            {
                ResultStudent resultStudent = new();
                List<double> marks = new();
                var simpleMarks = _context.Marks.Where(m => m.FlagF == 0 && m.GroupID == GroupID && m.SubjectID == SubjectID && m.StudentID == student.StudentID).AsNoTracking();
                foreach (var mark in simpleMarks)
                {
                    if (double.TryParse(mark.Value, out double m))
                    {
                        marks.Add(m);
                    }
                }
                resultStudent.Student = student;
                resultStudent.Value = Math.Round(marks.Sum() / marks.Count, 2);
                resultsStudent.Add(resultStudent);
            }

            ViewBag.GroupID = GroupID;
            ViewBag.SubjectID = SubjectID;
            return View(resultsStudent.OrderBy(s => s.Student.LastName));
        }

        public async Task<IActionResult> Statement(int GroupID)
        {
            var typeDR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дипломная работа");
            var typeGE = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Государственный экзамен");
            var typeS = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Стажировка");
            var typePP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Производственная практика");
            var typeUP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Учебная практика");
            var typeDP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дипломный проект");
            var typeMR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Магистерская работа");

            var group = await _context.Groups.FindAsync(GroupID);
            var inst = await _context.Institutes.FindAsync(group.InstituteID);

            var lessons = _context.StatementLessons.Where(l => l.GroupID == GroupID && (l.TypeOfExerciseID == typeDR.TypeOfExerciseID || l.TypeOfExerciseID == typeGE.TypeOfExerciseID
                || l.TypeOfExerciseID == typeS.TypeOfExerciseID || l.TypeOfExerciseID == typePP.TypeOfExerciseID || l.TypeOfExerciseID == typeUP.TypeOfExerciseID
                || l.TypeOfExerciseID == typeDP.TypeOfExerciseID || l.TypeOfExerciseID == typeMR.TypeOfExerciseID))
                .OrderBy(l => l.Date);

            var students = _context.Students
               .Where(s => s.GroupID == GroupID && s.Status == true)
               .OrderBy(s => s.LastName);

            var marks = _context.StatementMarks
                .Where(m => m.GroupID == GroupID && (m.TypeOfExerciseID == typeDR.TypeOfExerciseID || m.TypeOfExerciseID == typeGE.TypeOfExerciseID
                    || m.TypeOfExerciseID == typeS.TypeOfExerciseID || m.TypeOfExerciseID == typePP.TypeOfExerciseID || m.TypeOfExerciseID == typeUP.TypeOfExerciseID
                    || m.TypeOfExerciseID == typeDP.TypeOfExerciseID || m.TypeOfExerciseID == typeMR.TypeOfExerciseID))
                .OrderBy(m => m.Date);

            ViewBag.Group = group;
            ViewBag.Marks = marks;
            ViewBag.Students = students;
            ViewBag.GroupID = GroupID;
            ViewBag.typeDR = typeDR.TypeOfExerciseID;
            ViewBag.typeGE = typeGE.TypeOfExerciseID;
            ViewBag.typeS = typeS.TypeOfExerciseID;
            ViewBag.typePP = typePP.TypeOfExerciseID;
            ViewBag.typeUP = typeUP.TypeOfExerciseID;
            ViewBag.typeDP = typeDP.TypeOfExerciseID;
            ViewBag.typeMR = typeMR.TypeOfExerciseID;
            ViewBag.Institute = inst;

            return View(await lessons.ToListAsync());
        }

        public async Task<IActionResult> ElectiveJournal(int electiveID)
        {
            var elective = await _context.Electives.FindAsync(electiveID);
            var links = await _context.El_Stud_Links.Where(l => l.ElectiveID == electiveID).ToListAsync();
            List<Student> students = new();
            foreach (var link in links)
            {
                Student student = _context.Students.Find(link.StudentID);
                students.Add(student);
            }

            var lessons = await _context.ElectiveLessons
                .Where(l => l.Theme.ElectiveID == electiveID)
                .Include(l => l.Theme)
                .Include(l => l.Type)
                .OrderBy(l => l.Date)
                .ToListAsync();

            var marks = await _context.ElectiveMarks
                .Where(m => m.ElectiveLesson.Theme.ElectiveID == electiveID)
                .OrderBy(m => m.Date)
                .ToListAsync();

            ViewBag.Elective = elective;
            ViewBag.Students = students;
            ViewBag.Marks = marks;
            ViewBag.ElectiveID = electiveID;

            return View(lessons);
        }

        public async Task<IActionResult> Journal(int GroupID, int SubjectID)
        {
            var group = await _context.Groups.FindAsync(GroupID);
            var subject = await _context.Subjects.FindAsync(SubjectID);

            if (group == null || subject == null)
                return NotFound();

            var department = await _context.Departments.FindAsync(subject.DepartmentID);

            var journals = await _context.Journals
                .Where(j => j.GroupID == GroupID && j.SubjectID == SubjectID)
                .AsNoTracking()
                .ToListAsync();

            if (!journals.Any())
            {
                ViewBag.GroupID = GroupID;
                ViewBag.SubjectID = SubjectID;
                ViewBag.CountFlag = 0;
                return View();
            }

            var types = await _context.Types.ToListAsync();
            var typesDict = types.ToDictionary(t => t.Name, t => t.TypeOfExerciseID);

            var lessons = await _context.Lessons
                .Where(l => l.Theme.SubjectID == SubjectID && l.GroupID == GroupID)
                .OrderBy(l => l.Date)
                .Include(l => l.TypeOfExercise)
                .Include(l => l.Theme)
                .AsNoTracking()
                .ToListAsync();

            var students = await _context.Students
                .Where(s => s.GroupID == GroupID && s.Status == true)
                .OrderBy(s => s.LastName)
                .AsNoTracking()
                .ToListAsync();

            var marks = await _context.Marks
                .Where(m => m.SubjectID == SubjectID && m.GroupID == GroupID)
                .OrderBy(m => m.Date)
                .AsNoTracking()
                .ToListAsync();

            var statementLessons = lessons
                .Where(l => IsType(l.TypeOfExerciseID, typesDict, "Экзамен", "Дифференцированный зачёт", "Зачёт"))
                .OrderBy(l => l.Date)
                .ToList();

            var journalMarks = BuildJournalMarks(marks, typesDict);
            var journalLessons = BuildJournalLessons(lessons, typesDict);

            var journalViewModel = new JournalViewModel
            {
                JournalMarks = journalMarks.OrderBy(m => m.Mark.Date).ToList(),
                JournalLessons = journalLessons.OrderBy(l => l.Lesson.Date).ToList(),
                Students = students,
                Department = department,
                Subject = subject,
                Group = group,
                StatementLessons = statementLessons
            };

            return View(journalViewModel);
        }

        private List<JournalMarks> BuildJournalMarks(List<Mark> marks, Dictionary<string, int> types)
        {
            var list = new List<JournalMarks>();
            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            var previousMonth = currentMonth.AddMonths(-1);
            var tenthOfCurrentMonth = new DateTime(today.Year, today.Month, 9);

            foreach (var mark in marks)
            {
                var jm = new JournalMarks
                {
                    Mark = mark,
                    Controller = "Marks"
                };

                var markMonth = new DateTime(mark.Date.Year, mark.Date.Month, 1);
                bool isInAllowedDateRange = markMonth == currentMonth || (markMonth == previousMonth && today <= tenthOfCurrentMonth);

                var allowedTypes = _context.Types
                    .Where(t => t.Name == "Экзамен" || t.Name == "Зачёт" || t.Name == "Дифференцированный зачёт")
                    .Select(t => t.TypeOfExerciseID)
                    .ToList();

                bool isEditAllowedByRules = true;

                if (mark.FlagF != 0 && !allowedTypes.Contains(mark.TypeOfExerciseID))
                {
                    isEditAllowedByRules = false;
                }
                else if (mark.ChangeCounter >= 3)
                {
                    isEditAllowedByRules = false;
                }
                else if (mark.ChangeCounter == 1 && allowedTypes.Contains(mark.TypeOfExerciseID))
                {
                    isEditAllowedByRules = false;
                }

                jm.IsEdit = isInAllowedDateRange && isEditAllowedByRules;


                if (IsType(mark.TypeOfExerciseID, types, "Экзамен", "Зачёт", "Дифференцированный зачёт"))
                {
                    jm.Property = "tableMarkEKZ";
                    jm.Action = "Edit";
                }
                else if (IsType(mark.TypeOfExerciseID, types, "Итоговая оценка"))
                {
                    jm.Property = "tableMarkIO";
                    jm.Action = "Journal";
                    jm.Controller = "Journals";
                }
                else if (IsType(mark.TypeOfExerciseID, types, "Курсовой проект", "Курсовая работа"))
                {
                    jm.Property = "tableMarkK";
                    jm.Action = "Edit";
                }
                else
                {
                    jm.Property = mark.FlagF == 0 ? "tableMark" : "tableMarkSet";
                    jm.Action = "Edit";
                }

                list.Add(jm);
            }

            return list;
        }

        private List<JournalLessons> BuildJournalLessons(List<Lesson> lessons, Dictionary<string, int> types)
        {
            var list = new List<JournalLessons>();
            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            var previousMonth = currentMonth.AddMonths(-1);
            var tenthOfCurrentMonth = new DateTime(today.Year, today.Month, 9);

            foreach (var lesson in lessons)
            {
                var jl = new JournalLessons
                {
                    Lesson = lesson,
                    Controller = "Lessons"
                };

                var lessonMonth = new DateTime(lesson.Date.Year, lesson.Date.Month, 1);
                jl.IsEdit = lessonMonth == currentMonth || (lessonMonth == previousMonth && today <= tenthOfCurrentMonth);

                if (lesson.FlagF != 0)
                {
                    jl.IsEdit = false;
                }

                if (IsType(lesson.TypeOfExerciseID, types, "Экзамен", "Зачёт", "Дифференцированный зачёт"))
                {
                    jl.Action = "DeleteF";
                }
                else if (IsType(lesson.TypeOfExerciseID, types, "Итоговая оценка"))
                {
                    jl.Action = "Journal";
                    jl.Controller = "Journals";
                }
                else
                {
                    jl.Action = "Delete";
                }

                list.Add(jl);
            }

            return list;
        }

        private bool IsType(int typeId, Dictionary<string, int> dict, params string[] names)
        {
            foreach (var name in names)
            {
                if (dict.TryGetValue(name, out var id) && id == typeId)
                    return true;
            }
            return false;
        }


        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create(int GroupID, int SubjectID)
        {
            ViewData["GroupID"] = new SelectList(_context.Groups.Where(g => g.GroupID == GroupID), "GroupID", "Name");
            ViewData["SubjectID"] = new SelectList(_context.Subjects.Where(s => s.SubjectID == SubjectID), "SubjectID", "Name");
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int GroupID, int SubjectID, [Bind("JournalID,Comment,GroupID,SubjectID,Date")] Journal journal)
        {
            if (ModelState.IsValid)
            {
                journal.Date = DateTime.Now;
                _context.Add(journal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GroupID"] = new SelectList(_context.Groups.Where(g => g.GroupID == GroupID), "GroupID", "Name", journal.GroupID);
            ViewData["SubjectID"] = new SelectList(_context.Subjects.Where(s => s.SubjectID == SubjectID), "SubjectID", "Name", journal.SubjectID);
            return View(journal);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult CreateJournal()
        {
            ViewData["GroupID"] = new SelectList(_context.Groups, "GroupID", "Name");
            ViewData["SubjectID"] = new SelectList(_context.Subjects, "SubjectID", "Name");
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJournal([Bind("JournalID,Comment,GroupID,SubjectID,Date")] Journal journal)
        {
            if (ModelState.IsValid)
            {
                journal.Date = DateTime.Now;
                _context.Add(journal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GroupID"] = new SelectList(_context.Groups, "GroupID", "Name", journal.GroupID);
            ViewData["SubjectID"] = new SelectList(_context.Subjects, "SubjectID", "Name", journal.SubjectID);
            return View(journal);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var journal = await _context.Journals
                .Include(j => j.Group)
                .Include(j => j.Subject)
                .FirstOrDefaultAsync(m => m.JournalID == id);
            if (journal == null)
            {
                return NotFound();
            }

            return View(journal);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var journal = await _context.Journals.FindAsync(id);
            _context.Journals.Remove(journal);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool JournalExists(int id)
        {
            return _context.Journals.Any(e => e.JournalID == id);
        }
    }
}