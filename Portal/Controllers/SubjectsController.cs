using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Helpers.Comparers;
using Portal.Models;
using Portal.Services.Interfaces;
using Portal.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class SubjectsController : Controller
    {
        private readonly AcademyContext _context;
        private readonly IShortNameParser _shortNameParser;

        public SubjectsController(AcademyContext context, IShortNameParser shortNameParser)
        {
            _context = context;
            _shortNameParser = shortNameParser;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var subjects = _context.Subjects
                .OrderBy(s => s.Arch)
                    .ThenBy(s => s.Name)
                .Include(t => t.Themes)
                .Include(dep => dep.Department);
            return View(await subjects.ToListAsync());
        }

        public async Task<IActionResult> ChooseSubject(int? id)
        {
            if (id == null)
                return NotFound();

            var subject = await _context.Subjects
                .Include(s => s.Department)
                .Include(s => s.Themes)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.SubjectID == id);

            if (subject == null)
                return NotFound();

            subject.Themes = subject.Themes
                .OrderBy(t => _shortNameParser.GetNumericParts(t.ShortName), new NumericPartsComparer())
                .ToList();

            return View(subject);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .Include(s => s.Department)
                .Include(t => t.Themes)
                    .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.SubjectID == id);
            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create(object selectSubject = null)
        {
            var subjectQuery = from d in _context.Departments
                               orderby d.Name
                               select d;

            var specialityQuery = from s in _context.Specialities
                                  orderby s.Name
                                  select s;

            ViewBag.DepartmentID = new SelectList(subjectQuery, "DepartmentID", "Name", selectSubject);
            ViewBag.Specialitys = specialityQuery;

            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SubjectID,DepartmentID,Name,ShortName,SpecialitysID")] Subject subject, string[] specialitysId)
        {
            if (ModelState.IsValid)
            {
                _context.Add(subject);
                await _context.SaveChangesAsync();

                var themes = _context.Themes.Where(t => t.SubjectID == subject.SubjectID && t.Name == "Контрольное занятие");
                if (themes.Count() == 0)
                {
                    Theme themeKZ = new();
                    themeKZ.Name = "Контрольное занятие";
                    themeKZ.ShortName = "-";
                    themeKZ.Time = "-";
                    themeKZ.SubjectID = subject.SubjectID;
                    _context.Add(themeKZ);
                    await _context.SaveChangesAsync();
                }

                foreach (var specialityId in specialitysId)
                {
                    Sub_SpecLink subSpecLink = new();
                    subSpecLink.SpecialityID = specialityId;
                    subSpecLink.SubjectID = subject.SubjectID.ToString();
                    _context.Sub_SpecLinks.Add(subSpecLink);
                }
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            var subjectQuery = from d in _context.Departments
                               orderby d.Name
                               select d;

            var specialityQuery = from s in _context.Specialities
                                  orderby s.Name
                                  select s;

            ViewBag.DepartmentID = new SelectList(subjectQuery, "DepartmentID", "Name", subject.DepartmentID);
            ViewBag.Specialitys = specialityQuery;

            return View(subject);
        }

        public IActionResult CreateSubject(int id, object selectSubject = null)
        {
            var subjectQuery = from d in _context.Departments
                               where d.DepartmentID == id
                               orderby d.Name
                               select d;

            var specialityQuery = from s in _context.Specialities
                                  orderby s.Name
                                  select s;

            ViewBag.DepartmentID = new SelectList(subjectQuery, "DepartmentID", "Name", selectSubject);
            ViewBag.Specialitys = specialityQuery;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject(int id, [Bind("SubjectID,DepartmentID,Name,ShortName")] Subject subject, string[] specialitysId)
        {
            if (ModelState.IsValid)
            {
                _context.Add(subject);
                await _context.SaveChangesAsync();

                var themes = _context.Themes.Where(t => t.SubjectID == subject.SubjectID && t.Name == "Контрольное занятие");
                if (themes.Count() == 0)
                {
                    Theme themeKZ = new();
                    themeKZ.Name = "Контрольное занятие";
                    themeKZ.ShortName = "-";
                    themeKZ.Time = "-";
                    themeKZ.SubjectID = subject.SubjectID;
                    _context.Add(themeKZ);
                    await _context.SaveChangesAsync();
                }

                foreach (var specialityId in specialitysId)
                {
                    Sub_SpecLink subSpecLink = new();
                    subSpecLink.SpecialityID = specialityId;
                    subSpecLink.SubjectID = subject.SubjectID.ToString();
                    _context.Sub_SpecLinks.Add(subSpecLink);
                }
                await _context.SaveChangesAsync();

                return RedirectToAction("ChooseDepartment", "Departments");
            }

            var subjectQuery = from d in _context.Departments
                               orderby d.Name
                               select d;
            ViewBag.DepartmentID = new SelectList(subjectQuery, "DepartmentID", "Name", subject.DepartmentID);
            return View(subject);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects.FindAsync(id);

            var specialities = from s in _context.Specialities
                               orderby s.Name
                               select s;

            var links = from l in _context.Sub_SpecLinks
                        where l.SubjectID == id.ToString()
                        select l;

            if (subject == null)
            {
                return NotFound();
            }

            List<CheckSpeciality> checkSpecialities = new();
            foreach (var s in specialities)
            {
                if (links.FirstOrDefault(l => l.SpecialityID == s.SpecialityID.ToString()) != null)
                {
                    CheckSpeciality checkSpeciality = new();
                    checkSpeciality.Speciality = s;
                    checkSpeciality.Ckecked = true;
                    checkSpecialities.Add(checkSpeciality);
                }
                else
                {
                    CheckSpeciality checkSpeciality = new();
                    checkSpeciality.Speciality = s;
                    checkSpeciality.Ckecked = false;
                    checkSpecialities.Add(checkSpeciality);
                }
            }

            ViewData["DepartmentID"] = new SelectList(_context.Departments, "DepartmentID", "Name", subject.DepartmentID);
            ViewBag.Specialities = checkSpecialities;

            return View(subject);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SubjectID,DepartmentID,Name,ShortName,Arch")] Subject subject, string[] specialitysId)
        {
            if (id != subject.SubjectID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(subject);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubjectExists(subject.SubjectID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                var links = from l in _context.Sub_SpecLinks
                            select l;

                foreach (var l in links)
                {
                    if (l.SubjectID == id.ToString())
                    {
                        _context.Remove(l);
                    }
                }

                foreach (var specialityId in specialitysId)
                {
                    Sub_SpecLink subSpecLink = new();
                    subSpecLink.SpecialityID = specialityId;
                    subSpecLink.SubjectID = subject.SubjectID.ToString();
                    _context.Sub_SpecLinks.Add(subSpecLink);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentID"] = new SelectList(_context.Departments, "DepartmentID", "Name", subject.DepartmentID);
            return View(subject);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .Include(s => s.Department)
                .FirstOrDefaultAsync(m => m.SubjectID == id);
            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var links = from l in _context.Sub_SpecLinks
                        where l.SubjectID == id.ToString()
                        select l;
            var subject = await _context.Subjects.FindAsync(id);
            foreach (var l in links)
            {
                _context.Remove(l);
            }
            _context.Subjects.Remove(subject);
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

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(d => d.SubjectID == id);

            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, [Bind("SubjectID,DepartmentID,Name,ShortName,Arch")] Subject subject)
        {
            if (id != subject.SubjectID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var themes = _context.Themes.Where(t => t.SubjectID == subject.SubjectID);
                try
                {
                    if (subject.Arch == false)
                    {
                        subject.Arch = true;
                        foreach (var theme in themes)
                        {
                            theme.Arch = true;
                            _context.Update(theme);
                        }
                    }
                    else
                    {
                        subject.Arch = false;
                        foreach (var theme in themes)
                        {
                            theme.Arch = false;
                            _context.Update(theme);
                        }
                    }
                    _context.Update(subject);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    if (!SubjectExists(subject.SubjectID))
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
            return View(subject);
        }

        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.SubjectID == id);
        }
    }
}
