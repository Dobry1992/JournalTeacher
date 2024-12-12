using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models.Elective;

namespace Portal.Controllers
{
    public class ElectiveThemesController : Controller
    {
        private readonly AcademyContext _context;

        public ElectiveThemesController(AcademyContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Index(int id)
        {
            var electiveThemes = _context.ElectiveThemes
                .Where(e => e.ElectiveID == id)
                .Include(e => e.Elective);
            ViewBag.ElectiveID = id;

            return View(await electiveThemes.ToListAsync());
        }

        public IActionResult Create(int? id)
        {
            ViewData["ElectiveID"] = new SelectList(_context.Electives.Where(e => e.ElectiveID == id), "ElectiveID", "Name");
            ViewBag.Id = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ElectiveThemeID,Name,ShortName,Archive,ElectiveID")] ElectiveTheme electiveTheme, int id)
        {
            if (ModelState.IsValid)
            {
                _context.Add(electiveTheme);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Electives", new { id });
            }
            ViewBag.Id = id;
            ViewData["ElectiveID"] = new SelectList(_context.Electives.Where(e => e.ElectiveID == id), "ElectiveID", "Name", electiveTheme.ElectiveID);
            return View(electiveTheme);
        }

        public async Task<IActionResult> Edit(int? id, int? electiveID)
        {
            if (id == null)
            {
                return NotFound();
            }

            var electiveTheme = await _context.ElectiveThemes.FindAsync(id);
            if (electiveTheme == null)
            {
                return NotFound();
            }
            ViewData["ElectiveID"] = new SelectList(_context.Electives, "ElectiveID", "Name", electiveTheme.ElectiveID);
            return View(electiveTheme);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int electiveID, [Bind("ElectiveThemeID,Name,ShortName,Archive,ElectiveID")] ElectiveTheme electiveTheme)
        {
            if (id != electiveTheme.ElectiveThemeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(electiveTheme);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ElectiveThemeExists(electiveTheme.ElectiveThemeID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Electives", new { id = electiveID });
            }
            ViewData["ElectiveID"] = new SelectList(_context.Electives, "ElectiveID", "Name", electiveTheme.ElectiveID);
            return View(electiveTheme);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var electiveTheme = await _context.ElectiveThemes
                .Include(e => e.Elective)
                .FirstOrDefaultAsync(m => m.ElectiveThemeID == id);
            if (electiveTheme == null)
            {
                return NotFound();
            }

            return View(electiveTheme);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var electiveTheme = await _context.ElectiveThemes.FindAsync(id);
            _context.ElectiveThemes.Remove(electiveTheme);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "ElectiveThemes", new { id = electiveTheme.ElectiveID });
        }

        private bool ElectiveThemeExists(int id)
        {
            return _context.ElectiveThemes.Any(e => e.ElectiveThemeID == id);
        }
    }
}
