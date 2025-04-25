using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VipTransfer.Web.Data;
using VipTransfer.Web.Models;

namespace VipTransfer.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
        {
            return RedirectToAction("Login", "Account");
        }

        // Ana sayfada gösterilecek popüler firmaları getir
        var popularFirmalar = _context.FIRMA.Where(f => f.AKTIF == 1).Take(5).ToList();

        return View(popularFirmalar);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}