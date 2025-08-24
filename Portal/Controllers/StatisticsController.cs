using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly AcademyContext _context;

        public StatisticsController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> IndexInst()
        {
            return View(await _context.Institutes
                .OrderBy(i => i.Arch)
                    .ThenBy(i => i.Name)
                .ToListAsync());
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> IndexGroups(string searchString)
        {
            var groups = from g in _context.Groups
                         select g;

            var intitutes = from i in _context.Institutes
                            orderby i.Name
                            select i;

            if (!String.IsNullOrEmpty(searchString))
            {
                groups = groups.Where(g => g.Name == searchString || g.DateExit.Year.ToString() == searchString || g.DateEnter.Year.ToString() == searchString);
            }

            ViewBag.Institutes = intitutes;

            return View(await groups
                .Include(g => g.Speciality)
                    .ThenInclude(s => s.Institute)
                .AsNoTracking()
                .OrderBy(g => g.DateEnter)
                .OrderBy(i => i.InstituteID)
                .ToListAsync());
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> IndexStudents(string searchString)
        {
            var students = from s in _context.Students
                           select s;

            students = students.Where(s => s.LastName == searchString);

            return View(await students.AsNoTracking()
                .Include(s => s.Group)
                    .ThenInclude(g => g.Speciality)
                        .ThenInclude(s => s.Institute)
                .OrderBy(s => s.LastName)
                .ToListAsync());
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> Teachers(string searchString_1, string searchString_2, string searchString_3)
        {
            //Средние показатели преподавателей
            List<InstTeacherRaiting> teacherRaiting = new();
            var teachersMarks = _context.Marks.Where(m => m.SignatureOfTeacher != null);
            var teachers = from mark in teachersMarks
                           group mark by mark.SignatureOfTeacher into t
                           select new { NickName = t.Key };
            foreach (var teacher in teachers)
            {
                List<double> mrks = new();
                foreach (var mark in teachersMarks)
                {
                    if (mark.SignatureOfTeacher == teacher.NickName && double.TryParse(mark.Value, out var m))
                    {
                        mrks.Add(m);
                    }
                }
                InstTeacherRaiting instTeacherRaiting = new();
                instTeacherRaiting.Teacher = teacher.NickName;
                instTeacherRaiting.Raiting = Math.Round(mrks.Sum() / mrks.Count, 3);
                teacherRaiting.Add(instTeacherRaiting);
            }

            //Показатели преподаватель-предмет
            var subMarks = _context.Marks.Where(m => m.SubjectID.ToString() == searchString_1 && m.SignatureOfTeacher != null).AsNoTracking();
            var subTeachers = from mark in subMarks
                              group mark by mark.SignatureOfTeacher into t
                              select new { NickName = t.Key };
            List<TeachersSubView> teachersSubViews = new();
            foreach (var teacher in subTeachers)
            {
                TeachersSubView teachersSubView = new();
                teachersSubView.FullName = teacher.NickName;
                var subjectMarks = await subMarks.Where(m => m.SignatureOfTeacher == teacher.NickName).AsNoTracking().ToListAsync();
                List<double> subjectMarksDouble = new();
                foreach (var mark in subjectMarks)
                {
                    if (double.TryParse(mark.Value, out double number))
                    {
                        subjectMarksDouble.Add(number);
                    }
                }
                teachersSubView.SubValue = (Math.Round((subjectMarksDouble.Sum() / subjectMarksDouble.Count), 3)).ToString().Replace(",", ".");
                teachersSubViews.Add(teachersSubView);
            }

            //Показатели преподаватель-кафедра
            var depMarks = _context.Marks.Where(m => m.DepartmentID.ToString() == searchString_2 && m.SignatureOfTeacher != null).AsNoTracking();
            var depTeachers = from mark in depMarks
                              group mark by mark.SignatureOfTeacher into t
                              select new { NickName = t.Key };
            List<TeachersDepView> teachersDepViews = new();
            foreach (var teacher in depTeachers)
            {
                TeachersDepView teachersDepView = new();
                teachersDepView.FullName = teacher.NickName;
                var departmentMarks = await _context.Marks.Where(m => m.SignatureOfTeacher == teacher.NickName).AsNoTracking().ToListAsync();
                List<double> depMarksDouble = new();
                foreach (var mark in departmentMarks)
                {
                    if (double.TryParse(mark.Value, out double number))
                    {
                        depMarksDouble.Add(number);
                    }
                }
                teachersDepView.DepValue = (Math.Round((depMarksDouble.Sum() / depMarksDouble.Count), 3)).ToString().Replace(",", ".");
                teachersDepViews.Add(teachersDepView);
            }

            //Показатель преподаватель-группа
            var groupMarks = _context.Marks.Where(m => m.GroupID.ToString() == searchString_3 && m.SignatureOfTeacher != null);
            var groupTeachers = from mark in groupMarks
                                group mark by mark.SignatureOfTeacher into t
                                select new { NickName = t.Key };
            List<TeachersGroupView> teachersGroupViews = new();
            foreach (var teacher in groupTeachers)
            {
                TeachersGroupView teachersGroupView = new();
                teachersGroupView.FullName = teacher.NickName;
                var grMarks = await groupMarks.Where(m => m.SignatureOfTeacher == teacher.NickName).AsNoTracking().ToListAsync();
                List<double> groupMarksDouble = new();
                foreach (var mark in grMarks)
                {
                    if (double.TryParse(mark.Value, out double number))
                    {
                        groupMarksDouble.Add(number);
                    }
                }
                teachersGroupView.GroupValue = (Math.Round((groupMarksDouble.Sum() / groupMarksDouble.Count), 3)).ToString().Replace(",", ".");
                teachersGroupViews.Add(teachersGroupView);
            }

            ViewBag.SearchString_1 = searchString_1;
            ViewBag.SearchString_2 = searchString_2;
            ViewBag.SearchString_3 = searchString_3;
            ViewBag.TeacherRaiting = teacherRaiting.OrderByDescending(t => t.Raiting);
            ViewBag.Subjects = _context.Subjects.OrderBy(s => s.Name).AsNoTracking();
            ViewBag.Groups = _context.Groups.OrderBy(g => g.Name).AsNoTracking();
            ViewBag.Departments = _context.Departments.OrderBy(d => d.Name).AsNoTracking();
            ViewBag.TeachersSubViews = teachersSubViews.OrderBy(t => t.FullName);
            ViewBag.TeachersDepViews = teachersDepViews.OrderBy(t => t.FullName);
            ViewBag.TeachersGroupViews = teachersGroupViews.OrderBy(t => t.FullName);

            return View(new
            {
                SearchString_1 = searchString_1,
                SearchString_2 = searchString_2,
                SearchString_3 = searchString_3
            });
        }

        [Authorize(Roles = "User, SuperAdmin")]
        public async Task<IActionResult> IndexSpeciality()
        {
            var rawData = await _context.Groups
                .Select(g => new
                {
                    g.Speciality.SpecialityID,
                    g.Speciality.Name,
                    Year = g.DateEnter.Year
                })
                .ToListAsync();

            var specialityYears = rawData
                .GroupBy(x => new { x.SpecialityID, x.Name })
                .Select(g => new SpecialityYears
                {
                    SpecialityID = g.Key.SpecialityID,
                    SpecialityName = g.Key.Name,
                    Years = g.Select(x => x.Year)
                             .Distinct()
                             .OrderBy(y => y)
                             .ToList()
                })
                .ToList();

            return View(specialityYears);
        }

    }
}