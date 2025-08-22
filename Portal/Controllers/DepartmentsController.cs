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
            var electives = await _context.Electives
                .OrderBy(e => e.Name)
                .ToListAsync();

            var departments = _context.Departments
                .OrderBy(d => d.Arch)
                    .ThenBy(d => d.Name)
                .Include(sub => sub.Subjects);

            ViewBag.Electives = electives;
            return View(await departments.ToListAsync());
        }

        public async Task<IActionResult> SetSubject(int GroupID, DateTime date_1, DateTime date_2)
        {
            // Получаем группу
            var group = await _context.Groups.FindAsync(GroupID);
            if (group == null) return NotFound();

            var speciality = await _context.Specialities.FindAsync(group.SpecialityID);

            // Получаем ссылки Sub_SpecLinks и преобразуем SubjectID в int на клиенте
            var links = await _context.Sub_SpecLinks
                .Where(l => l.SpecialityID == group.SpecialityID.ToString())
                .ToListAsync();

            var subjectIds = links
                .Select(l => int.Parse(l.SubjectID))
                .ToList();

            // Получаем все предметы по ссылкам
            var subjects = await _context.Subjects
                .Where(s => subjectIds.Contains(s.SubjectID))
                .ToListAsync();

            // Фильтруем занятия по группе, предметам и датам
            var subjectLessonsQuery = _context.Lessons
                .Where(m => m.GroupID == GroupID && subjectIds.Contains(m.SubjectID));

            if (date_1 != default)
                subjectLessonsQuery = subjectLessonsQuery.Where(m => m.Date >= date_1);
            if (date_2 != default)
                subjectLessonsQuery = subjectLessonsQuery.Where(m => m.Date <= date_2);

            var subjectLessons = await subjectLessonsQuery.ToListAsync();

            // Получаем только предметы, по которым есть оценки
            var subjectIdsFromLessons = subjectLessons
                .Select(l => l.SubjectID)
                .Distinct()
                .ToList();

            subjects = subjects
                .Where(s => subjectIdsFromLessons.Contains(s.SubjectID))
                .ToList();

            // Получаем все департаменты за один запрос
            var departmentIds = subjects
                .Select(s => s.DepartmentID)
                .Distinct()
                .ToList();

            var departments = await _context.Departments
                .Where(d => departmentIds.Contains(d.DepartmentID))
                .OrderBy(d => d.Name)
                .ToListAsync();

            // Формируем модель
            var setSubjectModel = new SetSubjectModel
            {
                Departments = departments,
                Subjects = subjects
            };

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
