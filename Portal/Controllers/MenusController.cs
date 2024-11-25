using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Portal.Data;
using Portal.Models.Menu;
using Portal.Repository;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class MenusController : Controller
    {
        private readonly NewsContext _context;
        private readonly IWebHostEnvironment _appEnvironment;

        public MenusController(NewsContext context, IWebHostEnvironment appEnvironment)
        {
            _context = context;
            _appEnvironment = appEnvironment;
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
        public async Task<IActionResult> Create([Bind("MenuID,Title")] Menu menu, IFormFileCollection files)
        {
            if (ModelState.IsValid)
            {
                var menus = _context.Menus;
                foreach (var m in menus)
                {
                    _context.Menus.Remove(m);
                }
                await _context.SaveChangesAsync();

                foreach (var file in files)
                {
                    byte[] fileData = null;
                    using (var binaryReader = new BinaryReader(file.OpenReadStream()))
                    {
                        fileData = binaryReader.ReadBytes((int)file.Length);
                    }
                    menu.File = fileData;
                    _context.Menus.Add(menu);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
