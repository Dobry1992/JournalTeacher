using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.ViewModel;

namespace Portal.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly AcademyContext _context;

        public DepartmentsController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var departments = _context.Departments
                .OrderBy(d => d.Arch)
                    .ThenBy(d => d.Name)
                .Include(sub => sub.Subjects);
            return View(await departments.ToListAsync());
        }

        public async Task<IActionResult> ChooseDepartment()
        {
            var departments = _context.Departments
                .OrderBy(d => d.Arch)
                    .ThenBy(d => d.Name)
                .Include(sub => sub.Subjects);
            return View(await departments.ToListAsync());
        }

        public async Task<IActionResult> SetSubject(int GroupID)
        {
            var group = await _context.Groups.FindAsync(GroupID);
            var speciality = await _context.Specialities.FindAsync(group.SpecialityID);
            var links = _context.Sub_SpecLinks.Where(l => l.SpecialityID == group.SpecialityID.ToString());

            List<Subject> subjects = new();
            List<Department> departments = new();

            if (links != null)
            {
                foreach (var link in links)
                {
                    Subject subject = await _context.Subjects.FindAsync(int.Parse(link.SubjectID));
                    subjects.Add(subject);
                }
            }

            if (subjects != null)
            {
                foreach(var subject in subjects)
                {
                    Department department = await _context.Departments.FindAsync(subject.DepartmentID);
                    departments.Add(department);
                }
            }

            var distDepartments = departments.Distinct();

            SetSubjectModel setSubjectModel = new();
            setSubjectModel.Departments = distDepartments.OrderBy(d => d.Name).ToList();
            setSubjectModel.Subjects = subjects;

            ViewBag.GroupID = GroupID;
            return View(setSubjectModel);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(sub => sub.Subjects)
                    .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.DepartmentID == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
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
        public async Task<IActionResult> Create([Bind("DepartmentID,Name,InstituteID,Role")] Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            ViewData["InstituteID"] = new SelectList(_context.Institutes, "InstituteID", "Name");
            return View(department);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DepartmentID,Name,Arch,InstituteID,Role")] Department department)
        {
            if (id != department.DepartmentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DepartmentID))
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
            return View(department);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(m => m.DepartmentID == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            _context.Departments.Remove(department);
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

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentID == id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, [Bind("DepartmentID,Name,Arch,InstituteID,Role")] Department department)
        {
            if (id != department.DepartmentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var subjects = _context.Subjects
                        .Where(s => s.DepartmentID == department.DepartmentID)
                        .Include(t => t.Themes);

                    if (department.Arch == true)
                    {
                        department.Arch = false;
                        foreach (var subject in subjects)
                        {
                            subject.Arch = false;
                            _context.Update(subject);
                            foreach (var theme in subject.Themes)
                            {
                                theme.Arch = false;
                                _context.Update(subject);
                            }
                        }
                    }
                    else
                    {
                        department.Arch = true;
                        foreach (var subject in subjects)
                        {
                            subject.Arch = true;
                            _context.Update(subject);
                            foreach (var theme in subject.Themes)
                            {
                                theme.Arch = true;
                                _context.Update(subject);
                            }
                        }
                    }
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    if (!DepartmentExists(department.DepartmentID))
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
            return View(department);
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.DepartmentID == id);
        }
    }
}
