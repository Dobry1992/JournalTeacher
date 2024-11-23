using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portal.Data;
using Portal.Models;
using Portal.Models.Election;
using Portal.Repository;
using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly NewsContext _context;
        private readonly AcademyContext _academyContext;
        private readonly IWebHostEnvironment _appEnvironment;

        public HomeController(ILogger<HomeController> logger, NewsContext context, AcademyContext academyContext, IWebHostEnvironment appEnvironment)
        {
            _logger = logger;
            _context = context;
            _academyContext = academyContext;
            _appEnvironment = appEnvironment;
        }

        public IActionResult Index()
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

            var news = _context.Articles
                .Include(n => n.Images)
                .OrderByDescending(n => n.DateOfNews);
            return View(news);
        }

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
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

        [Authorize(Roles = "SuperAdmin, ANB-UMCH")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ScheduleID,Name")] Schedule schedule, IFormFileCollection files)
        {
            if (ModelState.IsValid)
            {
                var schedules = _academyContext.Schedules;
                foreach (var s in schedules)
                {
                    _academyContext.Remove(s);
                }
                await _academyContext.SaveChangesAsync();

                foreach (var file in files)
                {
                    byte[] fileData = null;
                    using (var binaryReader = new BinaryReader(file.OpenReadStream()))
                    {
                        fileData = binaryReader.ReadBytes((int)file.Length);
                    }
                    schedule.File = fileData;
                    _academyContext.Schedules.Add(schedule);
                }
                await _academyContext.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Route("Расписание")]
        public async Task<FileContentResult> GetSchedule()
        {
            var schedule = await _academyContext.Schedules.FirstOrDefaultAsync();
            return File(schedule.File, "application/pdf");
        }

        public IActionResult IndexElection()
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

            var news = _context.ElectionArticles
                .Include(n => n.Images)
                .OrderByDescending(n => n.Date);
            return View(news);
        }

        [Authorize(Roles = "SuperAdmin, Journalist")]
        public IActionResult CreateElectionArticle()
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
        public async Task<IActionResult> CreateElectionArticle([Bind("ElectionArticleID,Title,Text,Date")] ElectionArticle electionArticle, IFormFileCollection files)
        {
            if (ModelState.IsValid)
            {
                _context.Add(electionArticle);
                await _context.SaveChangesAsync();
                foreach (var file in files)
                {
                    string path = "/images/news/" + file.FileName;
                    using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    ElectionImage img = new()
                    {
                        ElectionArticleID = electionArticle.ElectionArticleID,
                        Title = electionArticle.Title,
                        Path = path
                    };
                    _context.Add(img);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
