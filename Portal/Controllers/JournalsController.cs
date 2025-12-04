using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
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

        public async Task<IActionResult> ExportToExcel(int GroupID, int SubjectID)
        {
            var group = await _context.Groups.FindAsync(GroupID);
            var subject = await _context.Subjects.FindAsync(SubjectID);

            if (group == null || subject == null)
                return NotFound();

            var students = await _context.Students
                .Where(s => s.GroupID == GroupID && s.Status == true)
                .OrderBy(s => s.LastName)
                .ToListAsync();

            var lessons = await _context.Lessons
                .Where(l => l.GroupID == GroupID && l.Theme.SubjectID == SubjectID)
                .Include(l => l.Theme)
                .Include(l => l.TypeOfExercise)
                .OrderBy(l => l.Date)
                .ToListAsync();

            var marks = await _context.Marks
                .Where(m => m.GroupID == GroupID && m.SubjectID == SubjectID)
                .ToListAsync();

            string templatePath = Path.Combine(_env.WebRootPath, "template", "journal_template.xlsx");

            if (!System.IO.File.Exists(templatePath))
                return NotFound("Шаблон журнала не найден");

            using (var package = new ExcelPackage(new FileInfo(templatePath)))
            {
                // Конфигурация на основе вашего шаблона
                int maxStudentsPerPage = 25; // 25 строк для студентов (строки 6-30)
                int studentStartRow = 6;     // строка с первым студентом
                int studentNumberColumn = 1; // колонка A для №
                int studentNameColumn = 2;   // колонка B для ФИО

                // Количество занятий на странице и начальные колонки
                int maxLessonsOddPage = 14;   // нечетная страница (лист 1)
                int oddPageStartColumn = 7;   // колонка G (7) для нечетных страниц

                int maxLessonsEvenPage = 26;  // четная страница (лист 2)
                int evenPageStartColumn = 2;  // колонка B (2) для четных страниц

                // Получаем шаблоны страниц
                var oddTemplate = package.Workbook.Worksheets[0];  // первый лист (нечетный)
                var evenTemplate = package.Workbook.Worksheets.Count > 1
                    ? package.Workbook.Worksheets[1]  // второй лист (четный)
                    : oddTemplate;                    // если нет второго, используем первый

                // Переименовываем первый лист
                oddTemplate.Name = "Страница 1";

                // Рассчитываем количество страниц для занятий
                List<int> lessonsPerPage = new List<int>();
                int remainingLessons = lessons.Count;
                bool isOddPage = true; // начинаем с нечетной

                while (remainingLessons > 0)
                {
                    int lessonsOnPage = isOddPage ? maxLessonsOddPage : maxLessonsEvenPage;
                    lessonsOnPage = Math.Min(lessonsOnPage, remainingLessons);
                    lessonsPerPage.Add(lessonsOnPage);
                    remainingLessons -= lessonsOnPage;
                    isOddPage = !isOddPage;
                }

                int totalPages = lessonsPerPage.Count;

                // Создаем дополнительные страницы если нужно (начиная со 2-й)
                for (int page = 2; page <= totalPages; page++)
                {
                    string sheetName = $"Страница {page}";

                    // Определяем шаблон для этой страницы
                    bool pageIsOdd = ((page - 1) % 2 == 0); // страница 1,3,5... нечетные
                    var template = pageIsOdd ? oddTemplate : evenTemplate;

                    // Удаляем лист если уже существует с таким именем
                    if (package.Workbook.Worksheets.Any(ws => ws.Name == sheetName))
                    {
                        package.Workbook.Worksheets.Delete(sheetName);
                    }

                    // Создаем новый лист из соответствующего шаблона
                    var newSheet = package.Workbook.Worksheets.Add(sheetName, template);

                    // Копируем заголовок дисциплины
                    newSheet.Cells["B2"].Value = template.Cells["B2"].Value;
                }

                // Заполняем все страницы
                int lessonOffset = 0;

                for (int page = 0; page < totalPages; page++)
                {
                    var sheet = package.Workbook.Worksheets[page];
                    int lessonsThisPage = lessonsPerPage[page];
                    bool currentPageIsOdd = ((page) % 2 == 0); // страница 0,2,4... нечетные

                    // Определяем начальную колонку для занятий на этой странице
                    int startColumn = currentPageIsOdd ? oddPageStartColumn : evenPageStartColumn;

                    // Заголовок дисциплины
                    string title = page == 0 ?
                        $"{subject.Name} ({group.Name})" :
                        $"{subject.Name} ({group.Name}) - продолжение {page + 1}";
                    sheet.Cells["B2"].Value = title;
                    sheet.Cells["B2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    sheet.Cells["B2"].Style.Font.Bold = true;

                    // Заполняем заголовки занятий (только нижние ячейки - дата, тема, вид)
                    // Колонки идут подряд от начальной колонки
                    for (int i = 0; i < lessonsThisPage; i++)
                    {
                        int lessonIndex = lessonOffset + i;
                        if (lessonIndex >= lessons.Count) break;

                        var lesson = lessons[lessonIndex];

                        // Колонка для занятия
                        int column = startColumn + i;

                        // Заполняем только нижнюю ячейку (строка 5) - дата, тема, вид
                        var infoCell = sheet.Cells[5, column];
                        infoCell.Value = FormatLessonInfo(lesson);

                        // Поворачиваем текст на 90 градусов
                        infoCell.Style.TextRotation = 90;
                        infoCell.Style.WrapText = true;
                        infoCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        infoCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        infoCell.Style.Font.Size = 8; // уменьшаем шрифт для вертикального текста

                        // Верхнюю ячейку (строка 4) не трогаем - она уже заполнена в шаблоне
                        // Убеждаемся, что в ней остался текст "Преподаватель подпись"
                        var teacherCell = sheet.Cells[4, column];
                        if (string.IsNullOrEmpty(teacherCell.Text))
                        {
                            teacherCell.Style.WrapText = true;
                            teacherCell.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                            teacherCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            teacherCell.Style.Font.Size = 8;
                        }
                    }

                    // Заполняем студентов и оценки (только первые 25 студентов на странице)
                    for (int studentIdx = 0; studentIdx < Math.Min(students.Count, maxStudentsPerPage); studentIdx++)
                    {
                        var student = students[studentIdx];
                        int row = studentStartRow + studentIdx;

                        // № п/п (колонка A) - только на нечетных страницах
                        if (currentPageIsOdd)
                        {
                            sheet.Cells[row, studentNumberColumn].Value = studentIdx + 1;
                            sheet.Cells[row, studentNumberColumn].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }
                        else
                        {
                            // На четных страницах колонка A может быть занята или пустой
                            // Оставляем как в шаблоне
                        }

                        // ФИО (колонка B)
                        sheet.Cells[row, studentNameColumn].Value = FormatStudentName(student);

                        // Оценки за занятия на этой странице
                        for (int lessonIdx = 0; lessonIdx < lessonsThisPage; lessonIdx++)
                        {
                            int lessonIndex = lessonOffset + lessonIdx;
                            if (lessonIndex >= lessons.Count) break;

                            var lesson = lessons[lessonIndex];

                            // Колонка для оценки
                            int column = startColumn + lessonIdx;

                            // Ищем оценку
                            var mark = marks.FirstOrDefault(m =>
                                m.StudentID == student.StudentID &&
                                m.LessonID == lesson.LessonID);

                            // Заполняем оценку
                            var markCell = sheet.Cells[row, column];
                            markCell.Value = mark?.Value ?? "";

                            // Центрируем оценку
                            markCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            markCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            // Делаем оценку жирной если она есть
                            if (!string.IsNullOrEmpty(mark?.Value))
                            {
                                markCell.Style.Font.Bold = true;
                            }
                        }
                    }

                    lessonOffset += lessonsThisPage;
                }

                // Если студентов больше 25, создаем продолжения
                if (students.Count > maxStudentsPerPage)
                {
                    int studentPages = (int)Math.Ceiling((double)students.Count / maxStudentsPerPage);

                    // Для каждой порции студентов создаем новые страницы
                    for (int studentPage = 2; studentPage <= studentPages; studentPage++)
                    {
                        int studentOffset = (studentPage - 1) * maxStudentsPerPage;
                        int studentsOnThisPage = Math.Min(maxStudentsPerPage, students.Count - studentOffset);

                        // Создаем новый блок страниц для этой порции студентов
                        int currentLessonOffset = 0; // сбрасываем смещение занятий для этого блока

                        for (int lessonPage = 0; lessonPage < totalPages; lessonPage++)
                        {
                            int continuationPageIndex = totalPages * (studentPage - 1) + lessonPage;
                            int lessonsThisPage = lessonsPerPage[lessonPage];
                            bool pageIsOdd = ((lessonPage) % 2 == 0); // определяем тип страницы

                            // Определяем начальную колонку
                            int startColumn = pageIsOdd ? oddPageStartColumn : evenPageStartColumn;

                            // Определяем имя для новой страницы
                            string continuationSheetName = $"Страница {continuationPageIndex + 1}";

                            // Выбираем правильный шаблон
                            var template = pageIsOdd ? oddTemplate : evenTemplate;

                            // Создаем новую страницу
                            if (continuationPageIndex >= package.Workbook.Worksheets.Count)
                            {
                                var continuationSheet = package.Workbook.Worksheets.Add(continuationSheetName, template);

                                // Заголовок
                                continuationSheet.Cells["B2"].Value = $"{subject.Name} ({group.Name}) - продолжение {continuationPageIndex + 1}";
                                continuationSheet.Cells["B2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                continuationSheet.Cells["B2"].Style.Font.Bold = true;

                                // Заполняем заголовки занятий
                                for (int i = 0; i < lessonsThisPage; i++)
                                {
                                    int lessonIndex = currentLessonOffset + i;
                                    if (lessonIndex >= lessons.Count) break;

                                    var lesson = lessons[lessonIndex];
                                    int column = startColumn + i;

                                    var infoCell = continuationSheet.Cells[5, column];
                                    infoCell.Value = FormatLessonInfo(lesson);
                                    infoCell.Style.TextRotation = 90;
                                    infoCell.Style.WrapText = true;
                                    infoCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                    infoCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                    infoCell.Style.Font.Size = 8;

                                    var teacherCell = continuationSheet.Cells[4, column];
                                    if (string.IsNullOrEmpty(teacherCell.Text))
                                    {
                                        teacherCell.Value = "Преподаватель\nподпись";
                                        teacherCell.Style.WrapText = true;
                                        teacherCell.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                                        teacherCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                        teacherCell.Style.Font.Size = 8;
                                    }
                                }

                                // Заполняем студентов
                                for (int i = 0; i < studentsOnThisPage; i++)
                                {
                                    var student = students[studentOffset + i];
                                    int row = studentStartRow + i;

                                    // № п/п (продолжение нумерации) - только на нечетных
                                    if (pageIsOdd)
                                    {
                                        continuationSheet.Cells[row, studentNumberColumn].Value = studentOffset + i + 1;
                                        continuationSheet.Cells[row, studentNumberColumn].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                    }

                                    // ФИО
                                    continuationSheet.Cells[row, studentNameColumn].Value = FormatStudentName(student);

                                    // Оценки
                                    for (int j = 0; j < lessonsThisPage; j++)
                                    {
                                        int lessonIndex = currentLessonOffset + j;
                                        if (lessonIndex >= lessons.Count) break;

                                        var lesson = lessons[lessonIndex];
                                        int column = startColumn + j;

                                        var mark = marks.FirstOrDefault(m =>
                                            m.StudentID == student.StudentID &&
                                            m.LessonID == lesson.LessonID);

                                        var markCell = continuationSheet.Cells[row, column];
                                        markCell.Value = mark?.Value ?? "";
                                        markCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                        markCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                                        if (!string.IsNullOrEmpty(mark?.Value))
                                        {
                                            markCell.Style.Font.Bold = true;
                                        }
                                    }
                                }
                            }

                            currentLessonOffset += lessonsThisPage;
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

            // Форматируем для вертикального отображения (короткий вариант)
            return $"{date}\n{theme}";
        }

        private string FormatStudentName(Student student)
        {
            if (string.IsNullOrEmpty(student.Name))
                return student.LastName ?? "";

            if (string.IsNullOrEmpty(student.Surname))
                return $"{student.LastName} {student.Name}";

            // Для экономии места используем инициалы
            string firstNameInitial = student.Name.Length > 0 ? student.Name[0].ToString() : "";
            string surnameInitial = student.Surname.Length > 0 ? student.Surname[0].ToString() : "";

            return $"{student.LastName} {firstNameInitial}.{surnameInitial}.";
        }

    }
}