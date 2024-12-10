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
using Portal.Models.Elective;
using Portal.ViewModel.Elective;

namespace Portal.Controllers
{
    public class ElectivesController : Controller
    {
        private readonly AcademyContext _context;

        public ElectivesController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(await _context.Electives
                .OrderBy(e => e.DepartmentID)
                .ToListAsync());
        }

        public async Task<IActionResult> ChooseElectives()
        {
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(await _context.Electives
                .OrderBy(e => e.DepartmentID)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var elective = await _context.Electives
                .Include(e => e.Themes)
                .FirstOrDefaultAsync(m => m.ElectiveID == id);

            if (elective == null)
            {
                return NotFound();
            }

            return View(elective);
        }

        public async Task<IActionResult> Create(int? id)
        {
            var groups = await _context.Groups
                .Include(s => s.Students)
                .ToListAsync();
            ViewBag.Groups = groups;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ElectiveID,Name,ShortName,DepartmentID")] Elective elective, int id, int[] IdStudents)
        {
            if (ModelState.IsValid)
            {
                elective.DepartmentID = id;
                _context.Electives.Add(elective);
                await _context.SaveChangesAsync();

                foreach (int studId in IdStudents)
                {
                    El_Stud_Link el_Stud_Link = new()
                    {
                        ElectiveID = elective.ElectiveID,
                        StudentID = studId
                    };
                    _context.El_Stud_Links.Add(el_Stud_Link);
                }
                await _context.SaveChangesAsync();

                return RedirectToAction("ChooseDepartment", "Departments");
            }

            var groups = await _context.Groups
                .Include(s => s.Students)
                .ToListAsync();
            ViewBag.Groups = groups;

            return View(elective);
        }

        public async Task<IActionResult> ElectiveCreate()
        {
            var groups = await _context.Groups
                .Include(s => s.Students)
                .ToListAsync();

            var departments = await _context.Departments
                .ToListAsync();

            ViewBag.Groups = groups;
            ViewBag.Departments = departments;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ElectiveCreate([Bind("ElectiveID,Name,ShortName,DepartmentID")] Elective elective, int[] IdStudents)
        {
            if (ModelState.IsValid)
            {
                _context.Electives.Add(elective);
                await _context.SaveChangesAsync();

                foreach (int studId in IdStudents)
                {
                    El_Stud_Link el_Stud_Link = new()
                    {
                        ElectiveID = elective.ElectiveID,
                        StudentID = studId
                    };
                    _context.El_Stud_Links.Add(el_Stud_Link);
                }
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Electives");
            }

            var groups = await _context.Groups
                .Include(s => s.Students)
                .ToListAsync();

            var departments = await _context.Departments
               .ToListAsync();

            ViewBag.Departments = departments;
            ViewBag.Groups = groups;

            return View(elective);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var elective = await _context.Electives.FindAsync(id);
            var links = await _context.El_Stud_Links.Where(l => l.ElectiveID == id).ToListAsync();
            var students = await _context.Students.Where(s => s.Status == true).ToListAsync();
            var groups = await _context.Groups.ToListAsync();
            List<Department> departments = await _context.Departments.ToListAsync();

            List<Student> electiveStudents = new();
            foreach (var link in links)
            {
                Student student = students.Find(s => s.StudentID == link.StudentID);
                electiveStudents.Add(student);
            }

            List<ElectiveStudent> viewStudents = new();
            foreach (var student in students)
            {
                bool status = false;
                status = electiveStudents.Exists(s => s.StudentID == student.StudentID);
                ElectiveStudent electiveStudent = new()
                {
                    Student = student,
                    IsActive = status
                };
                viewStudents.Add(electiveStudent);
            }

            List<ElectiveGroup> electiveGroups = new();
            foreach (var group in groups)
            {
                ElectiveGroup electiveGroup = new()
                {
                    Group = group,
                    ElectiveStudents = new()
                };
                foreach (var es in viewStudents)
                {
                    if (es.Student.GroupID == group.GroupID)
                    {
                        electiveGroup.ElectiveStudents.Add(es);
                    }
                }
                electiveGroups.Add(electiveGroup);
            }

            List<ElectiveDepartment> viewDepartments = new();
            foreach (Department department in departments)
            {
                bool status = false;
                if (department.DepartmentID == elective.DepartmentID) status = true;
                ElectiveDepartment electiveDepartment = new()
                {
                    Department = department,
                    IsActive = status
                };
                viewDepartments.Add(electiveDepartment);
            }

            if (elective == null)
            {
                return NotFound();
            }

            ViewBag.Groups = electiveGroups;
            ViewBag.Departments = viewDepartments;
            return View(elective);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ElectiveID,Name,ShortName,DepartmentID")] Elective elective, int[] IdStudents)
        {
            if (id != elective.ElectiveID)
            {
                return NotFound();
            }

            var links = await _context.El_Stud_Links.Where(l => l.ElectiveID == id).ToListAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    foreach (var link in links)
                    {
                        _context.El_Stud_Links.Remove(link);
                    }
                    await _context.SaveChangesAsync();

                    foreach (var ids in IdStudents)
                    {
                        El_Stud_Link stud_Link = new()
                        {
                            ElectiveID = id,
                            StudentID = ids
                        };
                        _context.El_Stud_Links.Add(stud_Link);
                    }
                    _context.Update(elective);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ElectiveExists(elective.ElectiveID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("ChooseDepartment", "Departments");
            }


            var students = await _context.Students.Where(s => s.Status == true).ToListAsync();
            var groups = await _context.Groups.ToListAsync();
            List<Department> departments = await _context.Departments.ToListAsync();

            List<Student> electiveStudents = new();
            foreach (var link in links)
            {
                Student student = students.Find(s => s.StudentID == link.StudentID);
                electiveStudents.Add(student);
            }

            List<ElectiveStudent> viewStudents = new();
            foreach (var student in students)
            {
                bool status = false;
                status = electiveStudents.Exists(s => s.StudentID == student.StudentID);
                ElectiveStudent electiveStudent = new()
                {
                    Student = student,
                    IsActive = status
                };
                viewStudents.Add(electiveStudent);
            }

            List<ElectiveGroup> electiveGroups = new();
            foreach (var group in groups)
            {
                ElectiveGroup electiveGroup = new()
                {
                    Group = group,
                    ElectiveStudents = new()
                };
                foreach (var es in viewStudents)
                {
                    if (es.Student.GroupID == group.GroupID)
                    {
                        electiveGroup.ElectiveStudents.Add(es);
                    }
                }
                electiveGroups.Add(electiveGroup);
            }

            List<ElectiveDepartment> viewDepartments = new();
            foreach (Department department in departments)
            {
                bool status = false;
                if (department.DepartmentID == elective.DepartmentID) status = true;
                ElectiveDepartment electiveDepartment = new()
                {
                    Department = department,
                    IsActive = status
                };
                viewDepartments.Add(electiveDepartment);
            }

            ViewBag.Groups = electiveGroups;
            ViewBag.Departments = viewDepartments;
            return View(elective);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var elective = await _context.Electives
                .FirstOrDefaultAsync(m => m.ElectiveID == id);
            if (elective == null)
            {
                return NotFound();
            }

            return View(elective);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var elective = await _context.Electives.FindAsync(id);
            _context.Electives.Remove(elective);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ElectiveExists(int id)
        {
            return _context.Electives.Any(e => e.ElectiveID == id);
        }
    }
}
