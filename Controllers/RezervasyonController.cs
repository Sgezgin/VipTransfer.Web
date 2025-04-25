using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VipTransfer.Web.Data;
using VipTransfer.Web.Models;
using VipTransfer.Web.Models.ViewModels;

namespace VipTransfer.Web.Controllers
{

    public class RezervasyonController : Controller
    {
       
        private readonly ApplicationDbContext _context;

        public RezervasyonController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Rezervasyon listesi
        public IActionResult List()
        {
            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Kullanıcı türüne göre rezervasyonları getir
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");
            var rezervasyonlar = new List<REZERVASYONModels>();

            if (userType == "Musteri")
            {
                rezervasyonlar = _context.REZERVASYON
                    .Where(r => r.MUSTERIGUID == userGuid)
                    .OrderByDescending(r => r.REZTARIH)
                    .ToList();
            }
            else if (userType == "Firma")
            {
                rezervasyonlar = _context.REZERVASYON
                    .Where(r => r.FIRMAGUID == userGuid)
                    .OrderByDescending(r => r.REZTARIH)
                    .ToList();
            }
            else
            {
                // Admin ise tüm rezervasyonları göster
                int? isAdmin = HttpContext.Session.GetInt32("IsAdmin");
                if (isAdmin.HasValue && isAdmin.Value == 1)
                {
                    rezervasyonlar = _context.REZERVASYON
                        .OrderByDescending(r => r.REZTARIH)
                        .ToList();
                }
            }

            return View(rezervasyonlar);
        }

        // Yeni rezervasyon sayfası
        [HttpGet]
        public IActionResult Create()
        {
            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Araç listesini getir
            var araclar = _context.ARACLAR.ToList();
            var model = new RezervasyonViewModel
            {
                RezervasyonTarihi = DateTime.Now.AddDays(1),
                AracListesi = araclar
            };

            return View(model);
        }

