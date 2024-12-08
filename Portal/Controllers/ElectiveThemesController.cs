using System.Linq;
using System.Threading.Tasks;
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

        public async Task<IActionResult> Index()
        {
            var academyContext = _context.ElectiveThemes.Include(e => e.Elective);
            return View(await academyContext.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewData["ElectiveID"] = new SelectList(_context.Electives, "ElectiveID", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ElectiveThemeID,Name,ShortName,Archive,ElectiveID")] ElectiveTheme electiveTheme)
        {
            if (ModelState.IsValid)
            {
                _context.Add(electiveTheme);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ElectiveID"] = new SelectList(_context.Electives, "ElectiveID", "Name", electiveTheme.ElectiveID);
            return View(electiveTheme);
        }

        public async Task<IActionResult> Edit(int? id)
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
        public async Task<IActionResult> Edit(int id, [Bind("ElectiveThemeID,Name,ShortName,Archive,ElectiveID")] ElectiveTheme electiveTheme)
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["ElectiveID"] = new SelectList(_context.Electives, "ElectiveID", "Name", electiveTheme.ElectiveID);
            return View(electiveTheme);
        }

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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var electiveTheme = await _context.ElectiveThemes.FindAsync(id);
            _context.ElectiveThemes.Remove(electiveTheme);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ElectiveThemeExists(int id)
        {
            return _context.ElectiveThemes.Any(e => e.ElectiveThemeID == id);
        }
    }
}
