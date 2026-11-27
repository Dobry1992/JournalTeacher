using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Portal.Data;
using Portal.Models;
using Portal.Repository;
using Portal.Services;
using Portal.ViewModel;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal
{
    public class GroupsController : Controller
    {
        private readonly AcademyContext _context;
        private readonly GroupRatingService _groupRating;
        private readonly StudentAverageMarkService _studentRating;

        public GroupsController(AcademyContext context, GroupRatingService groupRating, StudentAverageMarkService studentRating)
        {
            _context = context;
            _groupRating = groupRating;
            _studentRating = studentRating;
        }

        // ============================================================
        // ПОДГРУППЫ
        // ============================================================

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpGet]
        public async Task<IActionResult> SubgroupSelectSubject(int id)
        {
            var group = await _context.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GroupID == id);

            if (group == null)
                return NotFound();

            var subjectIds = await _context.Journals
                .Where(j => j.GroupID == id)
                .Select(j => j.SubjectID)
                .Distinct()
                .ToListAsync();

            var subjects = await _context.Subjects
                .AsNoTracking()
                .Where(s => subjectIds.Contains(s.SubjectID) && !s.Arch)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var departments = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();

            ViewBag.Departments = departments;

            var viewModel = new SubgroupSelectSubjectViewModel
            {
                GroupID = group.GroupID,
                GroupName = group.Name,
                Subjects = subjects
            };

            return View(viewModel);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpGet]
        public async Task<IActionResult> SubgroupCreate(int groupId, int subjectId)
        {
            var group = await _context.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GroupID == groupId);

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubjectID == subjectId);

            if (group == null || subject == null)
                return NotFound();

            var availableStudents = await GetAvailableStudentsAsync(groupId, subjectId);

            var viewModel = new SubgroupCreateViewModel
            {
                GroupID = group.GroupID,
                GroupName = group.Name,
                SubjectID = subject.SubjectID,
                SubjectName = subject.Name,
                SubgroupName = string.Empty,
                AvailableStudents = availableStudents
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> SubgroupCreate(SubgroupCreateViewModel model)
        {
            if (model.SelectedStudentIDs == null || !model.SelectedStudentIDs.Any())
                ModelState.AddModelError(nameof(model.SelectedStudentIDs), "Необходимо выбрать хотя бы одного студента");

            if (!ModelState.IsValid)
                return await ReloadSubgroupCreateViewAsync(model);

            var sortedSelectedIds = model.SelectedStudentIDs.OrderBy(id => id).ToList();

            // EF Core 5 не транслирует GroupBy + OrderBy внутри проекции в SQL —
            // поэтому сначала тянем плоские данные, потом группируем в памяти.
            var existingLinks = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == model.GroupID && sl.SubjectID == model.SubjectID)
                .Select(sl => new { sl.Name, sl.StudentID })
                .AsNoTracking()
                .ToListAsync();

            var existingSubgroups = existingLinks
                .GroupBy(sl => sl.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    StudentIds = g.Select(sl => sl.StudentID).OrderBy(id => id).ToList()
                })
                .ToList();

            var duplicateSubgroup = existingSubgroups
                .FirstOrDefault(sg => sg.StudentIds.SequenceEqual(sortedSelectedIds));

            if (duplicateSubgroup != null)
            {
                ModelState.AddModelError("", $"Подгруппа \"{duplicateSubgroup.Name}\" уже содержит выбранных студентов по предмету \"{model.SubjectName}\"");
                return await ReloadSubgroupCreateViewAsync(model);
            }

            var subgroupName = model.SubgroupName?.Trim();
            foreach (var studentId in model.SelectedStudentIDs)
            {
                _context.SubgroupLinks.Add(new SubgroupLink
                {
                    GroupID = model.GroupID,
                    SubjectID = model.SubjectID,
                    StudentID = studentId,
                    Name = subgroupName
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Подгруппа \"{subgroupName}\" успешно создана. Добавлено студентов: {model.SelectedStudentIDs.Count}";

            return RedirectToAction(nameof(Details), new { id = model.GroupID });
        }

        private async Task<List<Student>> GetAvailableStudentsAsync(int groupId, int subjectId)
        {
            var existingStudentIds = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == groupId && sl.SubjectID == subjectId)
                .Select(sl => sl.StudentID)
                .Distinct()
                .ToListAsync();

            return await _context.Students
                .AsNoTracking()
                .Where(s => s.GroupID == groupId
                         && s.Status
                         && !existingStudentIds.Contains(s.StudentID))
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }

        private async Task<IActionResult> ReloadSubgroupCreateViewAsync(SubgroupCreateViewModel model)
        {
            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubjectID == model.SubjectID);

            var group = await _context.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GroupID == model.GroupID);

            model.GroupName = group?.Name;
            model.SubjectName = subject?.Name;
            model.AvailableStudents = await GetAvailableStudentsAsync(model.GroupID, model.SubjectID);

            return View(model);
        }

        // ============================================================
        // ВЫБОР ГРУППЫ / ПРЕДМЕТА ДЛЯ ЗАНЯТИЯ
        // ============================================================

        public async Task<IActionResult> ChooseGroup(int SubjectID)
        {
            await LoadChooseGroupViewDataAsync(SubjectID);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChooseGroup(int[] groupsId, int SubjectID,
            [Bind("LessonID,Date,Comment,FlagF,ThemeID,TypeOfExerciseID,GroupID,SubjectID,Signature")] Lesson lesson)
        {
            if (groupsId == null || groupsId.Length == 0)
                ModelState.AddModelError("", "Не выбрана ни одна группа");

            if (lesson.Date > DateTime.Now)
                ModelState.AddModelError("", "Невозможно создать занятие в будущем!");

            if (!ModelState.IsValid)
            {
                await LoadChooseGroupViewDataAsync(SubjectID);
                return View(lesson);
            }

            string teacher = GetCurrentTeacherName();

            // 1. Собираем все уроки (первый — из bind-модели, остальные — копии)
            lesson.SubjectID = SubjectID;
            lesson.GroupID = groupsId[0];
            lesson.Signature = teacher;

            var lessons = new List<Lesson> { lesson };
            for (int i = 1; i < groupsId.Length; i++)
            {
                lessons.Add(new Lesson
                {
                    Date = lesson.Date,
                    Comment = lesson.Comment,
                    Signature = teacher,
                    SubjectID = SubjectID,
                    ThemeID = lesson.ThemeID,
                    TypeOfExerciseID = lesson.TypeOfExerciseID,
                    GroupID = groupsId[i]
                });
            }

            _context.Lessons.AddRange(lessons);
            await _context.SaveChangesAsync(); // один батч — все LessonID получены

            // 2. Собираем все отметки — тоже одним батчем
            var subject = await _context.Subjects.FindAsync(SubjectID);
            var groups = await _context.Groups
                .AsNoTracking()
                .Where(g => groupsId.Contains(g.GroupID))
                .ToDictionaryAsync(g => g.GroupID);

            var allStudents = await _context.Students
                .AsNoTracking()
                .Where(s => groupsId.Contains(s.GroupID))
                .ToListAsync();

            var studentsByGroup = allStudents
                .GroupBy(s => s.GroupID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var marks = new List<Mark>();
            foreach (var l in lessons)
            {
                if (!groups.TryGetValue(l.GroupID, out var g) ||
                    !studentsByGroup.TryGetValue(l.GroupID, out var studentsInGroup))
                    continue;

                foreach (var student in studentsInGroup)
                {
                    marks.Add(new Mark
                    {
                        Value = "",
                        Date = l.Date,
                        SubjectID = SubjectID,
                        GroupID = l.GroupID,
                        LessonID = l.LessonID,
                        TypeOfExerciseID = l.TypeOfExerciseID,
                        DepartmentID = subject.DepartmentID,
                        InstituteID = g.InstituteID,
                        SpecialityID = g.SpecialityID,
                        ThemeID = l.ThemeID,
                        StudentID = student.StudentID
                    });
                }
            }

            _context.Marks.AddRange(marks);
            await _context.SaveChangesAsync();

            return View("Success");
        }

        private string GetCurrentTeacherName()
        {
            var username = User.Identity?.Name;
            try
            {
                using var context = new PrincipalContext(ContextType.Domain, AD.root);
                var user = UserPrincipal.FindByIdentity(context, username);
                return user?.DisplayName ?? username;
            }
            catch
            {
                return username;
            }
        }

        private async Task LoadChooseGroupViewDataAsync(int subjectId)
        {
            var groupIds = await _context.Journals
                .Where(j => j.SubjectID == subjectId)
                .Select(j => j.GroupID)
                .Distinct()
                .ToListAsync();

            ViewBag.Groups = await _context.Groups
                .AsNoTracking()
                .Where(g => groupIds.Contains(g.GroupID))
                .OrderBy(g => g.Name)
                .ToListAsync();

            ViewBag.Themes = await _context.Themes
                .AsNoTracking()
                .Where(t => t.SubjectID == subjectId && t.Name != "Контрольное занятие")
                .ToListAsync();

            ViewData["TypeOfExerciseID"] = new SelectList(
                await _context.Types
                    .AsNoTracking()
                    .Where(t => t.Name == "Семинарское занятие"
                             || t.Name == "Практическое занятие"
                             || t.Name == "Лабораторное занятие"
                             || t.Name == "Лекция")
                    .ToListAsync(),
                "TypeOfExerciseID",
                "Name");

            ViewBag.SubjectID = subjectId;
        }

        public async Task<IActionResult> ChooseSubject()
        {
            var departments = await _context.Departments
                .Include(d => d.Subjects)
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();

            foreach (var dept in departments)
                dept.Subjects = dept.Subjects.OrderBy(s => s.Name).ToList();

            return View(departments);
        }

        // ============================================================
        // CRUD ГРУПП
        // ============================================================

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups
                .OrderByDescending(g => g.DateExit)
                .ThenBy(g => g.Name)
                .Include(s => s.Students)
                .Include(s => s.Speciality)
                .AsNoTracking()
                .ToListAsync();

            return View(groups);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var group = await _context.Groups
                .Include(s => s.Students)
                .Include(s => s.Speciality)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.GroupID == id);

            if (group == null)
                return NotFound();

            ViewBag.Subgroups = await LoadSubgroupsAsync(group.GroupID);
            return View(group);
        }

        private async Task<List<SubgroupViewModel>> LoadSubgroupsAsync(int groupId)
        {
            var subgroupLinks = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == groupId)
                .AsNoTracking()
                .ToListAsync();

            if (!subgroupLinks.Any())
                return new List<SubgroupViewModel>();

            var subjectIds = subgroupLinks.Select(sl => sl.SubjectID).Distinct().ToList();
            var subjects = await _context.Subjects
                .AsNoTracking()
                .Where(s => subjectIds.Contains(s.SubjectID))
                .ToDictionaryAsync(s => s.SubjectID, s => s.Name);

            var studentIds = subgroupLinks.Select(sl => sl.StudentID).Distinct().ToList();
            var students = await _context.Students
                .AsNoTracking()
                .Where(s => studentIds.Contains(s.StudentID))
                .ToDictionaryAsync(s => s.StudentID);

            return subgroupLinks
                .GroupBy(sl => new { sl.SubjectID, sl.Name })
                .Select(g => new SubgroupViewModel
                {
                    Name = g.Key.Name,
                    SubjectID = g.Key.SubjectID,
                    SubjectName = subjects.TryGetValue(g.Key.SubjectID, out var subj) ? subj : "—",
                    Students = g
                        .Select(sl => students.TryGetValue(sl.StudentID, out var st) ? st : null)
                        .Where(s => s != null)
                        .OrderBy(s => s.LastName)
                        .ThenBy(s => s.Name)
                        .ToList()
                })
                .OrderBy(sg => sg.SubjectName)
                .ThenBy(sg => sg.Name)
                .ToList();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH, ANB-HEAD, ANB-CI, ANB-ICDA, ANB-IST, User")]
        public async Task<IActionResult> DetailsGroup(int? id, string searchString)
        {
            if (id == null)
                return NotFound();

            var group = await _context.Groups
                .Include(s => s.Students)
                .Include(j => j.Journals).ThenInclude(s => s.Subject)
                .Include(s => s.Speciality).ThenInclude(s => s.Institute)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.GroupID == id);

            if (group == null)
                return NotFound();

            var types = await _context.Types
                .AsNoTracking()
                .Where(t => t.Name == "Семинарское занятие"
                         || t.Name == "Практическое занятие"
                         || t.Name == "Лабораторное занятие"
                         || t.Name == "Лекция"
                         || t.Name == "Контрольное мероприятие"
                         || t.Name == "Городское практическое занятие")
                .ToDictionaryAsync(t => t.Name, t => t.TypeOfExerciseID);

            var studyTypeIds = new HashSet<int>(
                types
                    .Where(kv => kv.Key == "Семинарское занятие"
                              || kv.Key == "Практическое занятие"
                              || kv.Key == "Лабораторное занятие"
                              || kv.Key == "Лекция"
                              || kv.Key == "Контрольное мероприятие"
                              || kv.Key == "Городское практическое занятие")
                    .Select(kv => kv.Value)
            );

            // Итоговые отметки (экзамен/зачёт): участвуют в рейтинге по правилу сессии
            var typeIORating = await _context.Types
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == "Итоговая отметка");
            int? typeIOId = typeIORating?.TypeOfExerciseID;

            var institute = await _context.Institutes
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InstituteID == group.InstituteID);

            var allMarks = await _context.Marks
                .AsNoTracking()
                .Where(m => m.GroupID == id)
                .ToListAsync();

            // Учебный год
            int startYear, endYear;
            if (DateTime.Now.Month >= 9)
            {
                startYear = DateTime.Now.Year;
                endYear = DateTime.Now.Year + 1;
            }
            else
            {
                startYear = DateTime.Now.Year - 1;
                endYear = DateTime.Now.Year;
            }

            var (marks, term) = FilterMarksBySemester(allMarks, studyTypeIds, typeIOId);

            // Распределение отметок считается по "живым" отметкам (итоговые - производные)
            var marksAverage = new List<double>();
            foreach (var mark in marks)
                if ((!typeIOId.HasValue || mark.TypeOfExerciseID != typeIOId.Value) &&
                    double.TryParse(mark.Value, out var m))
                    marksAverage.Add(m);

            // Определяем предмет для отображения
            int subjectID = 0;
            if (!string.IsNullOrEmpty(searchString) && int.TryParse(searchString, out var parsedID))
                subjectID = parsedID;
            else if (group.Journals.Count != 0)
                subjectID = group.Journals.First().SubjectID;

            var subject = subjectID > 0
                ? await _context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.SubjectID == subjectID)
                : null;

            var raitingTimeSubject = BuildMonthlyRating(marks.Where(m => m.SubjectID == subjectID &&
                (!typeIOId.HasValue || m.TypeOfExerciseID != typeIOId.Value)));

            var students = group.Students.Where(s => s.Status).ToList();
            var groupRating = BuildGroupRating(students, marks, typeIOId);
            var rating = groupRating.Any(g => g.Raiting > 0)
                ? Math.Round(groupRating.Where(g => g.Raiting > 0).Average(g => g.Raiting), 3, MidpointRounding.AwayFromZero)
                : 0;

            var (marksNumber, marksPercent) = BuildMarkDistribution(marksAverage);

            var yearMarks = allMarks.Where(m => studyTypeIds.Contains(m.TypeOfExerciseID)).ToList();
            var raitingTime = BuildYearlyRating(yearMarks, startYear, endYear);

            var bestStudent = groupRating.OrderByDescending(s => s.Raiting).FirstOrDefault();
            var worstStudent = groupRating.OrderByDescending(s => s.Raiting).LastOrDefault();

            ViewBag.Term = term;
            ViewBag.Raiting = rating;
            ViewBag.StudentsCount = students.Count;
            ViewBag.GroupRaiting = groupRating.OrderByDescending(s => s.Raiting);
            ViewBag.BestStudent = bestStudent ?? new GroupRaiting { Raiting = 0 };
            ViewBag.WorseStudent = worstStudent ?? new GroupRaiting { Raiting = 0 };
            ViewBag.Institute = institute;
            ViewBag.MarksNumber = marksNumber;
            ViewBag.MarksPercent = marksPercent;
            ViewBag.Year = $"{startYear}/{endYear}";
            ViewBag.TimeRaiting = raitingTime;
            ViewBag.SubjectRaiting = raitingTimeSubject;
            ViewBag.Subject = subject;

            return View(group);
        }

        private static (List<Mark> marks, string term) FilterMarksBySemester(List<Mark> allMarks, HashSet<int> studyTypeIds, int? typeIOId = null)
        {
            var date = DateTime.Now.AddYears(-1);

            // В окно семестра дополнительно берём итоговые отметки (сессия): они
            // "флажатся" при создании экзамена и иначе не попали бы в расчёт рейтинга
            bool InSemesterTypes(Mark m) =>
                studyTypeIds.Contains(m.TypeOfExerciseID) ||
                (typeIOId.HasValue && m.TypeOfExerciseID == typeIOId.Value);

            if (DateTime.Now.Month >= 9 && DateTime.Now.Month <= 12)
            {
                var semester = allMarks.Where(m =>
                    InSemesterTypes(m) &&
                    m.Date.Year == DateTime.Now.Year &&
                    m.Date.Month >= 9 && m.Date.Month <= 12).ToList();
                var incomplete = allMarks.Where(m => m.FlagF == 0).ToList();
                return (DedupeById(semester.Concat(incomplete)), "первый семестр");
            }

            if (DateTime.Now.Month == 1)
            {
                var semester = allMarks.Where(m =>
                    InSemesterTypes(m) &&
                    ((m.Date.Year == DateTime.Now.Year && m.Date.Month == 1) ||
                     (m.Date.Year == date.Year && m.Date.Month >= 9 && m.Date.Month <= 12))).ToList();
                var incomplete = allMarks.Where(m => m.FlagF == 0).ToList();
                return (DedupeById(semester.Concat(incomplete)), "первый семестр");
            }

            var semester2 = allMarks.Where(m =>
                InSemesterTypes(m) &&
                m.Date.Year == DateTime.Now.Year &&
                m.Date.Month >= 2 && m.Date.Month <= 8).ToList();
            var incomplete2 = allMarks.Where(m => m.FlagF == 0).ToList();
            return (DedupeById(semester2.Concat(incomplete2)), "второй семестр");
        }

        private static List<Mark> DedupeById(IEnumerable<Mark> marks)
        {
            var seen = new HashSet<int>();
            return marks.Where(m => seen.Add(m.MarkID)).ToList();
        }

        private List<GroupRaiting> BuildGroupRating(List<Student> students, List<Mark> marks, int? typeIOId)
        {
            var result = new List<GroupRaiting>();
            foreach (var student in students)
            {
                var marksStudent = marks.Where(m => m.StudentID == student.StudentID).ToList();
                // Для количества отметок берём "живые" отметки (итоговые - производные),
                // а в сервис передаём все: он применит правило сессии
                var marksValue = marksStudent
                    .Where(m => !typeIOId.HasValue || m.TypeOfExerciseID != typeIOId.Value)
                    .Select(m => double.TryParse(m.Value, out var v) ? v : (double?)null)
                    .Where(v => v.HasValue)
                    .Select(v => v.Value)
                    .ToList();

                double ratingStudent = 0;
                if (marksValue.Any())
                    ratingStudent = _studentRating.GetStudentAverageMark(student, marksStudent, typeIOId) ?? 0;

                result.Add(new GroupRaiting
                {
                    Student = student,
                    Raiting = Math.Round(ratingStudent, 3, MidpointRounding.AwayFromZero),
                    NumberOfMark = marksValue.Count
                });
            }
            return result;
        }

        private static (Dictionary<int, int> number, Dictionary<int, decimal> percent) BuildMarkDistribution(List<double> marksAverage)
        {
            var number = new Dictionary<int, int>();
            var percent = new Dictionary<int, decimal>();

            for (int i = 1; i <= 10; i++)
            {
                int count = marksAverage.Count(x => (int)x == i);
                number[i] = count;

                percent[i] = marksAverage.Count > 0
                    ? Math.Round((decimal)count / marksAverage.Count * 100, 3, MidpointRounding.AwayFromZero)
                    : 0;
            }

            return (number, percent);
        }

        private Dictionary<string, string> BuildMonthlyRating(IEnumerable<Mark> subjectMarks)
        {
            var result = new Dictionary<string, string>();
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                                "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };

            var byMonth = subjectMarks.ToLookup(m => m.Date.Month);
            for (int i = 0; i < months.Length; i++)
            {
                var monthMarks = byMonth[i + 1].ToList();
                double avg = CalculateAverageMark(monthMarks);
                result[months[i]] = avg.ToString("0.000").Replace(",", ".");
            }

            return result;
        }

        private Dictionary<string, string> BuildYearlyRating(List<Mark> yearMarks, int startYear, int endYear)
        {
            var result = new Dictionary<string, string>();

            for (int month = 9; month <= 12; month++)
            {
                string name = GetMonthName(month);
                var monthMarks = yearMarks.Where(m => m.Date.Month == month && m.Date.Year == startYear).ToList();
                result[name] = CalculateAverageMark(monthMarks).ToString("0.000").Replace(",", ".");
            }

            for (int month = 1; month <= 8; month++)
            {
                string name = GetMonthName(month);
                var monthMarks = yearMarks.Where(m => m.Date.Month == month && m.Date.Year == endYear).ToList();
                result[name] = CalculateAverageMark(monthMarks).ToString("0.000").Replace(",", ".");
            }

            return result;
        }

        private static string GetMonthName(int month)
        {
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                                "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            return months[month - 1];
        }

        private static double CalculateAverageMark(List<Mark> marks)
        {
            var numeric = marks
                .Select(m => double.TryParse(m.Value, out var v) ? v : (double?)null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            return numeric.Any()
                ? Math.Round(numeric.Average(), 3, MidpointRounding.AwayFromZero)
                : 0;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create(object selectSpeciality = null)
        {
            FillSpecialitiesViewBag(selectSpeciality);
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GroupID,SpecialityID,Name,DateEnter,DateExit,InstituteID")] Group group)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var speciality = await _context.Specialities.FindAsync(group.SpecialityID);
                    if (speciality == null)
                    {
                        ModelState.AddModelError(nameof(group.SpecialityID), "Специальность не найдена");
                    }
                    else
                    {
                        group.InstituteID = speciality.InstituteID;
                        _context.Groups.Add(group);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (RetryLimitExceededException)
            {
                ModelState.AddModelError("", "Возникла ошибка, обратитесь к администратору.");
            }

            FillSpecialitiesViewBag(group.SpecialityID);
            return View(group);
        }

        private void FillSpecialitiesViewBag(object selected = null)
        {
            var query = _context.Specialities
                .AsNoTracking()
                .OrderBy(sp => sp.Name);

            ViewBag.SpecialityID = new SelectList(query, "SpecialityID", "Name", selected);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound();

            ViewData["SpecialityID"] = new SelectList(
                _context.Specialities.AsNoTracking(),
                "SpecialityID", "Name", group.SpecialityID);

            return View(group);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GroupID,SpecialityID,Name,DateEnter,DateExit,InstituteID")] Group group)
        {
            if (id != group.GroupID)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(group);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await GroupExistsAsync(group.GroupID))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["SpecialityID"] = new SelectList(
                _context.Specialities.AsNoTracking(),
                "SpecialityID", "Name", group.SpecialityID);

            return View(group);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var group = await _context.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.GroupID == id);
            if (group == null)
                return NotFound();

            return View(group);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound();

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> GroupExistsAsync(int id) =>
            _context.Groups.AnyAsync(e => e.GroupID == id);

        // ============================================================
        // ВЕДОМОСТИ
        // ============================================================

        public async Task<IActionResult> GroupStatement(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound("Группа не найдена.");

            var students = await GetGroupStatementViewModelAsync(id);
            if (students == null)
                return NotFound("Студентов в группе нет.");

            var subjectAveragesList = students
                .SelectMany(s => s.SubjectAverages)
                .GroupBy(sa => new { sa.SubjectId, sa.SubjectName, sa.SubjectFullName })
                .Select(g => new SubjectAverageViewModel
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.Key.SubjectName,
                    SubjectFullName = g.Key.SubjectFullName,
                    AvgMark = g.Where(sa => sa.AvgMark.HasValue).Select(sa => sa.AvgMark!.Value).DefaultIfEmpty().Average()
                })
                .Where(sa => sa.AvgMark.HasValue)
                .ToList();

            var total = subjectAveragesList.Select(sa => sa.AvgMark!.Value).DefaultIfEmpty().Average();

            ViewBag.GroupName = group.Name;
            ViewBag.GroupId = group.GroupID;
            ViewBag.SubjectAverages = subjectAveragesList;
            ViewBag.TotalAverage = (double?)total;

            return View(students);
        }

        public async Task<IActionResult> ExportGroupStatement(int id)
        {
            var students = await GetGroupStatementViewModelAsync(id);
            if (students == null || !students.Any())
                return NotFound("Студентов в группе нет.");

            var subjectAverages = students
                .SelectMany(s => s.SubjectAverages)
                .GroupBy(sa => new { sa.SubjectId, sa.SubjectName })
                .Select(g => new SubjectAverageViewModel
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.Key.SubjectName,
                    AvgMark = g.Where(sa => sa.AvgMark.HasValue).Select(sa => sa.AvgMark!.Value).DefaultIfEmpty().Average()
                })
                .ToList();

            var totalAverage = subjectAverages
                .Where(sa => sa.AvgMark.HasValue)
                .Select(sa => sa.AvgMark!.Value)
                .DefaultIfEmpty()
                .Average();

            var content = GenerateGroupStatementExcel(students, subjectAverages, totalAverage);

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Ведомость_{id}.xlsx");
        }

        private async Task<List<StudentViewModel>> GetGroupStatementViewModelAsync(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
                return null;

            var typesToExclude = await _context.Types
                .AsNoTracking()
                .Where(t => t.Name == "Курсовая работа" || t.Name == "Курсовой проект")
                .Select(t => t.TypeOfExerciseID)
                .ToListAsync();

            var studentsData = await _context.Students
                .AsNoTracking()
                .Where(s => s.GroupID == groupId && s.Status == true)
                .OrderBy(s => s.LastName)
                .Select(s => new
                {
                    Student = new { s.StudentID, s.Name, s.Surname, s.LastName },
                    Marks = s.Marks
                        .Where(m => m.FlagF == 0 && !typesToExclude.Contains(m.TypeOfExerciseID))
                        .Select(m => new { m.SubjectID, m.Value })
                        .ToList()
                })
                .ToListAsync();

            if (!studentsData.Any())
                return null;

            var subjectIds = studentsData.SelectMany(s => s.Marks)
                .Select(m => m.SubjectID)
                .Distinct()
                .ToList();

            var allSubjects = await _context.Subjects
                .AsNoTracking()
                .Where(sub => subjectIds.Contains(sub.SubjectID))
                .Select(sub => new { sub.SubjectID, sub.Name, sub.ShortName })
                .ToListAsync();

            return studentsData.Select(s =>
            {
                var subjectAverages = allSubjects.Select(subject =>
                {
                    var marks = s.Marks
                        .Where(m => m.SubjectID == subject.SubjectID)
                        .Select(m => double.TryParse(m.Value, out var v) ? v : (double?)null)
                        .Where(v => v.HasValue)
                        .Select(v => v!.Value)
                        .ToList();

                    return new SubjectAverageViewModel
                    {
                        SubjectId = subject.SubjectID,
                        // В шапке и экспорте - краткое наименование, полное - в тултипе
                        SubjectName = string.IsNullOrWhiteSpace(subject.ShortName) ? subject.Name : subject.ShortName,
                        SubjectFullName = subject.Name,
                        AvgMark = marks.Any() ? marks.Average() : (double?)null
                    };
                }).ToList();

                var validAverages = subjectAverages
                    .Where(sa => sa.AvgMark.HasValue)
                    .Select(sa => sa.AvgMark!.Value)
                    .ToList();

                return new StudentViewModel
                {
                    StudentId = s.Student.StudentID,
                    Name = s.Student.Name,
                    Surname = s.Student.Surname,
                    LastName = s.Student.LastName,
                    SubjectAverages = subjectAverages,
                    StudentAverage = validAverages.Any() ? validAverages.Average() : (double?)null
                };
            }).ToList();
        }

        private byte[] GenerateGroupStatementExcel(List<StudentViewModel> students,
            List<SubjectAverageViewModel> subjectAverages, double? totalAverage)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ведомость");

            int col = 1;
            worksheet.Cell(1, col++).Value = "#";
            worksheet.Cell(1, col++).Value = "Ф.И.О.";

            var subjects = students.First().SubjectAverages;
            foreach (var subject in subjects)
            {
                var cell = worksheet.Cell(1, col++);
                cell.Value = subject.SubjectName;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.TextRotation = 90;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            var averageCell = worksheet.Cell(1, col++);
            averageCell.Value = "Средняя";
            averageCell.Style.Alignment.WrapText = true;
            averageCell.Style.Alignment.TextRotation = 90;
            averageCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            averageCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            int row = 2;
            int studentNumber = 1;
            foreach (var student in students)
            {
                col = 1;
                worksheet.Cell(row, col++).Value = studentNumber++;
                worksheet.Cell(row, col++).Value = $"{student.LastName} {student.Name} {student.Surname}";

                foreach (var subj in student.SubjectAverages)
                {
                    var cell = worksheet.Cell(row, col++);
                    if (subj.AvgMark.HasValue)
                    {
                        cell.Value = subj.AvgMark.Value;
                        cell.Style.NumberFormat.Format = "0.000";
                    }
                    else
                    {
                        cell.Value = "-";
                    }
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                var avgStudentCell = worksheet.Cell(row, col++);
                if (student.StudentAverage.HasValue)
                {
                    avgStudentCell.Value = student.StudentAverage.Value;
                    avgStudentCell.Style.NumberFormat.Format = "0.000";
                }
                else
                {
                    avgStudentCell.Value = "-";
                }
                avgStudentCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                avgStudentCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                row++;
            }

            // Итоговая строка
            col = 1;
            worksheet.Cell(row, col++).Value = "";
            worksheet.Cell(row, col++).Value = "Средняя по дисциплине";
            worksheet.Cell(row, col - 1).Style.Font.Bold = true;

            foreach (var subject in subjects)
            {
                var avg = subjectAverages.FirstOrDefault(sa => sa.SubjectId == subject.SubjectId);
                var cell = worksheet.Cell(row, col++);
                if (avg != null && avg.AvgMark.HasValue)
                {
                    cell.Value = avg.AvgMark.Value;
                    cell.Style.NumberFormat.Format = "0.000";
                }
                else
                {
                    cell.Value = "-";
                }
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Font.Bold = true;
            }

            var totalCell = worksheet.Cell(row, col++);
            if (totalAverage.HasValue)
            {
                totalCell.Value = totalAverage.Value;
                totalCell.Style.NumberFormat.Format = "0.000";
            }
            else
            {
                totalCell.Value = "-";
            }
            totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            totalCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            totalCell.Style.Font.Bold = true;

            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.Gray;
            worksheet.Row(1).Height = 80;
            worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.SheetView.FreezeRows(1);
            worksheet.Columns().AdjustToContents();

            var usedRange = worksheet.RangeUsed();
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ============================================================
        // СВОДНЫЕ ВЕДОМОСТИ
        // ============================================================

        public async Task<IActionResult> GroupSummaryStatement(int id, DateTime date_1, DateTime date_2)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound("Группа не найдена.");

            var students = await LoadGroupSummaryStudentsAsync(id);

            if (students.Count == 0)
            {
                ViewBag.GroupName = group.Name;
                ViewBag.GroupId = group.GroupID;
                return View(students);
            }

            await FillSummaryMarksAsync(students, date_1, date_2);

            ViewBag.GroupName = group.Name;
            ViewBag.GroupId = group.GroupID;

            return View(students);
        }

        public async Task<IActionResult> GetUnsatisfactoryMarks(int id, DateTime date_1, DateTime date_2)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound();

            var query = from m in _context.UnsatisfactoryMarks
                        join st in _context.Students on m.StudentID equals st.StudentID
                        join sb in _context.Subjects on m.SubjectID equals sb.SubjectID
                        join mk in _context.Marks on m.MarkID equals mk.MarkID
                        join toe in _context.Types on mk.TypeOfExerciseID equals toe.TypeOfExerciseID
                        where m.GroupID == id
                        select new UnsatisfactoryViewModel
                        {
                            StudentID = m.StudentID,
                            SubjectID = m.SubjectID,
                            StudentFullName = (st.LastName + " " + st.Name + " " + st.Surname).Trim(),
                            SubjectFullName = sb.Name.Trim(),
                            UnsatisfactoryMark = m.UnsatisfactoryValue,
                            UsatisfactoryDate = m.UnsatisfactoryDate,
                            CorrectedValue = m.CorrectedValue,
                            CorrectedDate = m.CorrectedDate,
                            GroupID = m.GroupID,
                            TypeName = toe.Name,
                            Comment = mk.Comment
                        };

            bool hasStart = date_1 != default;
            bool hasEnd = date_2 != default;

            if (hasStart && hasEnd)
            {
                var startDate = date_1.Date;
                var endDate = date_2.Date;
                if (startDate > endDate)
                    (startDate, endDate) = (endDate, startDate);

                query = query.Where(x => x.UsatisfactoryDate.Date >= startDate && x.UsatisfactoryDate.Date <= endDate);
            }
            else if (hasStart)
            {
                var startDate = date_1.Date;
                query = query.Where(x => x.UsatisfactoryDate.Date >= startDate);
            }
            else if (hasEnd)
            {
                var endDate = date_2.Date;
                query = query.Where(x => x.UsatisfactoryDate.Date <= endDate);
            }

            var unsatisfactoryMarks = await query.AsNoTracking().ToListAsync();

            ViewBag.GroupName = group.Name;
            ViewBag.id = id;

            return View(unsatisfactoryMarks);
        }

        public async Task<IActionResult> ExportGroupSummaryStatement(int id, DateTime date_1, DateTime date_2)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound("Группа не найдена.");

            var students = await LoadGroupSummaryStudentsAsync(id);
            if (!students.Any())
                return NotFound("Студентов в группе нет.");

            await FillSummaryMarksAsync(students, date_1, date_2);

            var content = GenerateSummaryExcelFile(students, group, date_1, date_2);

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Сводная_ведомость_{group.Name}_{DateTime.Now:dd.MM.yyyy}.xlsx");
        }

        private async Task<List<GroupSummaryStatementViewModel>> LoadGroupSummaryStudentsAsync(int groupId)
        {
            return await _context.Students
                .AsNoTracking()
                .Where(s => s.GroupID == groupId && s.Status == true)
                .OrderBy(s => s.LastName)
                .Select(s => new GroupSummaryStatementViewModel
                {
                    StudentID = s.StudentID,
                    GroupID = s.GroupID,
                    StudentName = s.Name,
                    StudentLastName = s.LastName,
                    StudentSurname = s.Surname,
                    Marks = new List<MarkGroupSummaryStatement>()
                })
                .ToListAsync();
        }

        private async Task FillSummaryMarksAsync(List<GroupSummaryStatementViewModel> students, DateTime date_1, DateTime date_2)
        {
            var types = await _context.Types
                .AsNoTracking()
                .Where(t => t.Name == "Итоговая отметка"
                         || t.Name == "Курсовая работа"
                         || t.Name == "Курсовой проект")
                .Select(t => t.TypeOfExerciseID)
                .ToListAsync();

            var validTypeIds = new HashSet<int>(types);
            var studentIds = students.Select(st => st.StudentID).ToList();

            var allMarks = await ((
                from m in _context.Marks
                where studentIds.Contains(m.StudentID) && validTypeIds.Contains(m.TypeOfExerciseID)
                join sub in _context.Subjects on m.SubjectID equals sub.SubjectID into subg
                from sub in subg.DefaultIfEmpty()
                join tp in _context.Types on m.TypeOfExerciseID equals tp.TypeOfExerciseID into tpg
                from tp in tpg.DefaultIfEmpty()
                select new SummaryMarkRow
                {
                    StudentID = m.StudentID,
                    Date = m.Date,
                    Value = m.Value,
                    SubjectID = (int?)m.SubjectID,
                    SubjectName = sub != null ? sub.Name : null,
                    ShortSubjectName = sub != null ? sub.ShortName : null,
                    TypeID = m.TypeOfExerciseID,
                    TypeName = tp != null ? tp.Name : null,
                    ShortTypeName = tp != null ? tp.ShortName : null
                })
            .Concat(
                from sm in _context.StatementMarks
                where studentIds.Contains(sm.StudentID)
                join tp in _context.Types on sm.TypeOfExerciseID equals tp.TypeOfExerciseID into tpg2
                from tp in tpg2.DefaultIfEmpty()
                select new SummaryMarkRow
                {
                    StudentID = sm.StudentID,
                    Date = sm.Date,
                    Value = sm.Value,
                    SubjectID = null,
                    SubjectName = null,
                    ShortSubjectName = null,
                    TypeID = sm.TypeOfExerciseID,
                    TypeName = tp != null ? tp.Name : null,
                    ShortTypeName = tp != null ? tp.ShortName : null
                }))
                .AsNoTracking()
                .ToListAsync();

            if (date_1 != default)
                allMarks = allMarks.Where(m => m.Date >= date_1).ToList();

            if (date_2 != default)
                allMarks = allMarks.Where(m => m.Date <= date_2).ToList();

            var marksByStudent = allMarks
                .GroupBy(x => x.StudentID)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.SubjectID).ThenBy(x => x.Date)
                          .Select(x => new MarkGroupSummaryStatement
                          {
                              Value = x.Value,
                              Date = x.Date,
                              SubjectID = x.SubjectID,
                              SubjectName = x.SubjectName,
                              ShortSubjectName = x.ShortSubjectName,
                              TypeID = x.TypeID,
                              TypeName = x.TypeName,
                              ShortTypeName = x.ShortTypeName
                          })
                          .ToList());

            foreach (var s in students)
                if (marksByStudent.TryGetValue(s.StudentID, out var list))
                    s.Marks = list;
        }

        /// <summary>Промежуточный тип для Concat двух источников.</summary>
        private class SummaryMarkRow
        {
            public int StudentID { get; set; }
            public DateTime Date { get; set; }
            public string Value { get; set; }
            public int? SubjectID { get; set; }
            public string SubjectName { get; set; }
            public string ShortSubjectName { get; set; }
            public int TypeID { get; set; }
            public string TypeName { get; set; }
            public string ShortTypeName { get; set; }
        }

        private byte[] GenerateSummaryExcelFile(List<GroupSummaryStatementViewModel> students,
            Group group, DateTime date_1, DateTime date_2)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Сводная ведомость");

            var allColumns = students
                .SelectMany(s => s.Marks ?? Enumerable.Empty<MarkGroupSummaryStatement>())
                .Select(m => new
                {
                    SubjectID = m.SubjectID ?? -1,
                    SubjectName = m.SubjectName ?? "",
                    ShortSubjectName = m.ShortSubjectName,
                    TypeID = m.TypeID,
                    TypeName = m.TypeName ?? "",
                    ShortTypeName = m.ShortTypeName,
                    Date = m.Date.Date
                })
                .Distinct()
                .OrderBy(c => c.SubjectID)
                .ThenBy(c => c.Date)
                .ToList();

            int row = 1;
            int col = 1;

            worksheet.Cell(row, col).Value = $"Сводная ведомость группы {group.Name}";
            worksheet.Cell(row, col).Style.Font.Bold = true;
            worksheet.Cell(row, col).Style.Font.FontSize = 14;
            worksheet.Range(row, col, row, col + allColumns.Count + 1).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            if (date_1 != default || date_2 != default)
            {
                string dateInfo;
                if (date_1 != default && date_2 != default)
                    dateInfo = $"Период: {date_1:dd.MM.yyyy} - {date_2:dd.MM.yyyy}";
                else if (date_1 != default)
                    dateInfo = $"С {date_1:dd.MM.yyyy}";
                else
                    dateInfo = $"По {date_2:dd.MM.yyyy}";

                worksheet.Cell(row, col).Value = dateInfo;
                worksheet.Cell(row, col).Style.Font.Italic = true;
                worksheet.Range(row, col, row, col + allColumns.Count + 1).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row++;
            }

            row++;

            col = 1;
            worksheet.Cell(row, col++).Value = "#";
            worksheet.Cell(row, col++).Value = "Ф.И.О.";

            foreach (var colInfo in allColumns)
            {
                var cell = worksheet.Cell(row, col++);
                cell.Value = $"{colInfo.Date:dd.MM.yyyy}\n{colInfo.ShortSubjectName ?? ""}\n{colInfo.ShortTypeName ?? ""}";
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.TextRotation = 90;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            worksheet.Row(row).Style.Font.Bold = true;
            worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.Gray;
            worksheet.Row(row).Height = 80;
            row++;

            int studentNumber = 1;
            foreach (var student in students)
            {
                col = 1;
                worksheet.Cell(row, col++).Value = studentNumber++;
                worksheet.Cell(row, col++).Value = $"{student.StudentLastName} {student.StudentName} {student.StudentSurname}";

                foreach (var colInfo in allColumns)
                {
                    var mark = student.Marks.FirstOrDefault(m =>
                        m.TypeID == colInfo.TypeID &&
                        m.Date.Date == colInfo.Date);
                    worksheet.Cell(row, col++).Value = mark?.Value ?? "-";
                }
                row++;
            }

            worksheet.Columns().AdjustToContents();

            var usedRange = worksheet.RangeUsed();
            if (usedRange != null)
            {
                usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ============================================================
        // РЕДАКТИРОВАНИЕ ПОДГРУППЫ
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> EditSubgroup(int groupId, int subjectId, string name)
        {
            var currentIds = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == groupId && sl.SubjectID == subjectId && sl.Name == name)
                .Select(sl => sl.StudentID)
                .ToListAsync();

            if (!currentIds.Any())
                return NotFound("Подгруппа не найдена");

            var model = new SubgroupEditViewModel
            {
                GroupID = groupId,
                SubjectID = subjectId,
                CurrentName = name,
                NewName = name,
                SelectedStudentIDs = currentIds
            };

            return await ReloadEditSubgroupViewAsync(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> EditSubgroup(SubgroupEditViewModel model)
        {
            var newName = model.NewName?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
                ModelState.AddModelError(nameof(model.NewName), "Название подгруппы обязательно");

            if (model.SelectedStudentIDs == null || !model.SelectedStudentIDs.Any())
                ModelState.AddModelError(nameof(model.SelectedStudentIDs), "Необходимо выбрать хотя бы одного студента");

            if (!ModelState.IsValid)
                return await ReloadEditSubgroupViewAsync(model);

            var currentLinks = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == model.GroupID
                          && sl.SubjectID == model.SubjectID
                          && sl.Name == model.CurrentName)
                .ToListAsync();

            if (!currentLinks.Any())
                return NotFound("Подгруппа не найдена");

            // Конфликт имени с другой подгруппой того же предмета
            if (newName != model.CurrentName)
            {
                var nameExists = await _context.SubgroupLinks
                    .AnyAsync(sl => sl.GroupID == model.GroupID
                                 && sl.SubjectID == model.SubjectID
                                 && sl.Name == newName);

                if (nameExists)
                {
                    ModelState.AddModelError(nameof(model.NewName),
                        $"Подгруппа с названием \"{newName}\" уже существует по этому предмету");
                    return await ReloadEditSubgroupViewAsync(model);
                }
            }

            var currentIds = currentLinks.Select(l => l.StudentID).ToList();
            var selectedIds = model.SelectedStudentIDs.Distinct().ToList();

            // Студент не может быть в двух подгруппах одного предмета
            var otherSubgroups = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == model.GroupID
                          && sl.SubjectID == model.SubjectID
                          && sl.Name != model.CurrentName)
                .Select(sl => new { sl.StudentID, sl.Name })
                .ToListAsync();

            var conflicting = selectedIds
                .Where(id => otherSubgroups.Any(o => o.StudentID == id))
                .ToList();

            if (conflicting.Any())
            {
                var names = string.Join(", ", otherSubgroups
                    .Where(o => conflicting.Contains(o.StudentID))
                    .Select(o => o.Name)
                    .Distinct());
                ModelState.AddModelError("", $"Студенты уже входят в другие подгруппы: {names}");
                return await ReloadEditSubgroupViewAsync(model);
            }

            // Убираем отмеченных, добавляем новых, переименовываем оставшихся
            _context.SubgroupLinks.RemoveRange(
                currentLinks.Where(l => !selectedIds.Contains(l.StudentID)));

            foreach (var id in selectedIds.Except(currentIds))
            {
                _context.SubgroupLinks.Add(new SubgroupLink
                {
                    GroupID = model.GroupID,
                    SubjectID = model.SubjectID,
                    StudentID = id,
                    Name = newName
                });
            }

            foreach (var link in currentLinks.Where(l => selectedIds.Contains(l.StudentID)))
                link.Name = newName;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Подгруппа \"{newName}\" успешно обновлена";
            return RedirectToAction(nameof(Details), new { id = model.GroupID });
        }

        private async Task<IActionResult> ReloadEditSubgroupViewAsync(SubgroupEditViewModel model)
        {
            var group = await _context.Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.GroupID == model.GroupID);
            var subject = await _context.Subjects.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubjectID == model.SubjectID);

            if (group == null || subject == null)
                return NotFound();

            model.GroupName = group.Name;
            model.SubjectName = subject.Name;

            var currentIds = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == model.GroupID
                          && sl.SubjectID == model.SubjectID
                          && sl.Name == model.CurrentName)
                .Select(sl => sl.StudentID)
                .ToListAsync();

            model.CurrentStudents = await _context.Students.AsNoTracking()
                .Where(s => currentIds.Contains(s.StudentID))
                .OrderBy(s => s.LastName).ThenBy(s => s.Name)
                .ToListAsync();

            var otherIds = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == model.GroupID
                          && sl.SubjectID == model.SubjectID
                          && sl.Name != model.CurrentName)
                .Select(sl => sl.StudentID)
                .ToListAsync();

            model.AvailableStudents = await _context.Students.AsNoTracking()
                .Where(s => s.GroupID == model.GroupID
                         && s.Status
                         && !currentIds.Contains(s.StudentID)
                         && !otherIds.Contains(s.StudentID))
                .OrderBy(s => s.LastName).ThenBy(s => s.Name)
                .ToListAsync();

            return View(model);
        }

        // ============================================================
        // УДАЛЕНИЕ ПОДГРУППЫ
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> DeleteSubgroup(int groupId, int subjectId, string name)
        {
            var group = await _context.Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.GroupID == groupId);
            var subject = await _context.Subjects.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubjectID == subjectId);

            if (group == null || subject == null)
                return NotFound();

            var studentIds = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == groupId && sl.SubjectID == subjectId && sl.Name == name)
                .Select(sl => sl.StudentID)
                .ToListAsync();

            if (!studentIds.Any())
                return NotFound("Подгруппа не найдена");

            var students = await _context.Students.AsNoTracking()
                .Where(s => studentIds.Contains(s.StudentID))
                .OrderBy(s => s.LastName).ThenBy(s => s.Name)
                .ToListAsync();

            var model = new SubgroupDeleteViewModel
            {
                GroupID = groupId,
                GroupName = group.Name,
                SubjectID = subjectId,
                SubjectName = subject.Name,
                SubgroupName = name,
                Students = students
            };

            return View(model);
        }

        [HttpPost, ActionName("DeleteSubgroup")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> DeleteSubgroupConfirmed(int groupId, int subjectId, string name)
        {
            var links = await _context.SubgroupLinks
                .Where(sl => sl.GroupID == groupId && sl.SubjectID == subjectId && sl.Name == name)
                .ToListAsync();

            if (!links.Any())
                return NotFound("Подгруппа не найдена");

            _context.SubgroupLinks.RemoveRange(links);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Подгруппа \"{name}\" удалена";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }
    }
}