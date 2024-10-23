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

namespace Portal.Controllers
{
    public class SpecialitiesController : Controller
    {
        private readonly AcademyContext _context;

        public SpecialitiesController(AcademyContext context)
        {
            _context = context;
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

        private bool SpecialityExists(int id)
        {
            return _context.Specialities.Any(e => e.SpecialityID == id);
        }
    }
}
