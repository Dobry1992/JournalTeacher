using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Services;
using Portal.ViewModel;
using Portal.ViewModel.Raiting;
using Portal.ViewModel.Statement;

namespace Portal
{
    public class InstitutesController : Controller
    {
        private readonly AcademyContext _context;
        private readonly StudentAverageMarkService _studentAverageMarkService;
        private readonly InstituteAverageMarkService _instituteAverage;

        public InstitutesController(AcademyContext context, StudentAverageMarkService studentAverageMarkService, InstituteAverageMarkService instituteAverage)
        {
            _context = context;
            _studentAverageMarkService = studentAverageMarkService;
            _instituteAverage = instituteAverage;
        }

        public async Task<IActionResult> Statement(int id)
        {
            var institute = await _context.Institutes.FindAsync(id);
            if (institute == null)
                return NotFound("Институт не найден.");

            var groups = await _context.Groups
                .Where(g => g.InstituteID == id)
                .OrderBy(g => g.Name)
                .ToListAsync();

            if (!groups.Any())
                return View(new List<ViewModel.Statement.GroupRaiting>());

            var typeIO = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Итоговая отметка");
            if (typeIO == null)
                return NotFound("Тип 'Итоговая отметка' не найден.");

            var groupIds = groups.Select(g => g.GroupID).ToList();

            var students = await _context.Students
                .Where(s => groupIds.Contains(s.GroupID))
                .ToListAsync();

            var journals = await _context.Journals
                .Where(j => groupIds.Contains(j.GroupID))
                .ToListAsync();

            var subjectIds = journals.Select(j => j.SubjectID).Distinct().ToList();

            var subjects = await _context.Subjects
                .Where(s => subjectIds.Contains(s.SubjectID))
                .ToDictionaryAsync(s => s.SubjectID);

            var studentIds = students.Select(s => s.StudentID).ToList();

            // Загрузка всех оценок как Mark
            var marks = await _context.Marks
                .Where(m =>
                    studentIds.Contains(m.StudentID) &&
                    subjectIds.Contains(m.SubjectID) &&
                    (m.FlagF == 0 || m.TypeOfExerciseID == typeIO.TypeOfExerciseID))
                .ToListAsync();

            // Группировки
            var studentsByGroup = students.GroupBy(s => s.GroupID).ToDictionary(g => g.Key, g => g.ToList());
            var journalsByGroup = journals.GroupBy(j => j.GroupID).ToDictionary(g => g.Key, g => g.ToList());
            var markLookup = marks
                .GroupBy(m => (m.StudentID, m.SubjectID))
                .ToDictionary(g => g.Key, g => g.ToList());

            List<ViewModel.Statement.GroupRaiting> groupRaitings = new();

            foreach (var group in groups)
            {
                var groupStudents = studentsByGroup.GetValueOrDefault(group.GroupID) ?? new List<Student>();
                var groupJournals = journalsByGroup.GetValueOrDefault(group.GroupID) ?? new List<Journal>();

                var groupSubjectIds = groupJournals
                    .Select(j => j.SubjectID)
                    .Distinct()
                    .Where(subjects.ContainsKey)
                    .ToList();

                var groupSubjects = groupSubjectIds
                    .Select(id => subjects[id])
                    .OrderBy(s => s.Name)
                    .ToList();

                List<StudRaiting> studRaitings = new();

                foreach (var student in groupStudents)
                {
                    List<SubRaiting> subRaitings = new();

                    foreach (var subject in groupSubjects)
                    {
                        var key = (student.StudentID, subject.SubjectID);
                        markLookup.TryGetValue(key, out var studentMarks);
                        studentMarks ??= new List<Mark>();

                        var finalMarks = studentMarks
                            .Where(m => m.TypeOfExerciseID == typeIO.TypeOfExerciseID)
                            .OrderByDescending(m => m.Date)
                            .ToList();

                        var regularMarks = studentMarks
                            .Where(m => m.FlagF == 0 && m.TypeOfExerciseID != typeIO.TypeOfExerciseID)
                            .ToList();

                        string rating;
                        string color;

                        if (finalMarks.Any() && !regularMarks.Any())
                        {
                            rating = finalMarks.First().Value;
                            color = "#ebc509";
                        }
                        else
                        {
                            var numericMarks = regularMarks
                                .Select(m => int.TryParse(m.Value, out int val) ? (int?)val : null)
                                .Where(v => v.HasValue)
                                .Select(v => v.Value)
                                .ToList();

                            rating = numericMarks.Any()
                                ? Math.Round(numericMarks.Average(), 3, MidpointRounding.AwayFromZero).ToString()
                                : "-";

                            color = "#FFFFFF";
                        }

                        subRaitings.Add(new SubRaiting
                        {
                            Subject = subject,
                            Raiting = rating,
                            Color = color,
                            FinalMarks = finalMarks
                        });
                    }

                    studRaitings.Add(new StudRaiting
                    {
                        Student = student,
                        SubRaitings = subRaitings
                    });
                }

                groupRaitings.Add(new ViewModel.Statement.GroupRaiting
                {
                    Group = group,
                    Subjects = groupSubjects,
                    Raitings = studRaitings
                });
            }

            ViewBag.Institute = institute;
            return View(groupRaitings);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Institutes
                .OrderBy(i => i.Arch)
                    .ThenBy(i => i.Name)
                .ToListAsync());
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH, ANB-HEAD, ANB-CI, ANB-ICDA, ANB-IST, ANB-SFIPKIP, User")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institute = await _context.Institutes.FindAsync(id);
            if (institute == null)
            {
                return NotFound();
            }

