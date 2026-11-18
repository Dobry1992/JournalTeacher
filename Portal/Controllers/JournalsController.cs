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
        private readonly ILogger<JournalsController> _logger;

        public JournalsController(
            AcademyContext context,
            IWebHostEnvironment env,
            ILogger<JournalsController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
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

        // ============================================================
        // ЖУРНАЛ (с поддержкой подгрупп)
        // ============================================================

        public async Task<IActionResult> Journal(int GroupID, int SubjectID, string subgroup = null)
        {
            return await PrepareJournalViewWithSubgroup(GroupID, SubjectID, "Journal", subgroup);
        }

        [Authorize(Roles = "SuperAdmin,ANB-UMCH")]
        public async Task<IActionResult> AdjustedJournal(int GroupID, int SubjectID, string subgroup = null)
        {
            var academicYears = await GetAcademicYearsForSubjectAndGroup(GroupID, SubjectID);
            ViewBag.AcademicYears = academicYears;

            return await PrepareJournalViewWithSubgroup(GroupID, SubjectID, "AdjustedJournal", subgroup);
        }

        // ============================================================
        // ВЫБОР ПОДГРУППЫ / ПОДГОТОВКА ЖУРНАЛА
        // ============================================================

        /// <summary>
        /// Решает: показать страницу выбора подгруппы или сразу журнал.
        /// subgroup == "__all__" — принудительно «все студенты».
        /// </summary>
        private async Task<IActionResult> PrepareJournalViewWithSubgroup(
            int GroupID, int SubjectID, string viewName, string subgroup)
        {
            // Escape-hatch «показать всех»
            if (subgroup == "__all__")
                return await PrepareJournalView(GroupID, SubjectID, viewName, null);

            var subgroupNames = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == GroupID && sl.SubjectID == SubjectID)
                .Select(sl => sl.Name)
                .Distinct()
                .ToListAsync();

            // Подгруппы есть, но конкретная не выбрана → страница выбора
            if (subgroupNames.Any() && string.IsNullOrEmpty(subgroup))
            {
                var group = await _context.Groups.FindAsync(GroupID);
                var subject = await _context.Subjects.FindAsync(SubjectID);

                if (group == null || subject == null)
                    return NotFound();

                var counts = await _context.SubgroupLinks
                    .Where(sl => sl.GroupID == GroupID && sl.SubjectID == SubjectID)
                    .GroupBy(sl => sl.Name)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .ToListAsync();

                var model = new SubgroupSelectionViewModel
                {
                    GroupID = GroupID,
                    SubjectID = SubjectID,
                    GroupName = group.Name,
                    SubjectName = subject.Name,
                    ReturnAction = viewName,
                    Subgroups = counts
                        .OrderBy(c => c.Name)
                        .Select(c => new SubgroupOption { Name = c.Name, StudentCount = c.Count })
                        .ToList()
                };

                return View("SelectSubgroup", model);
            }

            return await PrepareJournalView(GroupID, SubjectID, viewName, subgroup);
        }

        private List<JournalMarks> BuildJournalMarks(List<Mark> marks, Dictionary<string, int> types)
        {
            var list = new List<JournalMarks>();
            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            var previousMonth = currentMonth.AddMonths(-1);
            var tenthOfCurrentMonth = new DateTime(today.Year, today.Month, 9);

            int semester = GetSemester(today);

            var allowedTypes = _context.Types
                .Where(t => t.Name == "Экзамен" || t.Name == "Зачёт" || t.Name == "Дифференцированный зачёт")
                .Select(t => t.TypeOfExerciseID)
                .ToList();

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

        private async Task<IActionResult> PrepareJournalView(
            int GroupID, int SubjectID, string viewName, string subgroup = null)
        {
            var group = await _context.Groups.FindAsync(GroupID);
            var subject = await _context.Subjects.FindAsync(SubjectID);
            bool isActive = true;
            var completedSubject = await _context.CompletedSubjects.FirstOrDefaultAsync(cs => cs.GroupID == GroupID && cs.SubjectID == SubjectID);

            if (completedSubject != null) isActive = false;

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

            // ============================================================
            // ФИЛЬТРАЦИЯ СТУДЕНТОВ ПО ПОДГРУППЕ
            // ============================================================
            var subgroupNames = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == GroupID && sl.SubjectID == SubjectID)
                .Select(sl => sl.Name)
                .Distinct()
                .ToListAsync();

            bool hasSubgroups = subgroupNames.Any();

            // Если subgroup задан, но такого нет — игнорируем фильтр
            if (!string.IsNullOrEmpty(subgroup) && !subgroupNames.Contains(subgroup))
                subgroup = null;

            var studentsQuery = _context.Students
                .Where(s => s.GroupID == GroupID && s.Status == true);

            if (!string.IsNullOrEmpty(subgroup))
            {
                var subgroupStudentIds = await _context.SubgroupLinks
                    .Where(sl => sl.GroupID == GroupID
                              && sl.SubjectID == SubjectID
                              && sl.Name == subgroup)
                    .Select(sl => sl.StudentID)
                    .ToListAsync();

                studentsQuery = studentsQuery.Where(s => subgroupStudentIds.Contains(s.StudentID));
            }

            var students = await studentsQuery
                .OrderBy(s => s.LastName)
                .AsNoTracking()
                .ToListAsync();
            // ============================================================

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

                var todayLocal = DateTime.Today;
                int semesterNow = GetSemester(todayLocal);
                int semesterOfStat = GetSemester(statlesson.Date);
                if (semesterOfStat != semesterNow)
                {
                    journalLesson.CollapseId = "collapse";
                }
                else
                {
                    if (todayLocal.Year != statlesson.Date.Year)
                    {
                        journalLesson.CollapseId = "collapse";
                    }
                    else
                    {
                        journalLesson.CollapseId = "";
                    }
                }

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
                StatementLessons = statementLessons,
                IsActive = isActive,

                Subgroup = subgroup,
                HasSubgroups = hasSubgroups,
                SubgroupNames = subgroupNames.OrderBy(n => n).ToList(),
                ReturnAction = viewName
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

        public async Task<IActionResult> ExportToExcel(int GroupID, int SubjectID, DateTime d1, DateTime d2)
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
            .OfType<int>()
            .ToList();

            var lessons = await _context.Lessons
               .Where(l => l.GroupID == GroupID
                           && l.Theme.SubjectID == SubjectID
                           && l.Date >= d1
                           && l.Date <= d2
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
                int maxStudentsPerPage = 25;
                int studentStartRow = 6;
                int studentNumberColumn = 1;
                int studentNameColumn = 2;
                int maxLessonsOddPage = 6;
                int oddPageStartColumn = 7;
                int maxLessonsEvenPage = 12;
                int evenPageStartColumn = 2;

                var oddTemplate = package.Workbook.Worksheets[0];
                oddTemplate.Name = "Страница 1";

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

                List<int> lessonsPerPage = new List<int>();
                int lessonIndex = 0;
                int pageNumber = 1;

                while (lessonIndex < lessons.Count)
                {
                    bool isOddPage = (pageNumber % 2 == 1);
                    int maxLessonsOnPage = isOddPage ? maxLessonsOddPage : maxLessonsEvenPage;

                    int lessonsThisPage = Math.Min(maxLessonsOnPage, lessons.Count - lessonIndex);
                    lessonsPerPage.Add(lessonsThisPage);

                    lessonIndex += lessonsThisPage;
                    pageNumber++;
                }

                int totalPages = lessonsPerPage.Count;

                for (int i = 3; i <= totalPages; i++)
                {
                    string sheetName = $"Страница {i}";

                    bool pageIsOdd = (i % 2 == 1);
                    var template = pageIsOdd ? oddTemplate : evenTemplate;

                    if (package.Workbook.Worksheets.Any(ws => ws.Name == sheetName))
                    {
                        package.Workbook.Worksheets.Delete(sheetName);
                    }

                    package.Workbook.Worksheets.Add(sheetName, template);
                }

                lessonIndex = 0;

                for (int currentPage = 1; currentPage <= totalPages; currentPage++)
                {
                    var sheet = package.Workbook.Worksheets[currentPage - 1];

                    bool currentPageIsOdd = (currentPage % 2 == 1);

                    int pageLessonIndex = currentPage - 1;
                    int lessonsThisPage = lessonsPerPage[pageLessonIndex];

                    int startColumn = currentPageIsOdd ? oddPageStartColumn : evenPageStartColumn;

                    string title = subject.ShortName ?? subject.Name;

                    if (currentPageIsOdd)
                    {
                        var titleCell = sheet.Cells["F2"];
                        titleCell.Value = title;
                        titleCell.Style.Font.Name = "Times New Roman";
                        titleCell.Style.Font.Size = 10;
                        titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    else
                    {
                        var titleCell = sheet.Cells["G2"];
                        titleCell.Value = title;
                        titleCell.Style.Font.Name = "Times New Roman";
                        titleCell.Style.Font.Size = 10;
                        titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    for (int i = 0; i < lessonsThisPage; i++)
                    {
                        int lessonIdx = lessonIndex + i;
                        if (lessonIdx >= lessons.Count) break;

                        var lesson = lessons[lessonIdx];
                        int column = startColumn + i;

                        var infoCell = sheet.Cells[5, column];
                        infoCell.Value = FormatLessonInfo(lesson);

                        infoCell.Style.TextRotation = 90;
                        infoCell.Style.WrapText = true;
                        infoCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        infoCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        infoCell.Style.Font.Name = "Times New Roman";
                        infoCell.Style.Font.Size = 8;
                    }

                    for (int studentIdx = 0; studentIdx < Math.Min(students.Count, maxStudentsPerPage); studentIdx++)
                    {
                        var student = students[studentIdx];
                        int row = studentStartRow + studentIdx;

                        var nameCell = sheet.Cells[row, studentNameColumn];
                        nameCell.Value = FormatStudentNameFull(student);
                        nameCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        nameCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        nameCell.Style.Font.Name = "Times New Roman";
                        nameCell.Style.Font.Size = 10;

                        for (int lessonIdx = 0; lessonIdx < lessonsThisPage; lessonIdx++)
                        {
                            int lessonIdxGlobal = lessonIndex + lessonIdx;
                            if (lessonIdxGlobal >= lessons.Count) break;

                            var lesson = lessons[lessonIdxGlobal];
                            int column = startColumn + lessonIdx;

                            var mark = marks.FirstOrDefault(m =>
                                m.StudentID == student.StudentID &&
                                m.LessonID == lesson.LessonID);

                            var markCell = sheet.Cells[row, column];
                            markCell.Value = mark?.Value ?? "";
                            markCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            markCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            markCell.Style.Font.Name = "Times New Roman";
                            markCell.Style.Font.Size = 10;
                        }
                    }

                    lessonIndex += lessonsThisPage;
                }

                if (students.Count > maxStudentsPerPage)
                {
                    int totalStudentPages = (int)Math.Ceiling((double)students.Count / maxStudentsPerPage);

                    for (int studentPage = 2; studentPage <= totalStudentPages; studentPage++)
                    {
                        int studentOffset = (studentPage - 1) * maxStudentsPerPage;
                        int studentsThisPage = Math.Min(maxStudentsPerPage, students.Count - studentOffset);

                        int currentLessonIndex = 0;

                        for (int lessonPage = 0; lessonPage < totalPages; lessonPage++)
                        {
                            int newPageNumber = totalPages * (studentPage - 1) + lessonPage + 1;
                            int lessonsThisPage = lessonsPerPage[lessonPage];
                            bool pageIsOdd = (newPageNumber % 2 == 1);

                            int startColumn = pageIsOdd ? oddPageStartColumn : evenPageStartColumn;
                            var template = pageIsOdd ? oddTemplate : evenTemplate;
                            string newSheetName = $"Страница {newPageNumber}";

                            var newSheet = package.Workbook.Worksheets.Add(newSheetName, template);

                            string title = subject.ShortName ?? subject.Name;

                            if (pageIsOdd)
                            {
                                var titleCell = newSheet.Cells["F2"];
                                titleCell.Value = title;
                                titleCell.Style.Font.Name = "Times New Roman";
                                titleCell.Style.Font.Size = 10;
                                titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            }
                            else
                            {
                                var titleCell = newSheet.Cells["G2"];
                                titleCell.Value = title;
                                titleCell.Style.Font.Name = "Times New Roman";
                                titleCell.Style.Font.Size = 10;
                                titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            }

                            for (int i = 0; i < lessonsThisPage; i++)
                            {
                                int lessonIdx = currentLessonIndex + i;
                                if (lessonIdx >= lessons.Count) break;

                                var lesson = lessons[lessonIdx];
                                int column = startColumn + i;

                                var infoCell = newSheet.Cells[5, column];
                                infoCell.Value = FormatLessonInfo(lesson);
                                infoCell.Style.TextRotation = 90;
                                infoCell.Style.WrapText = true;
                                infoCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                infoCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                infoCell.Style.Font.Name = "Times New Roman";
                                infoCell.Style.Font.Size = 8;
                            }

                            for (int i = 0; i < studentsThisPage; i++)
                            {
                                int studentIdx = studentOffset + i;
                                if (studentIdx >= students.Count) break;

                                var student = students[studentIdx];
                                int row = studentStartRow + i;

                                var nameCell = newSheet.Cells[row, studentNameColumn];
                                nameCell.Value = FormatStudentNameFull(student);
                                nameCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                                nameCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                nameCell.Style.Font.Name = "Times New Roman";
                                nameCell.Style.Font.Size = 10;

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
                                    markCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                    markCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
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

        private string FormatLessonInfo(Lesson lesson)
        {
            string date = lesson.Date.ToString("dd.MM.yyyy");
            string theme = lesson.Theme?.ShortName ?? lesson.Theme?.Name ?? "Тема";
            string type = lesson.TypeOfExercise?.ShortName ?? "Вид";

            if (theme.Length > 15) theme = theme.Substring(0, 12) + "..";

            return $"{date}\n{theme}\n{type}";
        }

        private string FormatStudentNameFull(Student student)
        {
            if (string.IsNullOrEmpty(student.Name))
                return student.LastName ?? "";

            if (string.IsNullOrEmpty(student.Surname))
                return $"{student.LastName} {student.Name}";

            return $"{student.LastName} {student.Name} {student.Surname}";
        }

        private async Task<List<AcademicYearInfo>> GetAcademicYearsForSubjectAndGroup(int groupID, int subjectID)
        {
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

            int startYear = fromDate.Month >= 9 ? fromDate.Year : fromDate.Year - 1;
            int endYear = toDate.Month >= 9 ? toDate.Year : toDate.Year - 1;

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