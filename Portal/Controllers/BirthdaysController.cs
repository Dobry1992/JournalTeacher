using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models.Birthday;
using Portal.Repository;
using System;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class BirthdaysController : Controller
    {
        private readonly NewsContext _context;
        private readonly IWebHostEnvironment _appEnvironment;
        public BirthdaysController(NewsContext context, IWebHostEnvironment appEnvironment)
        {
            _context = context;
            _appEnvironment = appEnvironment;
        }
        public IActionResult Index()
        {
            return View(_context.Birthdays);
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        public IActionResult Create()
        {
            var username = User.Identity.Name;
            using (var context = new PrincipalContext(ContextType.Domain, AD.root))
            {
                try
                {
                    var user = UserPrincipal.FindByIdentity(context, username);

                    if (user != null)
                    {
                        ViewBag.FullName = user.DisplayName;
                    }
                }
                catch
                {
                    ViewBag.FullName = username;
                }
            }
            return View();
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BirthdayID,Title,Path,Date,DateBirth")] Birthday birthday, IFormFileCollection files)
        {
            if (ModelState.IsValid)
            {
                foreach (var file in files)
                {
                    string path = "/images/birthdays/" + file.FileName;
                    using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    birthday.Path = path;
                    birthday.Date = DateTime.Now;
                    _context.Add(birthday);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Birthdays
                .FirstOrDefaultAsync(m => m.BirthdayID == id);
            if (article == null)
            {
                return NotFound();
            }

            var username = User.Identity.Name;
            using (var context = new PrincipalContext(ContextType.Domain, AD.root))
            {
                try
                {
                    var user = UserPrincipal.FindByIdentity(context, username);

                    if (user != null)
                    {
                        ViewBag.FullName = user.DisplayName;
                    }
                }
                catch
                {
                    ViewBag.FullName = username;
                }
            }

            return View(article);
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var article = await _context.Birthdays.FindAsync(id);
            _context.Birthdays.Remove(article);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