            // Получаем типы занятий
            var typeSZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Семинарское занятие");
            var typePZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Практическое занятие");
            var typeLZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лабораторное занятие");
            var typeL = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лекция");
            var typeKM = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Контрольное мероприятие");
            var typeGPZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Городское практическое занятие");

            // ========================================
            // 1. ОТМЕТКИ ДЛЯ РЕЙТИНГА (с учетом FlagF == 0 из прошлых семестров)
            // ========================================
            List<double> marksAverage = new();
            List<Mark> marksForRating = new();
            string term;

            // Определяем учебный год
            int startYear;
            int endYear;
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

            // Получаем ВСЕ отметки для института
            var allMarks = await _context.Marks
                .Where(m => m.InstituteID == id)
                .ToListAsync();

            // Фильтруем отметки для рейтинга:
            // 1. За текущий учебный год
            // 2. ИЛИ с FlagF == 0 (обычные отметки из прошлых семестров)
            var marksForRatingFiltered = allMarks
                .Where(m => (m.Date.Year >= startYear && m.Date.Year <= endYear) || m.FlagF == 0)
                .ToList();

            // Определяем семестр и фильтруем отметки для рейтинга
            if (DateTime.Now.Month >= 9 && DateTime.Now.Month <= 12)
            {
                term = "первый семестр";

                var semesterMarks = marksForRatingFiltered
                    .Where(m =>
                        (m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeL.TypeOfExerciseID) &&
                        ((m.Date.Month >= 9 && m.Date.Month <= 12 && m.Date.Year == startYear) ||
                         m.FlagF == 0)
                    )
                    .ToList();

                var incompleteMarks = marksForRatingFiltered
                    .Where(m =>
                        m.FlagF == 0 &&
                        ((m.Date.Month >= 9 && m.Date.Month <= 12 && m.Date.Year == startYear) ||
                         m.FlagF == 0)
                    )
                    .ToList();

                var allIds = new HashSet<int>();
                marksForRating = semesterMarks
                    .Concat(incompleteMarks)
                    .Where(m => allIds.Add(m.MarkID))
                    .ToList();
            }
            else if (DateTime.Now.Month == 1)
            {
                term = "первый семестр";

                var semesterMarks = marksForRatingFiltered
                    .Where(m =>
                        (m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeL.TypeOfExerciseID) &&
                        (((m.Date.Month == 1 && m.Date.Year == endYear) ||
                          (m.Date.Month >= 9 && m.Date.Month <= 12 && m.Date.Year == startYear)) ||
                         m.FlagF == 0)
                    )
                    .ToList();

                var incompleteMarks = marksForRatingFiltered
                    .Where(m =>
                        m.FlagF == 0 &&
                        (((m.Date.Month == 1 && m.Date.Year == endYear) ||
                          (m.Date.Month >= 9 && m.Date.Month <= 12 && m.Date.Year == startYear)) ||
                         m.FlagF == 0)
                    )
                    .ToList();

                var allIds = new HashSet<int>();
                marksForRating = semesterMarks
                    .Concat(incompleteMarks)
                    .Where(m => allIds.Add(m.MarkID))
                    .ToList();
            }
            else if (DateTime.Now.Month >= 2 && DateTime.Now.Month <= 8)
            {
                term = "второй семестр";

                var semesterMarks = marksForRatingFiltered
                    .Where(m =>
                        (m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                         m.TypeOfExerciseID == typeL.TypeOfExerciseID) &&
                        ((m.Date.Month >= 2 && m.Date.Month <= 8 && m.Date.Year == endYear) ||
                         m.FlagF == 0)
                    )
                    .ToList();

                var incompleteMarks = marksForRatingFiltered
                    .Where(m =>
                        m.FlagF == 0 &&
                        ((m.Date.Month >= 2 && m.Date.Month <= 8 && m.Date.Year == endYear) ||
                         m.FlagF == 0)
                    )
                    .ToList();

                var allIds = new HashSet<int>();
                marksForRating = semesterMarks
                    .Concat(incompleteMarks)
                    .Where(m => allIds.Add(m.MarkID))
                    .ToList();
            }
            else
            {
                term = "не определен";
                marksForRating = new List<Mark>();
            }

