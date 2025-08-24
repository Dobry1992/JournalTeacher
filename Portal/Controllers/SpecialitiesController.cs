using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.Services;
using Portal.ViewModel;
using Portal.ViewModel.Raiting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class SpecialitiesController : Controller
    {
        private readonly AcademyContext _context;
        private readonly StudentAverageMarkService _studentAverageMarkService;
        private readonly InstituteAverageMarkService _instituteAverage;

        public SpecialitiesController(AcademyContext context, StudentAverageMarkService studentAverageMarkService, InstituteAverageMarkService instituteAverage)
        {
            _context = context;
            _studentAverageMarkService = studentAverageMarkService;
            _instituteAverage = instituteAverage;
        }

        public IActionResult NavMenuJournal()
        {
            return PartialView("_NavMenuJournal");
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var specialities = _context.Specialities
                .OrderBy(s => s.Arch)
                    .ThenBy(s => s.Name)
                .Include(g => g.Groups);
            return View(await specialities.ToListAsync());
        }

        public async Task<IActionResult> ChooseGroup()
        {
            var specialities = _context.Specialities
                  .OrderBy(s => s.Arch)
                     .ThenBy(s => s.Name)
                 .Include(g => g.Groups);
            return View(await specialities.ToListAsync());
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var speciality = await _context.Specialities
                .FirstOrDefaultAsync(m => m.SpecialityID == id);
            if (speciality == null)
            {
                return NotFound();
            }

            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create()
        {
            ViewData["InstituteID"] = new SelectList(_context.Institutes, "InstituteID", "Name");
            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SpecialityID,Name,TimeOFStudy,InstituteID")] Speciality speciality)
        {
            if (ModelState.IsValid)
            {
                _context.Add(speciality);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var speciality = await _context.Specialities.FindAsync(id);
            if (speciality == null)
            {
                return NotFound();
            }
            ViewData["InstituteID"] = new SelectList(_context.Institutes, "InstituteID", "Name");
            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("SpecialityID,Name,TimeOFStudy,Arch,InstituteID")] Speciality speciality)
        {
            if (id != speciality.SpecialityID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(speciality);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpecialityExists(speciality.SpecialityID))
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
            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var speciality = await _context.Specialities
                .FirstOrDefaultAsync(m => m.SpecialityID == id);
            if (speciality == null)
            {
                return NotFound();
            }

            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var speciality = await _context.Specialities.FindAsync(id);
            _context.Specialities.Remove(speciality);
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

            var speciality = await _context.Specialities
                .FirstOrDefaultAsync(d => d.SpecialityID == id);

            if (speciality == null)
            {
                return NotFound();
            }

            return View(speciality);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, [Bind("SpecialityID,Name,TimeOFStudy,Arch,InstituteID")] Speciality speciality)
        {
            if (id != speciality.SpecialityID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (speciality.Arch == true)
                    {
                        speciality.Arch = false;
                    }
                    else
                    {
                        speciality.Arch = true;
                    }
                    _context.Update(speciality);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    if (!SpecialityExists(speciality.SpecialityID))
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
            return View(speciality);
        }

        [Authorize]
        public async Task<IActionResult> Analytics(int id, int year)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Количество групп
            var groups = await _context.Groups
                .Where(g => g.SpecialityID == id && g.DateEnter.Year == year)
                .ToListAsync();

            var groupIds = groups
                .Select(g => g.GroupID)
                .ToList();

            //Расчёт среднего бала за текущий семестр.
            string term;
            var date = DateTime.Now.AddYears(-1);
            var typeSZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Семинарское занятие");
            var typePZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Практическое занятие");
            var typeLZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лабораторное занятие");
            var typeL = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лекция");
            var typeKM = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Контрольное мероприятие");
            var typeGPZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Городское практическое занятие");
            List<double> marksAverage = new();
            List<Mark> marks = new();
            if (DateTime.Now.Month.ToString() == "9" || DateTime.Now.Month.ToString() == "10" || DateTime.Now.Month.ToString() == "11" || DateTime.Now.Month.ToString() == "12")
            {
                term = "первый семестр";
                marks = await _context.Marks
                    .Where(m =>
                        m.SpecialityID == id &&
                        groupIds.Contains(m.GroupID) &&
                        (m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeL.TypeOfExerciseID) &&
                        m.Date.Year == DateTime.Now.Year &&
                            (m.Date.Month.ToString() == "9" ||
                            m.Date.Month.ToString() == "10" ||
                            m.Date.Month.ToString() == "11" ||
                            m.Date.Month.ToString() == "12")
                    )
                    .ToListAsync();
                if (marks != null)
                {
                    foreach (var mark in marks)
                    {
                        if (double.TryParse(mark.Value, out var m))
                        {
                            marksAverage.Add(m);
                        }
                    }
                }
            }
            else if (DateTime.Now.Month.ToString() == "1")
            {
                term = "первый семестр";
                marks = await _context.Marks
                    .Where(m =>
                        m.SpecialityID == id &&
                        groupIds.Contains(m.GroupID) &&
                            (
                                m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                                m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                                m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                                m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                                m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                                m.TypeOfExerciseID == typeL.TypeOfExerciseID
                            ) &&
                        ((m.Date.Year == DateTime.Now.Year &&
                        (m.Date.Month.ToString() == "1") ||
                            m.Date.Year == date.Year &&
                            (m.Date.Month.ToString() == "9" ||
                            m.Date.Month.ToString() == "10" ||
                            m.Date.Month.ToString() == "11" ||
                            m.Date.Month.ToString() == "12")))
                    )
                    .ToListAsync();
                if (marks != null)
                {
                    foreach (var mark in marks)
                    {
                        if (double.TryParse(mark.Value, out var m))
                        {
                            marksAverage.Add(m);
                        }
                    }
                }
            }
            else
            {
                term = "второй семестр";
                marks = await _context.Marks
                    .Where(m => m.SpecialityID == id &&
                        groupIds.Contains(m.GroupID) &&
                        (
                            m.TypeOfExerciseID == typeKM.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeGPZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeSZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typePZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeLZ.TypeOfExerciseID ||
                            m.TypeOfExerciseID == typeL.TypeOfExerciseID) &&
                    ((m.Date.Year == DateTime.Now.Year &&
                        (m.Date.Month.ToString() == "2" ||
                        m.Date.Month.ToString() == "3" ||
                        m.Date.Month.ToString() == "4" ||
                        m.Date.Month.ToString() == "5" ||
                        m.Date.Month.ToString() == "6" ||
                        m.Date.Month.ToString() == "7" ||
                        m.Date.Month.ToString() == "8")))
                    )
                    .ToListAsync();
                if (marks != null)
                {
                    foreach (var mark in marks)
                    {
                        if (double.TryParse(mark.Value, out var m))
                        {
                            marksAverage.Add(m);
                        }
                    }
                }
            }

            //Специальность
            var speciality = await _context.Specialities.FindAsync(id);

            //Количество обучающихся
            var students = _context.Students
                .Include(s => s.Group)
                .Where(s => groupIds.Contains(s.GroupID) && s.Status == true)
                .AsNoTracking();
            int studentNumber = students.Count();

            //Рейтинг учащихся
            List<StudentRaiting> studentRaitings = new();
            foreach (var student in students)
            {
                double? studentRating = _studentAverageMarkService.GetStudentAverageMark(student, marks);

                if (studentRating == null)
                {
                    studentRating = 0;
                }

                StudentRaiting sr = new StudentRaiting()
                {
                    Group = student.Group,
                    Student = student,
                    Raiting = studentRating,
                };

                studentRaitings.Add(sr);
            }

            //Рейтинг учебных групп
            List<InstGroupRaiting> groupsRating = new();
            foreach (var group in groups)
            {
                var groupStudents = students
                    .Where(s => s.GroupID == group.GroupID)
                    .ToList();

                var groupMarks = marks.
                    Where(m => m.GroupID == group.GroupID)
                    .ToList();

                List<double> groupStudentRatings = new();
                foreach (var student in groupStudents)
                {
                    double? studentRating = _studentAverageMarkService.GetStudentAverageMark(student, groupMarks);
                    if (studentRating == null)
                    {
                        studentRating = 0;
                    }
                    groupStudentRatings.Add((double)studentRating);
                }

                groupStudentRatings.RemoveAll(m => m == 0);
                double groupRating = 0;

                if (groupStudentRatings.Any())
                {
                    groupRating = Math.Round(groupStudentRatings.Average(), 3);
                }

                InstGroupRaiting instGroupRaiting = new()
                {
                    Group = group,
                    Raiting = groupRating
                };

                groupsRating.Add(instGroupRaiting);
            }

            //Средний балл курса
            double courseRating = 0;
            var filterGroupsRating = groupsRating
                .Where(m => m.Raiting != 0)
                .ToList();
            if (filterGroupsRating.Any())
            {
                courseRating = Math.Round(filterGroupsRating.Average(g => g.Raiting), 3);
            }

            //Оценочные показатели курса
            Dictionary<int, int> marksNumber = new();
            Dictionary<int, decimal> marksPercent = new();
            for (int i = 1; i <= 10; i++)
            {
                if (marksAverage.Count != 0)
                {
                    decimal n1 = marksAverage.Where(x => x == i).Count();
                    decimal n2 = marksAverage.Count;
                    decimal mp = n1 / n2 * 100;
                    marksPercent.Add(i, Math.Round(mp, 3));
                }
                marksNumber.Add(i, marksAverage.Where(x => x == i).Count());
            }

            //Средняя месячная успеваемость
            Dictionary<string, string> raitingTime = new();
            var septemberMarks = marks.Where(m => m.Date.Month.ToString() == "9");
            List<Mark> sepMarks = new();
            foreach (var m in septemberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    sepMarks.Add(m);
                }
            }
            if (sepMarks.Count != 0)
            {
                raitingTime.Add("Сентябрь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(sepMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Сентябрь", "0");
            }

            var octoberMarks = marks.Where(m => m.Date.Month.ToString() == "10");
            List<Mark> octMarks = new();
            foreach (var m in octoberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    octMarks.Add(m);
                }
            }
            if (octMarks.Count != 0)
            {
                raitingTime.Add("Октябрь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(octMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Октябрь", "0");
            }

            var novemberMarks = marks.Where(m => m.Date.Month.ToString() == "11");
            List<Mark> novMarks = new();
            foreach (var m in novemberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    novMarks.Add(m);
                }
            }
            if (novMarks.Count != 0)
            {
                raitingTime.Add("Ноябрь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(novMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Ноябрь", "0");
            }


            var decemberMarks = marks.Where(m => m.Date.Month.ToString() == "12");
            List<Mark> decMarks = new();
            foreach (var m in decemberMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    decMarks.Add(m);
                }
            }
            if (decMarks.Count != 0)
            {
                raitingTime.Add("Декабрь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(decMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Декабрь", "0");
            }

            var januaryMarks = marks.Where(m => m.Date.Month.ToString() == "1");
            List<Mark> janMarks = new();
            foreach (var m in januaryMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    janMarks.Add(m);
                }
            }
            if (janMarks.Count != 0)
            {
                raitingTime.Add("Январь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(janMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Январь", "0");
            }

            var februaryMarks = marks.Where(m => m.Date.Month.ToString() == "2");
            List<Mark> febMarks = new();
            foreach (var m in februaryMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    febMarks.Add(m);
                }
            }
            if (febMarks.Count != 0)
            {
                raitingTime.Add("Февраль", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(febMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Февраль", "0");
            }

            var marchMarks = marks.Where(m => m.Date.Month.ToString() == "3");
            List<Mark> marMarks = new();
            foreach (var m in marchMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    marMarks.Add(m);
                }
            }
            if (marMarks.Count != 0)
            {
                raitingTime.Add("Март", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(marMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Март", "0");
            }

            var aprilMarks = marks.Where(m => m.Date.Month.ToString() == "4");
            List<Mark> aprMarks = new();
            foreach (var m in aprilMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    aprMarks.Add(m);
                }
            }
            if (aprMarks.Count != 0)
            {
                raitingTime.Add("Апрель", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(aprMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Апрель", "0");
            }

            var mayMarks = marks.Where(m => m.Date.Month.ToString() == "5");
            List<Mark> mMarks = new();
            foreach (var m in mayMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    mMarks.Add(m);
                }
            }
            if (mMarks.Count != 0)
            {
                raitingTime.Add("Май", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(mMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Май", "0");
            }

            var juneMarks = marks.Where(m => m.Date.Month.ToString() == "6");
            List<Mark> junMarks = new();
            foreach (var m in juneMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    junMarks.Add(m);
                }
            }
            if (junMarks.Count != 0)
            {
                raitingTime.Add("Июнь", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(junMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Июнь", "0");
            }

            var julyMarks = marks.Where(m => m.Date.Month.ToString() == "7");
            List<Mark> julMarks = new();
            foreach (var m in julyMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    julMarks.Add(m);
                }
            }
            if (julMarks.Count != 0)
            {
                raitingTime.Add("Июль", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(julMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Июль", "0");
            }

            var augustMarks = marks.Where(m => m.Date.Month.ToString() == "8");
            List<Mark> augMarks = new();
            foreach (var m in augustMarks)
            {
                if (double.TryParse(m.Value, out var mark))
                {
                    augMarks.Add(m);
                }
            }
            if (augMarks.Count != 0)
            {
                raitingTime.Add("Август", Math.Round(await _instituteAverage.GetInstituteAverageMarkAsync(augMarks), 3).ToString().Replace(",", "."));
            }
            else
            {
                raitingTime.Add("Август", "0");
            }

            //Учебный год
            string yearsStudy = "";
            if (DateTime.Now.Month.ToString() == "9" || DateTime.Now.Month.ToString() == "10" || DateTime.Now.Month.ToString() == "11" || DateTime.Now.Month.ToString() == "12")
            {
                DateTime dateTimePlus = DateTime.Now.AddYears(1);
                yearsStudy = DateTime.Now.Year + "/" + dateTimePlus.Year;
            }
            else
            {
                DateTime dateTimeMinus = DateTime.Now.AddYears(-1);
                yearsStudy = dateTimeMinus.Year + "/" + DateTime.Now.Year;
            }

            var bestStudent = studentRaitings.OrderByDescending(s => s.Raiting).FirstOrDefault();
            if (bestStudent == null)
            {
                InstStudRaiting i = new();
                i.Raiting = 0;
                ViewBag.BestStudent = i;
            }
            else
            {
                if (bestStudent.Raiting == null)
                {
                    bestStudent.Raiting = 0;
                }
                ViewBag.BestStudent = bestStudent;
            }
            var worseStudent = studentRaitings.OrderByDescending(s => s.Raiting).LastOrDefault();
            if (worseStudent == null)
            {
                InstStudRaiting i = new();
                i.Raiting = 0;
                ViewBag.WorseStudent = i;
            }
            else
            {
                if (worseStudent.Raiting == null)
                {
                    worseStudent.Raiting = 0;
                }
                ViewBag.WorseStudent = worseStudent;
            }
            var bestGroup = groupsRating.OrderByDescending(g => g.Raiting).FirstOrDefault();
            if (bestGroup == null)
            {
                InstStudRaiting i = new();
                i.Raiting = 0;
                ViewBag.BestGroup = i;
            }
            else
            {
                ViewBag.BestGroup = bestGroup;
            }
            var worseGroup = groupsRating.OrderByDescending(g => g.Raiting).LastOrDefault();
            if (worseGroup == null)
            {
                InstStudRaiting i = new();
                i.Raiting = 0;
                ViewBag.WorseGroup = i;
            }
            else
            {
                ViewBag.WorseGroup = worseGroup;
            }

            SpecialityAnalyticViewModel specialityAnalyticViewModel = new() 
            { 
                SpecialityID = id,
                SpecialityName = speciality.Name,
                Term = term,
                Raiting = Math.Round(courseRating, 3),
                StudentNumber = studentNumber,
                GroupNumber = groups.Count(),
                EnterYear = year,
                StudentsRaiting = studentRaitings
                    .OrderByDescending(s => s.Raiting)
                    .ToList(),
                GroupsRaiting = groupsRating
                    .OrderByDescending(g => g.Raiting)
                    .ToList(),
                MarksNumber = marksNumber,
                MarksPercent = marksPercent,
                TimeRaiting = raitingTime,
                Year = yearsStudy
            };

            return View(specialityAnalyticViewModel);
        }

        [Authorize]
        public async Task<IActionResult> CourseStatement(int id, int year)
        {
            var speciality = await _context.Specialities.FindAsync(id);

            var groups = await _context.Groups
                .Where(g => g.SpecialityID == id && g.DateEnter.Year == year)
                .ToListAsync();

            if (!groups.Any())
                return NotFound("Групп для специальности и года нет.");

            var groupIds = groups.Select(g => g.GroupID).ToList();

            var typeKR = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовая работа");
            var typeKP = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Курсовой проект");

            var studentsData = await _context.Students
                .Where(s => groupIds.Contains(s.GroupID))
                .OrderBy(s => s.LastName)
                .Select(s => new
                {
                    Student = new { s.StudentID, s.Name, s.Surname, s.LastName },
                    Marks = s.Marks
                        .Where(m =>
                            m.FlagF == 0 &&
                            m.TypeOfExerciseID != typeKP.TypeOfExerciseID &&
                            m.TypeOfExerciseID != typeKR.TypeOfExerciseID
                        )
                        .Select(m => new { m.SubjectID, m.Value })
                })
                .AsNoTracking()
                .ToListAsync();

            if (!studentsData.Any())
                return NotFound("Студентов в группе нет.");

            var subjectIds = studentsData
                .SelectMany(s => s.Marks)
                .Select(m => m.SubjectID)
                .Distinct()
                .ToList();

            var allSubjects = await _context.Subjects
                .Where(sub => subjectIds.Contains(sub.SubjectID))
                .Select(sub => new { sub.SubjectID, sub.ShortName, sub.Name })
                .ToListAsync();

            var students = studentsData.Select(s => new StudentViewModel
            {
                StudentId = s.Student.StudentID,
                Name = s.Student.Name,
                Surname = s.Student.Surname,
                LastName = s.Student.LastName,

                SubjectAverages = allSubjects.Select(subject =>
                {
                    var marks = s.Marks
                        .Where(m => m.SubjectID == subject.SubjectID)
                        .Select(m => double.TryParse(m.Value, out var v) ? (double?)v : null)
                        .Where(v => v.HasValue)
                        .Select(v => v.Value)
                        .ToList();

                    return new SubjectAverageViewModel
                    {
                        SubjectId = subject.SubjectID,
                        SubjectName = subject.ShortName,
                        SubjectFullName = subject.Name,
                        AvgMark = marks.Any() ? marks.Average() : (double?)null
                    };
                }).ToList()
            }).ToList();

            return View(students);
        }

        [Authorize]
        public async Task<IActionResult> CourseSummaryStatement(int id, int year)
        {
            return View();
        }

        private bool SpecialityExists(int id)
        {
            return _context.Specialities.Any(e => e.SpecialityID == id);
        }
    }
}
