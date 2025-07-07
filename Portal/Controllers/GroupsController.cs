using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task<IActionResult> ChooseGroup(int SubjectID)
        {
            List<Journal> journals = await _context.Journals
                .Where(j => j.SubjectID == SubjectID)
                .ToListAsync();
            List<Group> groups = new();
            foreach (Journal journal in journals)
            {
                Group group = await _context.Groups.FindAsync(journal.GroupID);
                groups.Add(group);
            }
            List<Theme> themes = new();
            themes = _context.Themes.Where(t => t.SubjectID == SubjectID && t.Name != "Контрольное занятие").ToList();
            ViewBag.Themes = themes;

            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => t.Name == "Семинарское занятие" || t.Name == "Практическое занятие"
               || t.Name == "Лабораторное занятие" || t.Name == "Лекция"), "TypeOfExerciseID", "Name");
            ViewBag.SubjectID = SubjectID;
            ViewBag.Groups = groups;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChooseGroup(int[] groupsId, int SubjectID, [Bind("LessonID,Date,Comment,FlagF,ThemeID,TypeOfExerciseID,GroupID,SubjectID,Signature")] Lesson lesson)
        {
            if (lesson.Date > DateTime.Now)
            {
                ModelState.AddModelError("", "Невозможно создать занятие в будующем!");
            }

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

                lesson.SubjectID = SubjectID;
                lesson.GroupID = groupsId[0];
                lesson.Signature = teacher;
                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();

                var subject = await _context.Subjects.FindAsync(SubjectID);
                var group = await _context.Groups.FindAsync(groupsId[0]);
                var students = _context.Students.Where(s => s.GroupID == groupsId[0]);

                foreach (var student in students)
                {
                    Mark mark = new();
                    mark.Value = "";
                    mark.Date = lesson.Date;
                    mark.SubjectID = SubjectID;
                    mark.GroupID = groupsId[0];
                    mark.LessonID = lesson.LessonID;
                    mark.TypeOfExerciseID = lesson.TypeOfExerciseID;
                    mark.DepartmentID = subject.DepartmentID;
                    mark.InstituteID = group.InstituteID;
                    mark.SpecialityID = group.SpecialityID;
                    mark.ThemeID = lesson.ThemeID;
                    mark.StudentID = student.StudentID;
                    _context.Marks.Add(mark);
                }
                await _context.SaveChangesAsync();

                for (int i = 1; i < groupsId.Length; i++)
                {
                    Lesson lessoni = new();
                    lessoni.Date = lesson.Date;
                    lessoni.Comment = lesson.Comment;
                    lessoni.Signature = teacher;
                    lessoni.SubjectID = SubjectID;
                    lessoni.ThemeID = lesson.ThemeID;
                    lessoni.TypeOfExerciseID = lesson.TypeOfExerciseID;
                    lessoni.GroupID = groupsId[i];
                    _context.Lessons.Add(lessoni);
                    _context.SaveChanges();

                    var groupi = await _context.Groups.FindAsync(groupsId[i]);
                    var studentsi = _context.Students.Where(s => s.GroupID == groupsId[i]);

                    foreach (var studenti in studentsi)
                    {
                        Mark mark = new();
                        mark.Value = "";
                        mark.Date = lessoni.Date;
                        mark.SubjectID = SubjectID;
                        mark.GroupID = groupsId[i];
                        mark.LessonID = lessoni.LessonID;
                        mark.TypeOfExerciseID = lessoni.TypeOfExerciseID;
                        mark.DepartmentID = subject.DepartmentID;
                        mark.InstituteID = groupi.InstituteID;
                        mark.SpecialityID = groupi.SpecialityID;
                        mark.ThemeID = lessoni.ThemeID;
                        mark.StudentID = studenti.StudentID;
                        _context.Marks.Add(mark);
                    }
                }
                await _context.SaveChangesAsync();

                return View("Success");
            }
            List<Theme> themes = new();
            themes = _context.Themes.Where(t => t.SubjectID == SubjectID && t.Name != "Контрольное занятие").ToList();
            ViewBag.Themes = themes;

            ViewData["TypeOfExerciseID"] = new SelectList(_context.Types.Where(t => t.Name == "Семинарское занятие" || t.Name == "Практическое занятие"
                || t.Name == "Лабораторное занятие" || t.Name == "Лекция"), "TypeOfExerciseID", "Name");
            return View(lesson);
        }

        public async Task<IActionResult> ChooseSubject()
        {
            List<Department> departments = await _context.Departments
                .Include(d => d.Subjects)
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(departments);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var groups = _context.Groups
                .OrderByDescending(g => g.DateExit)
                    .ThenBy(g => g.Name)
                .Include(s => s.Students)
                .Include(s => s.Speciality);
            return View(await groups.ToListAsync());
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @group = await _context.Groups
                .Include(s => s.Students)
                .Include(s => s.Speciality)
                    .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.GroupID == id);

            if (@group == null)
            {
                return NotFound();
            }

            return View(@group);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH, ANB-HEAD, ANB-CI, ANB-ICDA, ANB-IST, User")]
        public async Task<IActionResult> DetailsGroup(int? id, string searchString)
        {
            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.Groups
                .Include(s => s.Students)
                .Include(j => j.Journals)
                    .ThenInclude(s => s.Subject)
                .Include(s => s.Speciality)
                    .ThenInclude(s => s.Institute)
                    .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.GroupID == id);

            //Текущий средний балл
            string term;
            var date = DateTime.Now.AddYears(-1);
            var typeSZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Семинарское занятие");
            var typePZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Практическое занятие");
            var typeLZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лабораторное занятие");
            var typeL = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Лекция");
            var typeKM = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Контрольное мероприятие");
            var typeGPZ = await _context.Types.FirstOrDefaultAsync(t => t.Name == "Городское практическое занятие");
            var institute = await _context.Institutes.FirstOrDefaultAsync(i => i.InstituteID == group.InstituteID);
            List<double> marksAverage = new();
            List<Mark> marks = new();

            //выборка оценок.
            if (DateTime.Now.Month.ToString() == "9" || DateTime.Now.Month.ToString() == "10" || DateTime.Now.Month.ToString() == "11" || DateTime.Now.Month.ToString() == "12")
            {
                term = "первый семестр";
                marks = await _context.Marks
                    .Where(m =>
                        m.GroupID == id &&
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
                        m.GroupID == id &&
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
                    .Where(m => m.GroupID == id &&
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

            //Текущий средний балл за предмет по месяцам
            int subjectID = 0;
            var subjects = await _context.Subjects.ToListAsync();
            if (!String.IsNullOrEmpty(searchString))
            {
                if (int.TryParse(searchString, out var subID))
                {
                    subjectID = subID;
                }
            }
            else if (group.Journals.Count != 0)
            {
                subjectID = group.Journals.FirstOrDefault().SubjectID;
            }
            var subject = subjects.FirstOrDefault(s => s.SubjectID == subjectID);
            var subjectMarks = marks.Where(m => m.SubjectID == subjectID);
            Dictionary<string, string> raitingTimeSubject = new();
            List<string> months = new List<string>() { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            foreach (var month in months)
            {
                int monthNumber = months.IndexOf(month) + 1;
                var monthMarks = subjectMarks
                    .Where(m => m.Date.Month.ToString() == monthNumber.ToString())
                    .ToList();
                raitingTimeSubject.Add(month, Math.Round(await _groupRating.CalculateGroupRatingAsync(monthMarks), 3).ToString().Replace(",", "."));
            }

            //Количество слушателей/курсантов
            var students = _context.Students.Where(s => s.Status == true && s.GroupID == id);

            //Рейтинг слушателей группы
            List<GroupRaiting> groupRating = new();
            foreach (var student in students)
            {
                List<double> marksValue = new();
                var marksStudent = marks
                    .Where(m => m.StudentID == student.StudentID)
                    .ToList();
                foreach (var mark in marksStudent)
                {
                    if (double.TryParse(mark.Value, out var m))
                    {
                        marksValue.Add(m);
                    }
                }
                if (marksValue.Count != 0)
                {
                    double? ratingStudent = _studentRating.GetStudentAverageMark(student, marksStudent);
                    GroupRaiting groupRatingStud = new()
                    {
                        Student = student,
                        Raiting = Math.Round((double)ratingStudent, 3),
                        NumberOfMark = marksValue.Count
                    };
                    groupRating.Add(groupRatingStud);
                }
                else
                {
                    double ratingStudent = 0;
                    GroupRaiting groupRatingStud = new()
                    {
                        Student = student,
                        Raiting = ratingStudent,
                        NumberOfMark = 0
                    };
                    groupRating.Add(groupRatingStud);
                }
            }

            double rating = Math.Round(groupRating.Average(g => g.Raiting), 3);

            //Оценочные показатели группы
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

            //Общая среднемесячная успеваемость
            Dictionary<string, string> raitingTime = new();
            foreach (var month in months)
            {
                int monthNumber = months.IndexOf(month) + 1;
                var monthMarks = marks
                    .Where(m => m.Date.Month.ToString() == monthNumber.ToString())
                    .ToList();
                raitingTime.Add(month, Math.Round(await _groupRating.CalculateGroupRatingAsync(monthMarks), 3).ToString().Replace(",", "."));
            }

            if (group == null)
            {
                return NotFound();
            }

            ViewBag.Term = term;
            ViewBag.Raiting = rating;
            ViewBag.Students = students.Count();
            ViewBag.GroupRaiting = groupRating.OrderByDescending(s => s.Raiting);
            ViewBag.BestStudent = groupRating.OrderByDescending(s => s.Raiting).FirstOrDefault();
            ViewBag.WorseStudent = groupRating.OrderByDescending(s => s.Raiting).LastOrDefault();
            ViewBag.Institute = institute;
            ViewBag.MarksNumber = marksNumber;
            ViewBag.MarksPercent = marksPercent;
            ViewBag.Year = yearsStudy;
            ViewBag.TimeRaiting = raitingTime;
            ViewBag.SubjectRaiting = raitingTimeSubject;
            ViewBag.Subject = subject;

            return View(group);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create(object selectSpeciality = null)
        {
            var specialityQuery = from sp in _context.Specialities
                                  orderby sp.Name
                                  select sp;
            ViewBag.SpecialityID = new SelectList(specialityQuery, "SpecialityID", "Name", selectSpeciality);
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
                    group.InstituteID = speciality.InstituteID;
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
            }
            catch (RetryLimitExceededException)
            {
                ModelState.AddModelError("", "Возникла ошибка обратитесь к администратору.");
            }

            var specialityQuery = from sp in _context.Specialities
                                  orderby sp.Name
                                  select sp;
            ViewBag.GroupID = new SelectList(specialityQuery, "GroupID", "Name", group.SpecialityID);
            return View(group);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @group = await _context.Groups.FindAsync(id);
            if (@group == null)
            {
                return NotFound();
            }
            ViewData["SpecialityID"] = new SelectList(_context.Specialities, "SpecialityID", "Name", @group.SpecialityID);
            return View(@group);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GroupID,SpecialityID,Name,DateEnter,DateExit,InstituteID")] Group @group)
        {
            if (id != @group.GroupID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@group);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupExists(@group.GroupID))
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
            ViewData["SpecialityID"] = new SelectList(_context.Groups, "SpecialityID", "Name", @group.SpecialityID);
            return View(@group);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @group = await _context.Groups
                .FirstOrDefaultAsync(m => m.GroupID == id);
            if (@group == null)
            {
                return NotFound();
            }

            return View(@group);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @group = await _context.Groups.FindAsync(id);
            _context.Groups.Remove(@group);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GroupExists(int id)
        {
            return _context.Groups.Any(e => e.GroupID == id);
        }
    }
}