            // Извлекаем числовые отметки для расчетов рейтинга
            foreach (var mark in marksForRating)
            {
                if (double.TryParse(mark.Value, out var m))
                {
                    marksAverage.Add(m);
                }
            }

            // ========================================
            // 2. ОТМЕТКИ ДЛЯ ГРАФИКА (только за календарный период)
            // ========================================
            // Определяем начальную дату для графика (начало учебного года)
            DateTime startDate = new DateTime(startYear, 9, 1);
            DateTime endDate = new DateTime(endYear, 8, 31);

            // Отметки для графика - только за текущий календарный период (без учета FlagF)
            var marksForChart = allMarks
                .Where(m =>
                    m.Date >= startDate &&
                    m.Date <= endDate &&
                    (m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                     m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                     m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                     m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                     m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                     m.TypeOfExerciseID == typeL.TypeOfExerciseID)
                )
                .ToList();

            // ========================================
            // СТУДЕНТЫ
            // ========================================
            var students = await _context.Students
                .Include(s => s.Group)
                .Where(s => s.InstituteID == id && s.Status == true)
                .AsNoTracking()
                .ToListAsync();
            int studentNumber = students.Count;

            // ========================================
            // РЕЙТИНГ СТУДЕНТОВ (используем marksForRating)
            // ========================================
            List<StudentRaiting> studentRaitings = new();
            foreach (var student in students)
            {
                double? studentRating = _studentAverageMarkService.GetStudentAverageMark(student, marksForRating);

                StudentRaiting sr = new StudentRaiting()
                {
                    Group = student.Group,
                    Student = student,
                    Raiting = studentRating ?? 0,
                };

                studentRaitings.Add(sr);
            }

            // ========================================
            // ГРУППЫ
            // ========================================
            var groups = await _context.Groups
                .Where(g => g.InstituteID == id && g.DateExit > DateTime.Now)
                .ToListAsync();

            // ========================================
            // СПЕЦИАЛЬНОСТИ
            // ========================================
            var specialitiesCount = await _context.Specialities
                .CountAsync(s => s.InstituteID == id && s.Arch == false);

