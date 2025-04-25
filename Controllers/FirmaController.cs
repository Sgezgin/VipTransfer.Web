using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VipTransfer.Web.Data;
using VipTransfer.Web.Models;

namespace VipTransfer.Web.Controllers
{
 
    public class FirmaController : Controller
    {
         private readonly ApplicationDbContext _context;

        public FirmaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Firma dashboard
        public IActionResult Dashboard()
        {
            // Admin kontrolü
            int? isAdmin = HttpContext.Session.GetInt32("IsAdmin");
            if (isAdmin == null || isAdmin.Value != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // İstatistikleri hazırla
            ViewBag.FirmaSayisi = _context.FIRMA.Count();
            ViewBag.MusteriSayisi = _context.MUSTERI.Count();
            ViewBag.AracSayisi = _context.ARACLAR.Count();
            ViewBag.RezervasyonSayisi = _context.REZERVASYON.Count();
            
            // Son 10 rezervasyonu getir
            var sonRezervasyonlar = _context.REZERVASYON
                .OrderByDescending(r => r.KAYITTARIH)
                .Take(10)
                .ToList();

            return View(sonRezervasyonlar);
        }

        // Firma rezervasyonları
        public IActionResult Reservations()
        {
            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Firma kontrolü
            string userType = HttpContext.Session.GetString("UserType");
            string firmaGuid = HttpContext.Session.GetString("UserGUID");
            
            if (userType != "Firma" && HttpContext.Session.GetInt32("IsAdmin") != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Admin ise tüm rezervasyonları getir, değilse firma rezervasyonlarını getir
            var rezervasyonlar = HttpContext.Session.GetInt32("IsAdmin") == 1
                ? _context.REZERVASYON.OrderByDescending(r => r.REZTARIH).ToList()
                : _context.REZERVASYON.Where(r => r.FIRMAGUID == firmaGuid).OrderByDescending(r => r.REZTARIH).ToList();

            // Müşteri ve araç bilgilerini al
            var musteriGuidList = rezervasyonlar.Select(r => r.MUSTERIGUID).Distinct().ToList();
            var aracGuidList = rezervasyonlar.Select(r => r.ARACGUID).Distinct().ToList();

            var musteriler = _context.MUSTERI.Where(m => musteriGuidList.Contains(m.MUSTERIGUID)).ToList();
            var araclar = _context.ARACLAR.Where(a => aracGuidList.Contains(a.ARACGUID)).ToList();

            // ViewBag ile gönder
            ViewBag.Musteriler = musteriler;
            ViewBag.Araclar = araclar;

            return View(rezervasyonlar);
        }

        // Rezervasyon onayla
        [HttpPost]
        public IActionResult ApproveReservation(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Reservations");
            }

            // Rezervasyonu getir
            var rezervasyon = _context.REZERVASYON.FirstOrDefault(r => r.REZGUID == id);
            if (rezervasyon == null)
            {
                return NotFound();
            }

            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");

            // Yetki kontrolü
            bool hasPermission = false;
            if (userType == "Firma" && rezervasyon.FIRMAGUID == userGuid)
            {
                hasPermission = true;
            }
            else if (HttpContext.Session.GetInt32("IsAdmin") == 1)
            {
                hasPermission = true;
            }

            if (!hasPermission)
            {
                return Unauthorized();
            }

            // Onay işlemi (burada gerçek onay işlemi olacak)
            // Örnek olarak onay bilgisini ALTFIRMA alanına kaydedelim
            rezervasyon.ALTFIRMA = "ONAYLANDI-" + username;
            
            _context.Update(rezervasyon);
            _context.SaveChanges();

            return RedirectToAction("Reservations");
        }

        // Firma araç yönetimi
        public IActionResult Vehicles()
        {
            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Firma kontrolü
            string userType = HttpContext.Session.GetString("UserType");
            string firmaGuid = HttpContext.Session.GetString("UserGUID");
            
            if (userType != "Firma" && HttpContext.Session.GetInt32("IsAdmin") != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Araçları getir
            var araclar = HttpContext.Session.GetInt32("IsAdmin") == 1
                ? _context.ARACLAR.ToList()
                : _context.ARACLAR.Where(a => a.FIRMAGUID == firmaGuid).ToList();

            return View(araclar);
        }

        // Yeni araç ekleme sayfası
        [HttpGet]
        public IActionResult AddVehicle()
        {
            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Firma kontrolü
            string userType = HttpContext.Session.GetString("UserType");
            
            if (userType != "Firma" && HttpContext.Session.GetInt32("IsAdmin") != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // Yeni araç ekleme işlemi
        [HttpPost]
        public IActionResult AddVehicle(ARACLARModels arac)
        {
            if (ModelState.IsValid)
            {
                // Oturum kontrolü
                string username = HttpContext.Session.GetString("Username");
                string userType = HttpContext.Session.GetString("UserType");
                string firmaGuid = HttpContext.Session.GetString("UserGUID");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Firma GUID'i belirle
                if (userType == "Firma")
                {
                    arac.FIRMAGUID = firmaGuid;
                }
                else if (HttpContext.Session.GetInt32("IsAdmin") == 1 && string.IsNullOrEmpty(arac.FIRMAGUID))
                {
                    // Admin için firma seçimi gerekli
                    ModelState.AddModelError("FIRMAGUID", "Firma seçimi zorunludur");
                    return View(arac);
                }

                // Yeni araç ekle
                arac.ARACGUID = Guid.NewGuid().ToString();
                
                _context.ARACLAR.Add(arac);
                _context.SaveChanges();

                return RedirectToAction("Vehicles");
            }

            return View(arac);
        }

        // Araç düzenleme sayfası
        [HttpGet]
        public IActionResult EditVehicle(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Vehicles");
            }

            // Aracı getir
            var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == id);
            if (arac == null)
            {
                return NotFound();
            }

            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            string userType = HttpContext.Session.GetString("UserType");
            string firmaGuid = HttpContext.Session.GetString("UserGUID");

            // Yetki kontrolü
            bool hasPermission = false;
            if (userType == "Firma" && arac.FIRMAGUID == firmaGuid)
            {
                hasPermission = true;
            }
            else if (HttpContext.Session.GetInt32("IsAdmin") == 1)
            {
                hasPermission = true;
            }

            if (!hasPermission)
            {
                return Unauthorized();
            }

            return View(arac);
        }

        // Araç düzenleme işlemi
        [HttpPost]
        public IActionResult EditVehicle(ARACLARModels arac)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(arac);
                    _context.SaveChanges();
                    return RedirectToAction("Vehicles");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ARACLAR.Any(a => a.ARACGUID == arac.ARACGUID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(arac);
        }

        // Araç silme işlemi
        [HttpPost]
        public IActionResult DeleteVehicle(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Vehicles");
            }

            // Aracı getir
            var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == id);
            if (arac == null)
            {
                return NotFound();
            }

            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            string userType = HttpContext.Session.GetString("UserType");
            string firmaGuid = HttpContext.Session.GetString("UserGUID");

            // Yetki kontrolü
            bool hasPermission = false;
            if (userType == "Firma" && arac.FIRMAGUID == firmaGuid)
            {
                hasPermission = true;
            }
            else if (HttpContext.Session.GetInt32("IsAdmin") == 1)
            {
                hasPermission = true;
            }

            if (!hasPermission)
            {
                return Unauthorized();
            }

            // Aracı sil
            _context.ARACLAR.Remove(arac);
            _context.SaveChanges();

            return RedirectToAction("Vehicles");
        }
    }
}