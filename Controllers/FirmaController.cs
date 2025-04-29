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

            // Müşteri bilgilerini getir
            var musteriGuidListesi = sonRezervasyonlar.Select(r => r.MUSTERIGUID).Distinct().ToList();
            var musteriler = _context.MUSTERI.Where(m => musteriGuidListesi.Contains(m.MUSTERIGUID)).ToList();
            ViewBag.Musteriler = musteriler;

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



        // Firma Listesi
        public IActionResult FirmaListesi(string firmaAdi = null, int? aktif = null)
        {
            // Admin kontrolü
            int? isAdmin = HttpContext.Session.GetInt32("IsAdmin");
            if (isAdmin == null || isAdmin.Value != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Sorguyu oluştur
            var query = _context.FIRMA.AsQueryable();

            // Filtreleme
            if (!string.IsNullOrEmpty(firmaAdi))
                query = query.Where(f => f.FIRMAADI.Contains(firmaAdi));

            if (aktif.HasValue)
                query = query.Where(f => f.AKTIF == aktif.Value);

            // Sonuçları getir
            var firmalar = query.ToList();

            return View(firmalar);
        }

        // Müşteri Listesi
        public IActionResult MusteriListesi(string ad = null, string email = null, string telefon = null)
        {
            // Admin kontrolü
            int? isAdmin = HttpContext.Session.GetInt32("IsAdmin");
            if (isAdmin == null || isAdmin.Value != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Sorguyu oluştur
            var query = _context.MUSTERI.AsQueryable();

            // Filtreleme - NULL kontrolü ile
            if (!string.IsNullOrEmpty(ad))
                query = query.Where(m =>
                    (m.MADISOYADI != null && m.MADISOYADI.Contains(ad)) ||
                    (m.MADI != null && m.MADI.Contains(ad)) ||
                    (m.MSOYADI != null && m.MSOYADI.Contains(ad)));

            if (!string.IsNullOrEmpty(email))
                query = query.Where(m => m.EMAIL != null && m.EMAIL.Contains(email));

            if (!string.IsNullOrEmpty(telefon))
                query = query.Where(m =>
                    (m.TELEFON1 != null && m.TELEFON1.Contains(telefon)) ||
                    (m.TELEFON2 != null && m.TELEFON2.Contains(telefon)));

            // Sonuçları getir
            var musteriler = query.ToList();

            return View(musteriler);
        }
        // Araç Listesi
        public IActionResult AracListesi()
        {
            // Admin kontrolü
            int? isAdmin = HttpContext.Session.GetInt32("IsAdmin");
            if (isAdmin == null || isAdmin.Value != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var araclar = _context.ARACLAR
                .Include(a => a.FIRMA)
                .ToList();
            return View(araclar);
        }



        public IActionResult MusteriDetay(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("MusteriListesi");
            }

            var musteri = _context.MUSTERI.FirstOrDefault(m => m.MUSTERIGUID == id);
            if (musteri == null)
            {
                return NotFound();
            }

            // Müşterinin rezervasyonlarını getir
            var rezervasyonlar = _context.REZERVASYON
                .Where(r => r.MUSTERIGUID == id)
                .OrderByDescending(r => r.REZTARIH)
                .ToList();

            ViewBag.Rezervasyonlar = rezervasyonlar;

            return View(musteri);
        }

        // Yeni Müşteri Ekleme Sayfası
        [HttpGet]
        public IActionResult YeniMusteri()
        {
            return View();
        }

        // Yeni Müşteri Kaydetme
        [HttpPost]
        public IActionResult YeniMusteri(MUSTERIModels musteri)
        {
            ModelState.Remove("MUSTERIGUID");

            if (ModelState.IsValid)
            {
                try
                {
                    musteri.MUSTERIGUID = Guid.NewGuid().ToString();
                    if (!string.IsNullOrEmpty(musteri.MADI) && !string.IsNullOrEmpty(musteri.MSOYADI))
                    {
                        musteri.MADISOYADI = $"{musteri.MADI} {musteri.MSOYADI}";
                    }

                    _context.MUSTERI.Add(musteri);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Müşteri başarıyla eklendi.";
                    return RedirectToAction("MusteriListesi");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Kayıt sırasında bir hata oluştu: {ex.Message}");
                }
            }


            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { x.Key, x.Value.Errors })
                .ToList();

            foreach (var error in errors)
            {
                foreach (var err in error.Errors)
                {
                    ModelState.AddModelError(string.Empty, $"{error.Key}: {err.ErrorMessage}");
                }
            }

            return View(musteri);
        }


        [HttpGet]
        public IActionResult MusteriDuzenle(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("MusteriListesi");
            }

            var musteri = _context.MUSTERI.FirstOrDefault(m => m.MUSTERIGUID == id);
            if (musteri == null)
            {
                return NotFound();
            }

            return View(musteri);
        }


        [HttpPost]
        public IActionResult MusteriDuzenle(MUSTERIModels musteri)
        {
            ModelState.Remove("MUSTERIGUID");
            if (ModelState.IsValid)
            {
                try
                {
                    if (!string.IsNullOrEmpty(musteri.MADI) && !string.IsNullOrEmpty(musteri.MSOYADI))
                    {
                        musteri.MADISOYADI = $"{musteri.MADI} {musteri.MSOYADI}";
                    }

                    _context.Update(musteri);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Müşteri bilgileri başarıyla güncellendi.";
                    return RedirectToAction("MusteriListesi");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Güncelleme sırasında bir hata oluştu: {ex.Message}");
                }
            }

            return View(musteri);
        }


        [HttpGet]
        public IActionResult YeniFirma()
        {
            return View();
        }

        // Yeni Firma Kaydetme
        [HttpPost]
        public IActionResult YeniFirma(FIRMAModels firma)
        {
            ModelState.Remove("FIRMAGUID");

            if (ModelState.IsValid)
            {
                try
                {
                    // Yeni GUID oluştur
                    firma.FIRMAGUID = Guid.NewGuid().ToString();

                    // Lisans tarihleri belirlenmemiş ise varsayılan değerler ata
                    if (firma.LISANSBAS == DateTime.MinValue)
                        firma.LISANSBAS = DateTime.Now;

                    if (firma.LISANSBIT == DateTime.MinValue)
                        firma.LISANSBIT = DateTime.Now.AddYears(1);

                    _context.FIRMA.Add(firma);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Firma başarıyla eklendi.";
                    return RedirectToAction("FirmaListesi");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Kayıt sırasında bir hata oluştu: {ex.Message}");
                }
            }

            return View(firma);
        }

        // Firma Düzenleme Sayfası
        [HttpGet]
        public IActionResult FirmaDuzenle(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("FirmaListesi");
            }

            var firma = _context.FIRMA.FirstOrDefault(f => f.FIRMAGUID == id);
            if (firma == null)
            {
                return NotFound();
            }

            return View(firma);
        }

        // Firma Güncelleme
        [HttpPost]
        public IActionResult FirmaDuzenle(FIRMAModels firma)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(firma);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Firma bilgileri başarıyla güncellendi.";
                    return RedirectToAction("FirmaListesi");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Güncelleme sırasında bir hata oluştu: {ex.Message}");
                }
            }

            return View(firma);
        }


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
            string firmaGuid = HttpContext.Session.GetString("UserGUID");

            if (userType != "Firma" && HttpContext.Session.GetInt32("IsAdmin") != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Firma listesini getir
            List<FIRMAModels> firmalar;
            if (HttpContext.Session.GetInt32("IsAdmin") == 1)
            {
                // Admin tüm firmaları görür
                firmalar = _context.FIRMA.Where(f => f.AKTIF == 1).ToList();
            }
            else
            {
                // Normal kullanıcı sadece kendi firmasını görür
                firmalar = _context.FIRMA.Where(f => f.FIRMAGUID == firmaGuid).ToList();
            }

            ViewBag.Firmalar = firmalar;

            // Araç renkleri
            ViewBag.Renkler = new List<string> {
        "Beyaz", "Siyah", "Gri", "Kırmızı", "Mavi", "Yeşil", "Sarı", "Turuncu",
        "Kahverengi", "Bordo", "Gümüş", "Lacivert", "Diğer"
    };

            // Araç yakıt tipleri
            ViewBag.YakitTipleri = new List<string> {
        "Benzin", "Dizel", "LPG", "Elektrik", "Hibrit", "Benzin+LPG", "Diğer"
    };

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddVehicle(ARACLARModels arac, List<IFormFile> aracFotograflari)
        {
            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            string userType = HttpContext.Session.GetString("UserType");
            string firmaGuid = HttpContext.Session.GetString("UserGUID");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }
            ModelState.Remove("ARACGUID");
            ModelState.Remove("FIRMA");
            // Model geçerliliği kontrolü
            if (ModelState.IsValid)
            {
                try
                {
                    // Admin değilse kendi firma GUID'ini kullan
                    if (HttpContext.Session.GetInt32("IsAdmin") != 1 && userType == "Firma")
                    {
                        arac.FIRMAGUID = firmaGuid;
                    }

                    // Yeni GUID oluştur
                    arac.ARACGUID = Guid.NewGuid().ToString();

                    _context.ARACLAR.Add(arac);
                    await _context.SaveChangesAsync();

                    // Fotoğrafları kaydet
                    if (aracFotograflari != null && aracFotograflari.Count > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "araclar");
                        Directory.CreateDirectory(uploadsFolder); // Klasör yoksa oluştur

                        int siraNo = 1;
                        foreach (var foto in aracFotograflari)
                        {
                            if (foto.Length > 0)
                            {
                                string dosyaAdi = arac.ARACGUID + "_" + siraNo + Path.GetExtension(foto.FileName);
                                string dosyaYolu = Path.Combine(uploadsFolder, dosyaAdi);

                                using (var stream = new FileStream(dosyaYolu, FileMode.Create))
                                {
                                    await foto.CopyToAsync(stream);
                                }

                                // Veritabanına fotoğraf kaydı ekle
                                var aracFoto = new ARACFOTOModels
                                {
                                    FOTOGUID = Guid.NewGuid().ToString(),
                                    ARACGUID = arac.ARACGUID,
                                    FOTOYOLU = "/uploads/araclar/" + dosyaAdi,
                                    SIRANO = siraNo,
                                    EKTARIH = DateTime.Now
                                };

                                _context.ARACFOTO.Add(aracFoto);
                                siraNo++;
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Araç başarıyla eklendi.";
                    return RedirectToAction("Vehicles");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Hata: " + ex.Message);
                }
            }

            // Hata durumunda firma listesini tekrar getir
            List<FIRMAModels> firmalar;
            if (HttpContext.Session.GetInt32("IsAdmin") == 1)
            {
                firmalar = _context.FIRMA.Where(f => f.AKTIF == 1).ToList();
            }
            else
            {
                firmalar = _context.FIRMA.Where(f => f.FIRMAGUID == firmaGuid).ToList();
            }

            ViewBag.Firmalar = firmalar;

            // Araç renkleri
            ViewBag.Renkler = new List<string> {
        "Beyaz", "Siyah", "Gri", "Kırmızı", "Mavi", "Yeşil", "Sarı", "Turuncu",
        "Kahverengi", "Bordo", "Gümüş", "Lacivert", "Diğer"
    };

            // Araç yakıt tipleri
            ViewBag.YakitTipleri = new List<string> {
        "Benzin", "Dizel", "LPG", "Elektrik", "Hibrit", "Benzin+LPG", "Diğer"
    };

            return View(arac);
        }


        // Araç Detayları
        [HttpGet]
        public IActionResult VehicleDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Vehicles");
            }

            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var arac = _context.ARACLAR
                .Include(a => a.FIRMA)
                .FirstOrDefault(a => a.ARACGUID == id);

            if (arac == null)
            {
                return NotFound();
            }

            // Araç fotoğraflarını getir
            var fotograflar = _context.ARACFOTO
                .Where(af => af.ARACGUID == id)
                .OrderBy(af => af.SIRANO)
                .ToList();

            ViewBag.Fotograflar = fotograflar;

            // Rezervasyon istatistiklerini getir
            ViewBag.RezervasyonSayisi = _context.REZERVASYON.Count(r => r.ARACGUID == id);
            ViewBag.BuAyRezervasyonSayisi = _context.REZERVASYON.Count(r =>
                r.ARACGUID == id &&
                r.REZTARIH.Month == DateTime.Now.Month &&
                r.REZTARIH.Year == DateTime.Now.Year);
            ViewBag.IptalRezervasyonSayisi = _context.REZERVASYON.Count(r => r.ARACGUID == id && r.IPTAL == 1);
            ViewBag.ToplamCiro = _context.REZERVASYON
                .Where(r => r.ARACGUID == id && r.IPTAL == 0)
                .Sum(r => r.UCRET);

            return View(arac);
        }

        // Araç Silme İşlemi
        [HttpPost]
        public IActionResult DeleteVehicle(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Vehicles");
            }

            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Aracı getir
            var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == id);
            if (arac == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            bool hasPermission = false;
            if (userType == "Firma" && arac.FIRMAGUID == userGuid)
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

            try
            {
                // Araç fotoğraflarını sil
                var fotograflar = _context.ARACFOTO.Where(af => af.ARACGUID == id).ToList();
                foreach (var foto in fotograflar)
                {
                    // Dosya sisteminden fotoğrafı sil
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", foto.FOTOYOLU.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    _context.ARACFOTO.Remove(foto);
                }

                // Aracı sil
                _context.ARACLAR.Remove(arac);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Araç başarıyla silindi.";
                return RedirectToAction("Vehicles");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Araç silinirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("VehicleDetails", new { id = id });
            }
        }
    }
}