            // ========================================
            // РЕЙТИНГ ГРУПП (используем marksForRating)
            // ========================================
            List<InstGroupRaiting> groupsRating = new();
            foreach (var group in groups)
            {
                var groupStudents = students
                    .Where(s => s.GroupID == group.GroupID && s.Status == true)
                    .ToList();

                var groupMarks = marksForRating
                    .Where(m => m.GroupID == group.GroupID)
                    .ToList();

                List<double> groupStudentRatings = new();
                foreach (var student in groupStudents)
                {
                    double? studentRating = _studentAverageMarkService.GetStudentAverageMark(student, groupMarks);
                    if (studentRating != null && studentRating > 0)
                    {
                        groupStudentRatings.Add((double)studentRating);
                    }
                }

                double groupRating = 0;
                if (groupStudentRatings.Any())
                {
                    groupRating = Math.Round(groupStudentRatings.Average(), 3, MidpointRounding.AwayFromZero);
                }

                InstGroupRaiting instGroupRaiting = new()
                {
                    Group = group,
                    Raiting = groupRating
                };

                groupsRating.Add(instGroupRaiting);
            }

            // ========================================
            // СРЕДНИЙ БАЛЛ ИНСТИТУТА
            // ========================================
            double instRating = 0;
            var filterGroupsRatig = groupsRating.Where(m => m.Raiting != 0).ToList();
            if (filterGroupsRatig.Any())
            {
                instRating = Math.Round(filterGroupsRatig.Average(g => g.Raiting), 3, MidpointRounding.AwayFromZero);
            }

            // ========================================
            // РАСПРЕДЕЛЕНИЕ ОТМЕТОК (1-10) - используем marksForRating
            // ========================================
            Dictionary<int, int> marksNumber = new();
            Dictionary<int, decimal> marksPercent = new();
            for (int i = 1; i <= 10; i++)
            {
                int count = marksAverage.Where(x => (int)x == i).Count();
                marksNumber.Add(i, count);

                if (marksAverage.Count > 0)
                {
                    decimal percent = (decimal)count / marksAverage.Count * 100;
                    marksPercent.Add(i, Math.Round(percent, 3, MidpointRounding.AwayFromZero));
                }
                else
                {
                    marksPercent.Add(i, 0);
                }
            }

            // ========================================
            // МЕСЯЧНАЯ УСПЕВАЕМОСТЬ (используем marksForChart - только за календарный период)
            // ========================================
            Dictionary<string, string> raitingTime = new();

            // Заполняем данные по месяцам за учебный год
            var months = new Dictionary<int, string>
    {
        { 9, "Сентябрь" },
        { 10, "Октябрь" },
        { 11, "Ноябрь" },
        { 12, "Декабрь" },
        { 1, "Январь" },
        { 2, "Февраль" },
        { 3, "Март" },
        { 4, "Апрель" },
        { 5, "Май" },
        { 6, "Июнь" },
        { 7, "Июль" },
        { 8, "Август" }
    };

            foreach (var month in months)
            {
                // Берем отметки только за конкретный месяц (без учета FlagF)
                var monthMarks = marksForChart
                    .Where(m => m.Date.Month == month.Key && m.Date.Year == startYear)
                    .ToList();

                // Если месяц Январь, то год может быть endYear
                if (month.Key == 1)
                {
                    monthMarks = marksForChart
                        .Where(m => m.Date.Month == 1 && m.Date.Year == endYear)
                        .ToList();
                }

                if (monthMarks.Any())
                {
                    double avg = await GetAverageMarkAsync(monthMarks);
                    raitingTime.Add(month.Value, avg.ToString("0.000").Replace(",", "."));
                }
                else
                {
                    raitingTime.Add(month.Value, "0");
                }
            }

            // ========================================
            // УЧЕБНЫЙ ГОД
            // ========================================
            string yearsStudy = $"{startYear}/{endYear}";

            // ========================================
            // ЛУЧШИЙ И ХУДШИЙ СТУДЕНТ
            // ========================================
            var bestStudent = studentRaitings.OrderByDescending(s => s.Raiting).FirstOrDefault();
            if (bestStudent == null)
            {
                ViewBag.BestStudent = new StudentRaiting { Raiting = 0 };
            }
            else
            {
                ViewBag.BestStudent = bestStudent;
            }

