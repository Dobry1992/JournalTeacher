using DocumentFormat.OpenXml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Portal.Data;
using Portal.Models;
using Portal.Models.Model;
using Portal.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal
{
    public class JournalsController : Controller
    {
        private readonly AcademyContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<JournalsController> _logger; // Добавляем логгер

        public JournalsController(
            AcademyContext context,
            IWebHostEnvironment env,
            ILogger<JournalsController> logger) // Добавляем в конструктор
        {
            _context = context;
            _env = env;
            _logger = logger; // Инициализируем
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var journals = _context.Journals
                .Include(j => j.Group)
                .Include(j => j.Subject);
            return View(await journals.ToListAsync());
        }

        public async Task<IActionResult> Final(int GroupID, int SubjectID)
        {
            var students = _context.Students
                .Where(s => s.GroupID == GroupID)
                .AsNoTracking();

            TypeOfExercise typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
            TypeOfExercise typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");
            List<ResultStudent> resultsStudent = new();
            foreach (var student in students)
            {
                ResultStudent resultStudent = new();
                List<double> marks = new();
                var simpleMarks = _context.Marks
                    .Where(m =>
                        m.FlagF == 0 &&
                        m.GroupID == GroupID &&
                        m.SubjectID == SubjectID &&
                        m.StudentID == student.StudentID &&
                        m.TypeOfExerciseID != typeKR.TypeOfExerciseID &&
                        m.TypeOfExerciseID != typeKP.TypeOfExerciseID
                    )
                    .AsNoTracking();
                foreach (var mark in simpleMarks)
                {
                    if (double.TryParse(mark.Value, out double m))
                    {
                        marks.Add(m);
                    }
                }
                resultStudent.Student = student;
                resultStudent.Value = Math.Round(marks.Sum() / marks.Count, 3, MidpointRounding.AwayFromZero);
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
            return await PrepareJournalView(GroupID, SubjectID, "Journal");
        }

        public async Task<IActionResult> Controls(int GroupID, int SubjectID)
        {
            var group = await _context.Groups.FindAsync(GroupID);
            var subject = await _context.Subjects.FindAsync(SubjectID);
            var controlType = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Контрольное мероприятие");

            if (group == null || subject == null)
                return NotFound();

            var department = await _context.Departments.FindAsync(subject.DepartmentID);

            var lessons = await _context.Lessons
                .Where(l => l.Theme.SubjectID == SubjectID && l.GroupID == GroupID && l.TypeOfExerciseID == controlType.TypeOfExerciseID)
                .Include(l => l.TypeOfExercise)
                .Include(l => l.Theme)
                .OrderBy(l => l.Date)
                .AsNoTracking()
                .ToListAsync();

            var students = await _context.Students
                .Where(s => s.GroupID == GroupID && s.Status == true)
                .OrderBy(s => s.LastName)
                .AsNoTracking()
                .ToListAsync();

            var marks = await _context.Marks
                .Where(m => m.SubjectID == SubjectID && m.GroupID == GroupID && m.TypeOfExerciseID == controlType.TypeOfExerciseID)
                .OrderBy(m => m.Date)
                .AsNoTracking()
                .ToListAsync();

            var controlViewModel = new ControlsViewModel
            {
                Marks = marks,
                Lessons = lessons,
                Students = students,
                Department = department,
                Subject = subject,
                Group = group
            };

            return View(controlViewModel);
        }

        [Authorize(Roles = "SuperAdmin,ANB-UMCH")]
        public async Task<IActionResult> AdjustedJournal(int GroupID, int SubjectID)
        {
            var academicYears = await GetAcademicYearsForSubjectAndGroup(GroupID, SubjectID);
            ViewBag.AcademicYears = academicYears;

            return await PrepareJournalView(GroupID, SubjectID, "AdjustedJournal");
        }

        private List<JournalMarks> BuildJournalMarks(List<Mark> marks, Dictionary<string, int> types)
        {
            var list = new List<JournalMarks>();
            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            var previousMonth = currentMonth.AddMonths(-1);
            var tenthOfCurrentMonth = new DateTime(today.Year, today.Month, 9);

            int semester = GetSemester(today);

            foreach (var mark in marks)
            {
                var jm = new JournalMarks
                {
                    Mark = mark,
                    Controller = "Marks"
                };

                int semesterOfMark = GetSemester(mark.Date);
                if (semester != semesterOfMark)
                {
                    jm.CollapseId = "collapse";
                }
                else
                {
                    if (mark.Date.Year != today.Year)
                    {
                        jm.CollapseId = "collapse";
                    }
                    else
                    {
                        jm.CollapseId = "";
                    }
                }

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
                else if (mark.ChangeCounter >= 1 && allowedTypes.Contains(mark.TypeOfExerciseID))
                {
                    isEditAllowedByRules = false;
                }

                jm.IsEdit = isInAllowedDateRange && isEditAllowedByRules;


                if (IsType(mark.TypeOfExerciseID, types, "Экзамен", "Зачёт", "Дифференцированный зачёт"))
                {
                    jm.Property = "tableMarkEKZ";
                    jm.Action = "Edit";
                }
                else if (IsType(mark.TypeOfExerciseID, types, "Итоговая отметка"))
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
                else if (IsType(mark.TypeOfExerciseID, types, "Контрольное мероприятие"))
                {
                    jm.Property = "ControlEventMark";
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

            int semester = GetSemester(today);

            foreach (var lesson in lessons)
            {
                var jl = new JournalLessons
                {
                    Lesson = lesson,
                    Controller = "Lessons"
                };

                int semesterOfLesson = GetSemester(lesson.Date);
                if (semesterOfLesson != semester)
                {
                    jl.CollapseId = "collapse";
                }
                else
                {
                    if (today.Year != lesson.Date.Year)
                    {
                        jl.CollapseId = "collapse";
                    }
                    else
                    {
                        jl.CollapseId = "";
                    }
                }

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
                else if (IsType(lesson.TypeOfExerciseID, types, "Итоговая отметка"))
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

        private async Task<IActionResult> PrepareJournalView(int GroupID, int SubjectID, string viewName)
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

            ViewBag.CountFlag = 1;

            if (!journals.Any())
            {
                ViewBag.GroupID = GroupID;
                ViewBag.SubjectID = SubjectID;
                ViewBag.CountFlag = 0;
                return View(viewName);
            }

            var types = await _context.Types.ToListAsync();
            var typesDict = types.ToDictionary(t => t.Name, t => t.TypeOfExerciseID);

            var lessons = await _context.Lessons
                .Where(l =>
                    l.Theme.SubjectID == SubjectID &&
                    l.GroupID == GroupID
                )
                .OrderBy(l => l.Date)
                .Include(l => l.TypeOfExercise)
                .Include(l => l.Theme)
                .AsNoTracking()
                .ToListAsync();



            var students = await _context.Students
                .Where(s =>
                    s.GroupID == GroupID &&
                    s.Status == true
                )
                .OrderBy(s => s.LastName)
                .AsNoTracking()
                .ToListAsync();

            var marks = await _context.Marks
                .Where(m =>
                    m.SubjectID == SubjectID &&
                    m.GroupID == GroupID
                )
                .OrderBy(m => m.Date)
                .AsNoTracking()
                .ToListAsync();

            var statementLessons = lessons
                .Where(l => IsType(l.TypeOfExerciseID, typesDict, "Экзамен", "Дифференцированный зачёт", "Зачёт"))
                .OrderBy(l => l.Date)
                .ToList();

            var journalMarks = BuildJournalMarks(marks, typesDict);
            var journalLessons = BuildJournalLessons(lessons, typesDict);

            var simpleLessons = lessons
                .Where(l =>
                    !IsType(l.TypeOfExerciseID, typesDict, "Экзамен", "Дифференцированный зачёт", "Зачёт", "Итоговая отметка", "Курсовой проект", "Курсовая работа")
                )
                .ToList();

            List<Mark> simpleMarks = marks
                .Where(m =>
                    !IsType(m.TypeOfExerciseID, typesDict, "Экзамен", "Дифференцированный зачёт", "Зачёт", "Итоговая отметка", "Курсовой проект", "Курсовая работа")
                )
                .ToList();

            Dictionary<int, List<Lesson>> lessonsByFlag = simpleLessons
                .GroupBy(l => l.FlagF)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(l => l.Date).ToList()
                );

            List<JournalLessons> statLessons = new();
            List<JournalMarks> statMarks = new();
            Theme theme = await _context.Themes
                .FirstOrDefaultAsync(t =>
                    t.Name == "Контрольное занятие" &&
                    t.SubjectID == subject.SubjectID
                );
            TypeOfExercise type = await _context.Types
                .FirstOrDefaultAsync(t =>
                    t.Name == "Контрольное мероприятие"
                );

            foreach (var l in lessonsByFlag)
            {
                List<Lesson> keySimpleLessons = l.Value;
                Lesson statlesson = new()
                {
                    FlagF = l.Key,
                    SubjectID = SubjectID,
                    GroupID = GroupID,
                    Group = group,
                    ThemeID = theme.ThemeID,
                    Theme = theme,
                    TypeOfExercise = type,
                    TypeOfExerciseID = type.TypeOfExerciseID,
                    Date = keySimpleLessons[^1].Date.AddHours(1)
                };

                JournalLessons journalLesson = new()
                {
                    Lesson = statlesson,
                    IsEdit = false
                };

                statLessons.Add(journalLesson);

                foreach (var student in students)
                {
                    List<Mark> statSimpleMarks = simpleMarks
                        .Where(m =>
                            m.StudentID == student.StudentID &&
                            m.FlagF == l.Key
                        )
                        .ToList();

                    List<double> doubleMark = new();
                    foreach (var mark in statSimpleMarks)
                    {
                        if (double.TryParse(mark.Value, out double number))
                        {
                            doubleMark.Add(number);
                        }
                    }

                    if (!doubleMark.Any())
                    {
                        Mark statMark = new()
                        {
                            ChangeCounter = 3,
                            Date = statlesson.Date,
                            FlagF = l.Key,
                            GroupID = GroupID,
                            Student = student,
                            Theme = theme,
                            ThemeID = theme.ThemeID,
                            StudentID = student.StudentID,
                            TypeOfExerciseID = type.TypeOfExerciseID,
                            SubjectID = SubjectID,
                            DepartmentID = department.DepartmentID,
                            InstituteID = group.InstituteID,
                            SpecialityID = group.SpecialityID,
                            Value = ""
                        };

                        JournalMarks journalMark = new()
                        {
                            IsEdit = false,
                            Mark = statMark
                        };

                        statMarks.Add(journalMark);
                    }
                    else
                    {
                        Mark statMark = new()
                        {
                            ChangeCounter = 3,
                            Date = statlesson.Date,
                            FlagF = l.Key,
                            GroupID = GroupID,
                            Student = student,
                            Theme = theme,
                            ThemeID = theme.ThemeID,
                            StudentID = student.StudentID,
                            TypeOfExerciseID = type.TypeOfExerciseID,
                            SubjectID = SubjectID,
                            DepartmentID = department.DepartmentID,
                            InstituteID = group.InstituteID,
                            SpecialityID = group.SpecialityID,
                            Value = Math.Round(doubleMark.Average(), 3, MidpointRounding.AwayFromZero).ToString()
                        };

                        JournalMarks journalMark = new()
                        {
                            IsEdit = false,
                            Mark = statMark,
                            Property = "statMark"
                        };

                        statMarks.Add(journalMark);
                    }
                }
            }

            journalLessons.AddRange(statLessons);
            journalMarks.AddRange(statMarks);

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

            return View(viewName, journalViewModel);
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

        private static int GetSemester(DateTime date)
        {
            if (date.Month == 1)
                return 1;

            if (date.Month >= 9 && date.Month <= 12)
                return 1;

            if (date.Month >= 2 && date.Month <= 8)
                return 2;

            return 0;
        }

        public async Task<IActionResult> ExportToExcel(int GroupID, int SubjectID, DateTime d1, DateTime d2, int flg)
        {
            var group = await _context.Groups.FindAsync(GroupID);
            var subject = await _context.Subjects.FindAsync(SubjectID);

            if (group == null || subject == null)
                return NotFound();

            var students = await _context.Students
                .Where(s => s.GroupID == GroupID && s.Status == true)
                .OrderBy(s => s.LastName)
                .ToListAsync();

            var typeEKZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Экзамен");
            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");
            var typeIO = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая отметка");
            var typeZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Зачёт");
            var typeDZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Дифференцированный зачёт");

            var excludedTypeIds = new[]
            {
                typeEKZ?.TypeOfExerciseID,
                typeKR?.TypeOfExerciseID,
                typeKP?.TypeOfExerciseID,
                typeIO?.TypeOfExerciseID,
                typeZ?.TypeOfExerciseID,
                typeDZ?.TypeOfExerciseID
            }
            .OfType<int>() // Автоматически фильтрует null и преобразует в int
            .ToList();

            var lessons = await _context.Lessons
               .Where(l => l.GroupID == GroupID
                           && l.Theme.SubjectID == SubjectID
                           && l.Date >= d1  // Дата урока >= d1
                           && l.Date <= d2  // Дата урока <= d2
                           && !excludedTypeIds.Contains(l.TypeOfExerciseID))
               .Include(l => l.Theme)
               .Include(l => l.TypeOfExercise)
               .OrderBy(l => l.Date)
               .ToListAsync();

            var marks = await _context.Marks
                .Where(m => m.GroupID == GroupID && 
                    m.SubjectID == SubjectID &&
                    !excludedTypeIds.Contains(m.TypeOfExerciseID) &&
                    m.Date >= d1 &&
                    m.Date <= d2
                )
                .ToListAsync();

            string templatePath = Path.Combine(_env.WebRootPath, "template", "journal_template.xlsx");

            if (!System.IO.File.Exists(templatePath))
                return NotFound("Шаблон журнала не найден");

            using (var package = new ExcelPackage(new FileInfo(templatePath)))
            {
                // Конфигурация на основе шаблона
                int maxStudentsPerPage = 25; // 25 строк для студентов (строки 6-30)
                int studentStartRow = 6;     // строка с первым студентом

                // Колонки для № и ФИО
                int studentNumberColumn = 1; // колонка A для №
                int studentNameColumn = 2;   // колонка B для ФИО

                // Количество занятий на странице и начальные колонки
                int maxLessonsOddPage = 6;    // нечетная страница (лист 1,3,5...)
                int oddPageStartColumn = 7;   // колонка G (7) для нечетных страниц

                int maxLessonsEvenPage = 12;  // четная страница (лист 2,4,6...)
                int evenPageStartColumn = 2;  // колонка B (2) для четных страниц

                // Получаем шаблоны страниц из файла
                var oddTemplate = package.Workbook.Worksheets[0];  // первый лист (нечетный)
                oddTemplate.Name = "Страница 1";

                // Создаем четный шаблон если его нет
                ExcelWorksheet evenTemplate;
                if (package.Workbook.Worksheets.Count > 1)
                {
                    evenTemplate = package.Workbook.Worksheets[1];
                    evenTemplate.Name = "Страница 2";
                }
                else
                {
                    evenTemplate = package.Workbook.Worksheets.Add("Страница 2", oddTemplate);
                }

                // РАСПРЕДЕЛЯЕМ ЗАНЯТИЯ ПО СТРАНИЦАМ
                List<int> lessonsPerPage = new List<int>();
                int lessonIndex = 0;
                int pageNumber = 1; // начинаем с страницы 1

                while (lessonIndex < lessons.Count)
                {
                    bool isOddPage = (pageNumber % 2 == 1); // нечетная: 1,3,5...
                    int maxLessonsOnPage = isOddPage ? maxLessonsOddPage : maxLessonsEvenPage;

                    int lessonsThisPage = Math.Min(maxLessonsOnPage, lessons.Count - lessonIndex);
                    lessonsPerPage.Add(lessonsThisPage);

                    lessonIndex += lessonsThisPage;
                    pageNumber++;
                }

                int totalPages = lessonsPerPage.Count; // общее количество страниц для занятий

                // Создаем дополнительные страницы если нужно (начиная со страницы 3)
                for (int i = 3; i <= totalPages; i++)
                {
                    string sheetName = $"Страница {i}";

                    // Определяем тип страницы
                    bool pageIsOdd = (i % 2 == 1); // нечетная: 1,3,5...
                    var template = pageIsOdd ? oddTemplate : evenTemplate;

                    // Удаляем лист если уже существует с таким именем
                    if (package.Workbook.Worksheets.Any(ws => ws.Name == sheetName))
                    {
                        package.Workbook.Worksheets.Delete(sheetName);
                    }

                    // Создаем новый лист из соответствующего шаблона
                    package.Workbook.Worksheets.Add(sheetName, template);
                }

                // ЗАПОЛНЯЕМ ВСЕ СТРАНИЦЫ
                lessonIndex = 0; // сбрасываем индекс занятий

                for (int currentPage = 1; currentPage <= totalPages; currentPage++)
                {
                    var sheet = package.Workbook.Worksheets[currentPage - 1]; // -1 т.к. индексация с 0

                    bool currentPageIsOdd = (currentPage % 2 == 1); // нечетная страница

                    // Сколько занятий на этой странице
                    int pageLessonIndex = currentPage - 1;
                    int lessonsThisPage = lessonsPerPage[pageLessonIndex];

                    // Определяем начальную колонку для занятий
                    int startColumn = currentPageIsOdd ? oddPageStartColumn : evenPageStartColumn;

                    // ЗАГОЛОВОК ДИСЦИПЛИНЫ - РАЗНЫЕ ЯЧЕЙКИ
                    string title = subject.ShortName ?? subject.Name;

                    if (currentPageIsOdd)
                    {
                        // Нечетная страница - ячейка F2
                        var titleCell = sheet.Cells["F2"];
                        titleCell.Value = title;
                        titleCell.Style.Font.Name = "Times New Roman";
                        titleCell.Style.Font.Size = 10;
                        titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    else
                    {
                        // Четная страница - ячейка G2 (колонка 7)
                        var titleCell = sheet.Cells["G2"];
                        titleCell.Value = title;
                        titleCell.Style.Font.Name = "Times New Roman";
                        titleCell.Style.Font.Size = 10;
                        titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    // ЗАПОЛНЯЕМ ЗАГОЛОВКИ ЗАНЯТИЙ НА ЭТОЙ СТРАНИЦЕ
                    for (int i = 0; i < lessonsThisPage; i++)
                    {
                        int lessonIdx = lessonIndex + i;
                        if (lessonIdx >= lessons.Count) break;

                        var lesson = lessons[lessonIdx];
                        int column = startColumn + i;

                        // Заполняем нижнюю ячейку (строка 5) - дата, тема, вид
                        var infoCell = sheet.Cells[5, column];
                        infoCell.Value = FormatLessonInfo(lesson);

                        // ПОВОРАЧИВАЕМ ТЕКСТ НА 90 ГРАДУСОВ
                        infoCell.Style.TextRotation = 90;
                        infoCell.Style.WrapText = true;
                        infoCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        infoCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        // ШРИФТ Times New Roman 8
                        infoCell.Style.Font.Name = "Times New Roman";
                        infoCell.Style.Font.Size = 8;
                    }

                    // ЗАПОЛНЯЕМ СТУДЕНТОВ И ОЦЕНКИ НА ЭТОЙ СТРАНИЦЕ
                    // На КАЖДОЙ странице заполняем ВСЕХ студентов (1-25)
                    for (int studentIdx = 0; studentIdx < Math.Min(students.Count, maxStudentsPerPage); studentIdx++)
                    {
                        var student = students[studentIdx];
                        int row = studentStartRow + studentIdx;

                        // НЕ ТРОГАЕМ НУМЕРАЦИЮ СТУДЕНТОВ! Она уже есть в шаблоне

                        // ФИО (колонка B) - заполняем на ВСЕХ страницах ПОЛНОЕ ФИО
                        var nameCell = sheet.Cells[row, studentNameColumn];
                        nameCell.Value = FormatStudentNameFull(student);

                        // ВЫРАВНИВАЕМ ФАМИЛИИ ПО ЛЕВОМУ КРАЮ
                        nameCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        nameCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        // ШРИФТ Times New Roman 10
                        nameCell.Style.Font.Name = "Times New Roman";
                        nameCell.Style.Font.Size = 10;

                        // ОЦЕНКИ за занятия на этой странице
                        for (int lessonIdx = 0; lessonIdx < lessonsThisPage; lessonIdx++)
                        {
                            int lessonIdxGlobal = lessonIndex + lessonIdx;
                            if (lessonIdxGlobal >= lessons.Count) break;

                            var lesson = lessons[lessonIdxGlobal];
                            int column = startColumn + lessonIdx;

                            // Ищем оценку
                            var mark = marks.FirstOrDefault(m =>
                                m.StudentID == student.StudentID &&
                                m.LessonID == lesson.LessonID);

                            // Заполняем оценку
                            var markCell = sheet.Cells[row, column];
                            markCell.Value = mark?.Value ?? "";

                            // ВЫРАВНИВАЕМ ОТМЕТКИ ПО ЦЕНТРУ ЯЧЕЙКИ
                            markCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            markCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            // ШРИФТ Times New Roman 10 для оценок
                            markCell.Style.Font.Name = "Times New Roman";
                            markCell.Style.Font.Size = 10;
                        }
                    }

                    // Переходим к следующей порции занятий
                    lessonIndex += lessonsThisPage;
                }

                // ЕСЛИ СТУДЕНТОВ БОЛЬШЕ 25 - СОЗДАЕМ ДОПОЛНИТЕЛЬНЫЕ СТРАНИЦЫ
                if (students.Count > maxStudentsPerPage)
                {
                    int totalStudentPages = (int)Math.Ceiling((double)students.Count / maxStudentsPerPage);

                    // Для каждой дополнительной порции студентов (начиная со 2-й)
                    for (int studentPage = 2; studentPage <= totalStudentPages; studentPage++)
                    {
                        int studentOffset = (studentPage - 1) * maxStudentsPerPage;
                        int studentsThisPage = Math.Min(maxStudentsPerPage, students.Count - studentOffset);

                        // Создаем новый блок страниц для этих студентов
                        // Нужно повторить ВСЕ страницы с занятиями для этих студентов
                        int currentLessonIndex = 0; // начинаем занятия сначала

                        for (int lessonPage = 0; lessonPage < totalPages; lessonPage++)
                        {
                            int newPageNumber = totalPages * (studentPage - 1) + lessonPage + 1;
                            int lessonsThisPage = lessonsPerPage[lessonPage];
                            bool pageIsOdd = (newPageNumber % 2 == 1);

                            // Определяем начальную колонку для занятий
                            int startColumn = pageIsOdd ? oddPageStartColumn : evenPageStartColumn;

                            // Выбираем шаблон
                            var template = pageIsOdd ? oddTemplate : evenTemplate;

                            // Имя новой страницы
                            string newSheetName = $"Страница {newPageNumber}";

                            // Создаем новую страницу
                            var newSheet = package.Workbook.Worksheets.Add(newSheetName, template);

                            // ЗАГОЛОВОК ДИСЦИПЛИНЫ
                            string title = subject.ShortName ?? subject.Name;

                            if (pageIsOdd)
                            {
                                // Нечетная страница - ячейка F2
                                var titleCell = newSheet.Cells["F2"];
                                titleCell.Value = title;
                                titleCell.Style.Font.Name = "Times New Roman";
                                titleCell.Style.Font.Size = 10;
                                titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            }
                            else
                            {
                                // Четная страница - ячейка G2 (колонка 7)
                                var titleCell = newSheet.Cells["G2"];
                                titleCell.Value = title;
                                titleCell.Style.Font.Name = "Times New Roman";
                                titleCell.Style.Font.Size = 10;
                                titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            }

                            // ЗАПОЛНЯЕМ ЗАГОЛОВКИ ЗАНЯТИЙ
                            for (int i = 0; i < lessonsThisPage; i++)
                            {
                                int lessonIdx = currentLessonIndex + i;
                                if (lessonIdx >= lessons.Count) break;

                                var lesson = lessons[lessonIdx];
                                int column = startColumn + i;

                                var infoCell = newSheet.Cells[5, column];
                                infoCell.Value = FormatLessonInfo(lesson);

                                // ПОВОРАЧИВАЕМ ТЕКСТ НА 90 ГРАДУСОВ
                                infoCell.Style.TextRotation = 90;
                                infoCell.Style.WrapText = true;
                                infoCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                infoCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                                // ШРИФТ Times New Roman 8
                                infoCell.Style.Font.Name = "Times New Roman";
                                infoCell.Style.Font.Size = 8;
                            }

                            // ЗАПОЛНЯЕМ СТУДЕНТОВ (текущая порция)
                            for (int i = 0; i < studentsThisPage; i++)
                            {
                                int studentIdx = studentOffset + i;
                                if (studentIdx >= students.Count) break;

                                var student = students[studentIdx];
                                int row = studentStartRow + i;

                                // НЕ ТРОГАЕМ КОЛОНКУ A - нумерация уже есть в шаблоне

                                // ФИО - ПОЛНОЕ ФИО
                                var nameCell = newSheet.Cells[row, studentNameColumn];
                                nameCell.Value = FormatStudentNameFull(student);

                                // ВЫРАВНИВАЕМ ФАМИЛИИ ПО ЛЕВОМУ КРАЮ
                                nameCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                                nameCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                                // ШРИФТ Times New Roman 10
                                nameCell.Style.Font.Name = "Times New Roman";
                                nameCell.Style.Font.Size = 10;

                                // ОЦЕНКИ
                                for (int j = 0; j < lessonsThisPage; j++)
                                {
                                    int lessonIdx = currentLessonIndex + j;
                                    if (lessonIdx >= lessons.Count) break;

                                    var lesson = lessons[lessonIdx];
                                    int column = startColumn + j;

                                    var mark = marks.FirstOrDefault(m =>
                                        m.StudentID == student.StudentID &&
                                        m.LessonID == lesson.LessonID);

                                    var markCell = newSheet.Cells[row, column];
                                    markCell.Value = mark?.Value ?? "";

                                    // ВЫРАВНИВАЕМ ОТМЕТКИ ПО ЦЕНТРУ ЯЧЕЙКИ
                                    markCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                    markCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                                    // ШРИФТ Times New Roman 10 для оценок
                                    markCell.Style.Font.Name = "Times New Roman";
                                    markCell.Style.Font.Size = 10;
                                }
                            }

                            currentLessonIndex += lessonsThisPage;
                        }
                    }
                }

                using (var memoryStream = new MemoryStream())
                {
                    package.SaveAs(memoryStream);
                    memoryStream.Position = 0;

                    var fileName = $"Журнал_{group.Name}_{subject.Name}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(memoryStream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }

        // Вспомогательные методы
        private string FormatLessonInfo(Lesson lesson)
        {
            string date = lesson.Date.ToString("dd.MM.yyyy");
            string theme = lesson.Theme?.ShortName ?? lesson.Theme?.Name ?? "Тема";
            string type = lesson.TypeOfExercise?.ShortName ?? "Вид";

            // Форматирование для вертикального отображения
            if (theme.Length > 15) theme = theme.Substring(0, 12) + "..";

            return $"{date}\n{theme}\n{type}";
        }

        private string FormatStudentNameFull(Student student)
        {
            if (string.IsNullOrEmpty(student.Name))
                return student.LastName ?? "";

            if (string.IsNullOrEmpty(student.Surname))
                return $"{student.LastName} {student.Name}";

            // ПОЛНОЕ ФИО без сокращений
            return $"{student.LastName} {student.Name} {student.Surname}";
        }

        //Учебные года
        private async Task<List<AcademicYearInfo>> GetAcademicYearsForSubjectAndGroup(int groupID, int subjectID)
        {
            // Получаем даты уроков для конкретной группы и предмета
            var lessonDates = await _context.Lessons
                .Where(l => l.GroupID == groupID && l.Theme.SubjectID == subjectID)
                .Select(l => l.Date)
                .Distinct()
                .ToListAsync();

            if (!lessonDates.Any())
                return new List<AcademicYearInfo>();

            var minDate = lessonDates.Min();
            var maxDate = lessonDates.Max();

            return GenerateAcademicYears(minDate, maxDate);
        }

        private List<AcademicYearInfo> GenerateAcademicYears(DateTime fromDate, DateTime toDate)
        {
            var academicYears = new List<AcademicYearInfo>();

            // Определяем первый учебный год
            int startYear = fromDate.Month >= 9 ? fromDate.Year : fromDate.Year - 1;

            // Определяем последний учебный год
            int endYear = toDate.Month >= 9 ? toDate.Year : toDate.Year - 1;

            // Генерируем учебные годы
            for (int year = startYear; year <= endYear; year++)
            {
                var academicYear = new AcademicYearInfo
                {
                    StartDate = new DateTime(year, 9, 1),
                    EndDate = new DateTime(year + 1, 7, 31),
                    Name = $"{year}/{year + 1}"
                };

                academicYears.Add(academicYear);
            }

            return academicYears.OrderByDescending(ay => ay.StartDate).ToList();
        }
    }
}