        // Yeni rezervasyon oluştur
        [HttpPost]
        public IActionResult Create(RezervasyonViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Oturum kontrolü
                string username = HttpContext.Session.GetString("Username");
                string userGuid = HttpContext.Session.GetString("UserGUID");
                string userType = HttpContext.Session.GetString("UserType");

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userGuid))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Seçilen araç bilgilerini getir
                var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == model.SeciliAracGUID);
                if (arac == null)
                {
                    ModelState.AddModelError("SeciliAracGUID", "Lütfen bir araç seçiniz");
                    model.AracListesi = _context.ARACLAR.ToList();
                    return View(model);
                }

                // Firma bilgisini getir
                string firmaGuid = arac.FIRMAGUID;

                // Yeni rezervasyon oluştur
                var rezervasyon = new REZERVASYONModels
                {
                    REZGUID = Guid.NewGuid().ToString(),
                    ARACGUID = model.SeciliAracGUID,
                    FIRMAGUID = firmaGuid,
                    MUSTERIGUID = userType == "Musteri" ? userGuid : null,
                    NERDEN = model.Nereden,
                    NEREYE = model.Nereye,
                    REZTARIH = model.RezervasyonTarihi,
                    REZSAAT = model.RezervasyonSaati,
                    UCUSNO = model.UcusNo,
                    TAHMINIKM = model.TahminiKm,
                    UCRET = model.Ucret,
                    IPTAL = 0,
                    KAYITKULL = username,
                    KAYITTARIH = DateTime.Now
                };

                _context.REZERVASYON.Add(rezervasyon);
                _context.SaveChanges();

                return RedirectToAction("Details", new { id = rezervasyon.REZGUID });
            }

            // Hata durumunda araç listesini tekrar yükle
            model.AracListesi = _context.ARACLAR.ToList();
            return View(model);
        }

        // Rezervasyon detayları
        public IActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("List");
            }

            // Rezervasyon bilgilerini getir
            var rezervasyon = _context.REZERVASYON
                .FirstOrDefault(r => r.REZGUID == id);

            if (rezervasyon == null)
            {
                return NotFound();
            }

            // Araç bilgilerini getir
            var arac = _context.ARACLAR
                .FirstOrDefault(a => a.ARACGUID == rezervasyon.ARACGUID);

            // Müşteri bilgilerini getir
            var musteri = _context.MUSTERI
                .FirstOrDefault(m => m.MUSTERIGUID == rezervasyon.MUSTERIGUID);

            // Firma bilgilerini getir
            var firma = _context.FIRMA
                .FirstOrDefault(f => f.FIRMAGUID == rezervasyon.FIRMAGUID);

            // ViewBag ile diğer bilgileri gönder
            ViewBag.Arac = arac;
            ViewBag.Musteri = musteri;
            ViewBag.Firma = firma;

            return View(rezervasyon);
        }

        // Rezervasyon iptal et
        [HttpPost]
        public IActionResult Cancel(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("List");
            }

            var rezervasyon = _context.REZERVASYON.FirstOrDefault(r => r.REZGUID == id);
            if (rezervasyon == null)
            {
                return NotFound();
            }

            // Oturum bilgilerini kontrol et
            string username = HttpContext.Session.GetString("Username");
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");

            // Kullanıcı türüne göre iptal yetkisi kontrol et
            bool canCancel = false;
            
            if (userType == "Musteri" && rezervasyon.MUSTERIGUID == userGuid)
            {
                canCancel = true;
            }
            else if (userType == "Firma" && rezervasyon.FIRMAGUID == userGuid)
            {
                canCancel = true;
            }
            else
            {
                int? isAdmin = HttpContext.Session.GetInt32("IsAdmin");
                if (isAdmin.HasValue && isAdmin.Value == 1)
                {
                    canCancel = true;
                }
            }

            if (!canCancel)
            {
                return Unauthorized();
            }

            // Rezervasyonu iptal et
            rezervasyon.IPTAL = 1;
            rezervasyon.IPTALKULL = username;
            rezervasyon.IPTALTARIH = DateTime.Now;

            _context.Update(rezervasyon);
            _context.SaveChanges();

            return RedirectToAction("Details", new { id = rezervasyon.REZGUID });
        }

        // Fiyat hesapla (AJAX için)
        [HttpPost]
        public IActionResult CalculatePrice(string nereden, string nereye, string aracGuid)
        {
            if (string.IsNullOrEmpty(nereden) || string.IsNullOrEmpty(nereye) || string.IsNullOrEmpty(aracGuid))
            {
                return Json(new { success = false, message = "Gerekli bilgiler eksik" });
            }

            // Araç bilgilerini getir
            var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == aracGuid);
            if (arac == null)
            {
                return Json(new { success = false, message = "Araç bulunamadı" });
            }

            // Basit bir fiyat hesaplama algoritması (gerçek uygulama daha karmaşık olabilir)
            double mesafe = 0;
            
            // Burada gerçek bir mesafe API'si kullanılabilir
            // Şimdilik basit bir tahmin yapalım
            if (nereden.Contains("Havalimanı") || nereye.Contains("Havalimanı"))
            {
                mesafe = 30; // Havalimanı transferleri için varsayılan mesafe
            }
            else
            {
                // Diğer destinasyonlar için basit bir hesaplama
                Random random = new Random();
                mesafe = random.Next(5, 50);
            }

            // Araç tipine göre km başına ücret belirle
            double kmBasinaUcret = 0;
            switch (arac.TIP)
            {
                case 1: // Ekonomik
                    kmBasinaUcret = 5;
                    break;
                case 2: // VIP
                    kmBasinaUcret = 10;
                    break;
                case 3: // Lüks
                    kmBasinaUcret = 15;
                    break;
                default:
                    kmBasinaUcret = 7;
                    break;
            }

            double toplamUcret = mesafe * kmBasinaUcret;

            return Json(new 
            { 
                success = true, 
                mesafe = mesafe, 
                ucret = toplamUcret 
            });
        }
    }
}