            var worstStudent = studentRaitings.OrderByDescending(s => s.Raiting).LastOrDefault();
            if (worstStudent == null)
            {
                ViewBag.WorseStudent = new StudentRaiting { Raiting = 0 };
            }
            else
            {
                ViewBag.WorseStudent = worstStudent;
            }

            // ========================================
            // ЛУЧШАЯ И ХУДШАЯ ГРУППА
            // ========================================
            var bestGroup = groupsRating.OrderByDescending(g => g.Raiting).FirstOrDefault();
            if (bestGroup == null)
            {
                ViewBag.BestGroup = new InstGroupRaiting { Raiting = 0 };
            }
            else
            {
                ViewBag.BestGroup = bestGroup;
            }

            var worstGroup = groupsRating.OrderByDescending(g => g.Raiting).LastOrDefault();
            if (worstGroup == null)
            {
                ViewBag.WorseGroup = new InstGroupRaiting { Raiting = 0 };
            }
            else
            {
                ViewBag.WorseGroup = worstGroup;
            }

            // ========================================
            // VIEWBAG
            // ========================================
            ViewBag.Term = term;
            ViewBag.Raiting = Math.Round(instRating, 3, MidpointRounding.AwayFromZero);
            ViewBag.Students = studentNumber;
            ViewBag.Groups = groups.Count();
            ViewBag.Specialities = specialitiesCount;
            ViewBag.StudentsRaiting = studentRaitings.OrderByDescending(s => s.Raiting);
            ViewBag.GroupsRaiting = groupsRating.OrderByDescending(g => g.Raiting);
            ViewBag.MarksNumber = marksNumber;
            ViewBag.MarksPercent = marksPercent;
            ViewBag.TimeRaiting = raitingTime;
            ViewBag.Year = yearsStudy;

            return View(institute);
        }

        // Вспомогательный метод для подсчета среднего балла за месяц
        private async Task<double> GetAverageMarkAsync(List<Mark> marks)
        {
            var numericMarks = marks
                .Select(m => double.TryParse(m.Value, out var value) ? value : (double?)null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            if (numericMarks.Any())
            {
                return Math.Round(numericMarks.Average(), 3, MidpointRounding.AwayFromZero);
            }

            return 0;
        }


        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InstituteID,Arch,Name,Role")] Institute institute)
        {
            if (ModelState.IsValid)
            {
                _context.Add(institute);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institute = await _context.Institutes.FindAsync(id);
            if (institute == null)
            {
                return NotFound();
            }
            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InstituteID,Arch,Name,Role")] Institute institute)
        {
            if (id != institute.InstituteID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(institute);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InstituteExists(institute.InstituteID))
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
            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institute = await _context.Institutes
                .FirstOrDefaultAsync(m => m.InstituteID == id);
            if (institute == null)
            {
                return NotFound();
            }

            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var institute = await _context.Institutes.FindAsync(id);
            _context.Institutes.Remove(institute);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institute = await _context.Institutes
                .FirstOrDefaultAsync(d => d.InstituteID == id);

            if (institute == null)
            {
                return NotFound();
            }

            return View(institute);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, [Bind("InstituteID,Name,Arch,Role")] Institute institute)
        {
            if (id != institute.InstituteID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var specialities = _context.Specialities
                        .Where(s => s.InstituteID == institute.InstituteID);

                    if (institute.Arch == true)
                    {
                        institute.Arch = false;
                        foreach (var speciality in specialities)
                        {
                            speciality.Arch = false;
                            _context.Specialities.Update(speciality);
                        }
                    }
                    else
                    {
                        institute.Arch = true;
                        foreach (var speciality in specialities)
                        {
                            speciality.Arch = true;
                            _context.Specialities.Update(speciality);
                        }
                    }
                    _context.Update(institute);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    if (!InstituteExists(institute.InstituteID))
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
            return View(institute);
        }

        private bool InstituteExists(int id)
        {
            return _context.Institutes.Any(e => e.InstituteID == id);
        }
    }
}
