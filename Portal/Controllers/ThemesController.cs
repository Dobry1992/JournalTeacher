using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;

namespace Portal.Controllers
{
    public class ThemesController : Controller
    {
        private readonly AcademyContext _context;

        public ThemesController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index()
        {
            var academyContext = _context.Themes
                .OrderBy(t => t.Arch)
                    .ThenBy(t => t.Name)
                .Include(t => t.Subject);
            return View(await academyContext.ToListAsync());
        }

        public IActionResult CreateTheme(int? id)
        {
            var subjectQuery = from sub in _context.Subjects
                               where sub.SubjectID == id
                               select sub;
            ViewBag.SubjectID = new SelectList(subjectQuery, "SubjectID", "Name");
            ViewBag.ID = id;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTheme(int id, [Bind("ThemeID,SubjectID,Name,Time,ShortName")] Theme theme)
        {
            if (ModelState.IsValid)
            {
                _context.Add(theme);
                await _context.SaveChangesAsync();
                return RedirectToAction("ChooseSubject", "Subjects", new { id });
            }
            var subjectsQuery = from sub in _context.Subjects
                                orderby sub.Name
                                select sub;
            ViewBag.SubjectID = new SelectList(subjectsQuery, "SubjectID", "Name", theme.SubjectID);
            ViewBag.ID = id;
            return View(theme);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public IActionResult Create(int? id)
        {
            if (id != null)
            {
                var subjectQuery = from sub in _context.Subjects
                                   where sub.SubjectID == id
                                   select sub;
                ViewBag.SubjectID = new SelectList(subjectQuery, "SubjectID", "Name");
            }
            else
            {
                var subjectsQuery = from sub in _context.Subjects
                                    orderby sub.Name
                                    select sub;
                ViewBag.SubjectID = new SelectList(subjectsQuery, "SubjectID", "Name");
            }

            return View();
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ThemeID,SubjectID,Name,Time,ShortName")] Theme theme)
        {
            if (ModelState.IsValid)
            {
                _context.Add(theme);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            var subjectsQuery = from sub in _context.Subjects
                                orderby sub.Name
                                select sub;
            ViewBag.SubjectID = new SelectList(subjectsQuery, "SubjectID", "Name", theme.SubjectID);
            return View(theme);
        }

        public async Task<IActionResult> EditTheme(int? id, int? SubjectID)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theme = await _context.Themes.FindAsync(id);
            if (theme == null)
            {
                return NotFound();
            }
            ViewBag.ID = SubjectID;
            ViewData["SubjectID"] = new SelectList(_context.Subjects.Where(s => s.SubjectID == SubjectID), "SubjectID", "Name", theme.SubjectID);
            return View(theme);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTheme(int id, int SubjectID, [Bind("ThemeID,SubjectID,Name,Time,ShortName,Arch")] Theme theme)
        {
            if (id != theme.ThemeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(theme);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThemeExists(theme.ThemeID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("ChooseSubject", "Subjects", new { id = SubjectID });
            }
            ViewBag.ID = SubjectID;
            ViewData["SubjectID"] = new SelectList(_context.Subjects.Where(s => s.SubjectID == SubjectID), "SubjectID", "Name", theme.SubjectID);
            return View(theme);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theme = await _context.Themes.FindAsync(id);
            if (theme == null)
            {
                return NotFound();
            }
            ViewData["SubjectID"] = new SelectList(_context.Subjects, "SubjectID", "Name", theme.SubjectID);
            return View(theme);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ThemeID,SubjectID,Name,Time,ShortName,Arch")] Theme theme)
        {
            if (id != theme.ThemeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(theme);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThemeExists(theme.ThemeID))
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
            ViewData["SubjectID"] = new SelectList(_context.Subjects, "SubjectID", "Name", theme.SubjectID);
            return View(theme);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theme = await _context.Themes
                .Include(t => t.Subject)
                .FirstOrDefaultAsync(m => m.ThemeID == id);
            if (theme == null)
            {
                return NotFound();
            }

            return View(theme);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var theme = await _context.Themes.FindAsync(id);
            _context.Themes.Remove(theme);
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

            var theme = await _context.Themes
                .FirstOrDefaultAsync(d => d.ThemeID == id);

            if (theme == null)
            {
                return NotFound();
            }

            return View(theme);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, [Bind("ThemeID,SubjectID,Name,Time,ShortName,Arch")] Theme theme)
        {
            if (id != theme.ThemeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (theme.Arch == true)
                    {
                        theme.Arch = false;
                    }
                    else
                    {
                        theme.Arch = true;
                    }
                    _context.Update(theme);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    if (!ThemeExists(theme.ThemeID))
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
            return View(theme);
        }

        private bool ThemeExists(int id)
        {
            return _context.Themes.Any(e => e.ThemeID == id);
        }
    }
}