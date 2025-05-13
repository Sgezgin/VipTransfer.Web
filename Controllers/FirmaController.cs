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

            // Onaylanan, bekleyen ve iptal rezervasyon sayıları
            ViewBag.OnaylananSayisi = _context.REZERVASYON.Count(r => r.IPTAL == 2);
            ViewBag.BekleyenSayisi = _context.REZERVASYON.Count(r => r.IPTAL == 0); 
            ViewBag.IptalSayisi = _context.REZERVASYON.Count(r => r.IPTAL == 1);

            // Bugün ve yarınki rezervasyon sayıları
            DateTime bugun = DateTime.Today;
            DateTime yarin = bugun.AddDays(1);
            ViewBag.BugunRezervasyonSayisi = _context.REZERVASYON.Count(r => r.REZTARIH.Date == bugun);
            ViewBag.YarinRezervasyonSayisi = _context.REZERVASYON.Count(r => r.REZTARIH.Date == yarin);

            // Son rezervasyonları getir
            var sonRezervasyonlar = _context.REZERVASYON
                .OrderByDescending(r => r.KAYITTARIH)
                .Take(10)  // Daha fazla rezervasyon göster
                .ToList();

            // Müşteri bilgilerini getir
            var musteriGuidListesi = sonRezervasyonlar.Select(r => r.MUSTERIGUID).Distinct().ToList();
            var musteriler = _context.MUSTERI.Where(m => musteriGuidListesi.Contains(m.MUSTERIGUID)).ToList();
            ViewBag.Musteriler = musteriler;

            // Araç bilgilerini getir
            var aracGuidListesi = sonRezervasyonlar.Select(r => r.ARACGUID).Distinct().ToList();
            var araclar = _context.ARACLAR.Where(a => aracGuidListesi.Contains(a.ARACGUID)).ToList();
            ViewBag.Araclar = araclar;

            return View(sonRezervasyonlar);
        }    // Firma dashboard


        // Belirli bir güne ait rezervasyonlar
        public IActionResult DailyReservations(string date)
        {
            // Admin kontrolü
            int? isAdmin = HttpContext.Session.GetInt32("IsAdmin");
            if (isAdmin == null || isAdmin.Value != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Tarih parametresi kontrolü
            DateTime selectedDate;
            if (!DateTime.TryParse(date, out selectedDate))
            {
                // Geçersiz tarih formatı, bugünün tarihini kullan
                selectedDate = DateTime.Today;
            }

            // Seçilen güne ait rezervasyonları getir
            var rezervasyonlar = _context.REZERVASYON
                .Where(r => r.REZTARIH.Date == selectedDate.Date)
                .OrderBy(r => r.REZSAAT)
                .ToList();

            // Müşteri bilgilerini getir
            var musteriGuidListesi = rezervasyonlar.Select(r => r.MUSTERIGUID).Distinct().ToList();
            var musteriler = _context.MUSTERI.Where(m => musteriGuidListesi.Contains(m.MUSTERIGUID)).ToList();
            ViewBag.Musteriler = musteriler;

            // Araç bilgilerini getir
            var aracGuidListesi = rezervasyonlar.Select(r => r.ARACGUID).Distinct().ToList();
            var araclar = _context.ARACLAR.Where(a => aracGuidListesi.Contains(a.ARACGUID)).ToList();
            ViewBag.Araclar = araclar;

            // Seçilen tarihi ve formatlanmış görüntüsünü ViewBag'e ekle
            ViewBag.SelectedDate = selectedDate;
            ViewBag.FormattedDate = selectedDate.ToString("dd MMMM yyyy, dddd");

            return View(rezervasyonlar);
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


        // Araç Düzenleme Sayfası
        [HttpGet]
        public IActionResult EditVehicle(string id)
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

            var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == id);
            if (arac == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");
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

            // Firma listesini getir
            List<FIRMAModels> firmalar;
            if (HttpContext.Session.GetInt32("IsAdmin") == 1)
            {
                firmalar = _context.FIRMA.Where(f => f.AKTIF == 1).ToList();
            }
            else
            {
                firmalar = _context.FIRMA.Where(f => f.FIRMAGUID == userGuid).ToList();
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

        // Araç Güncelleme
        [HttpPost]
        public IActionResult EditVehicle(ARACLARModels arac)
        {
            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Yetki kontrolü
            var existingArac = _context.ARACLAR.AsNoTracking().FirstOrDefault(a => a.ARACGUID == arac.ARACGUID);
            if (existingArac == null)
            {
                return NotFound();
            }

            bool hasPermission = false;
            if (userType == "Firma" && existingArac.FIRMAGUID == userGuid)
            {
                hasPermission = true;
                // Firma kullanıcısı kendi firma GUID'ini değiştiremez
                arac.FIRMAGUID = userGuid;
            }
            else if (HttpContext.Session.GetInt32("IsAdmin") == 1)
            {
                hasPermission = true;
            }

            if (!hasPermission)
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(arac);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Araç bilgileri başarıyla güncellendi.";
                    return RedirectToAction("VehicleDetails", new { id = arac.ARACGUID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Hata: " + ex.Message);
                }
            }

            // Firma listesini tekrar getir
            List<FIRMAModels> firmalar;
            if (HttpContext.Session.GetInt32("IsAdmin") == 1)
            {
                firmalar = _context.FIRMA.Where(f => f.AKTIF == 1).ToList();
            }
            else
            {
                firmalar = _context.FIRMA.Where(f => f.FIRMAGUID == userGuid).ToList();
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

        // Fotoğraf Ekleme Sayfası
        [HttpGet]
        public IActionResult AddPhotos(string id)
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

            var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == id);
            if (arac == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");
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

            // Mevcut fotoğrafları getir
            var fotograflar = _context.ARACFOTO
                .Where(f => f.ARACGUID == id)
                .OrderBy(f => f.SIRANO)
                .ToList();

            ViewBag.Fotograflar = fotograflar;
            return View(arac);
        }

        // Fotoğraf Ekleme İşlemi
        [HttpPost]
        public async Task<IActionResult> AddPhotos(string id, List<IFormFile> Fotograflar, int BaslangicSira = 1)
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

            var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == id);
            if (arac == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");
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

            // Fotoğraf yükleme
            if (Fotograflar != null && Fotograflar.Count > 0)
            {
                try
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "araclar");
                    Directory.CreateDirectory(uploadsFolder); // Klasör yoksa oluştur

                    int siraNo = BaslangicSira;
                    foreach (var foto in Fotograflar)
                    {
                        if (foto.Length > 0)
                        {
                            // Dosya adını oluştur
                            string dosyaAdi = $"{arac.ARACGUID}_{DateTime.Now.Ticks}_{siraNo}{Path.GetExtension(foto.FileName)}";
                            string dosyaYolu = Path.Combine(uploadsFolder, dosyaAdi);

                            // Dosyayı kaydet
                            using (var stream = new FileStream(dosyaYolu, FileMode.Create))
                            {
                                await foto.CopyToAsync(stream);
                            }

                            // Veritabanına kaydet
                            var aracFoto = new ARACFOTOModels
                            {
                                FOTOGUID = Guid.NewGuid().ToString(),
                                ARACGUID = arac.ARACGUID,
                                FOTOYOLU = $"/uploads/araclar/{dosyaAdi}",
                                SIRANO = siraNo,
                                EKTARIH = DateTime.Now
                            };

                            _context.ARACFOTO.Add(aracFoto);
                            siraNo++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Fotoğraflar başarıyla yüklendi.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Fotoğraf yükleme sırasında bir hata oluştu: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Lütfen en az bir fotoğraf seçin.";
            }

            return RedirectToAction("AddPhotos", new { id });
        }

        // Fotoğraf Silme
        [HttpPost]
        public IActionResult DeletePhoto(string id, string aracId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(aracId))
            {
                return RedirectToAction("Vehicles");
            }

            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var foto = _context.ARACFOTO.FirstOrDefault(f => f.FOTOGUID == id);
            if (foto == null)
            {
                return NotFound();
            }

            var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == foto.ARACGUID);
            if (arac == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");
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
                // Dosya sisteminden sil
                if (!string.IsNullOrEmpty(foto.FOTOYOLU))
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", foto.FOTOYOLU.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // Veritabanından sil
                _context.ARACFOTO.Remove(foto);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Fotoğraf başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Fotoğraf silinirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction("AddPhotos", new { id = aracId });
        }

        // Araç Rezervasyonları Sayfası
        [HttpGet]
        public IActionResult VehicleReservations(string id, DateTime? dateFrom = null, DateTime? dateTo = null, string status = null)
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

            var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == id);
            if (arac == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");
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

            // Rezervasyonları getir
            var query = _context.REZERVASYON.Where(r => r.ARACGUID == id);

            // Filtreler
            if (dateFrom.HasValue)
            {
                query = query.Where(r => r.REZTARIH >= dateFrom.Value.Date);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(r => r.REZTARIH <= dateTo.Value.Date.AddDays(1).AddTicks(-1));
            }

            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(r => r.IPTAL == 0 && (string.IsNullOrEmpty(r.ALTFIRMA) || !r.ALTFIRMA.StartsWith("ONAYLANDI")));
                        break;
                    case "approved":
                        query = query.Where(r => r.IPTAL == 0 && !string.IsNullOrEmpty(r.ALTFIRMA) && r.ALTFIRMA.StartsWith("ONAYLANDI"));
                        break;
                    case "canceled":
                        query = query.Where(r => r.IPTAL == 1);
                        break;
                }
            }

            var rezervasyonlar = query.OrderByDescending(r => r.REZTARIH).ToList();

            // Müşteri bilgilerini getir
            if (rezervasyonlar.Any())
            {
                var musteriIdleri = rezervasyonlar.Select(r => r.MUSTERIGUID).Distinct().ToList();
                var musteriler = _context.MUSTERI.Where(m => musteriIdleri.Contains(m.MUSTERIGUID)).ToList();
                ViewBag.Musteriler = musteriler;
            }

            ViewBag.Arac = arac;
            return View(rezervasyonlar);
        }


        [HttpPost]
        public IActionResult CreateCustomerFromModal([FromBody] MUSTERIModels model)
        {
            try
            {
              
                model.MUSTERIGUID = Guid.NewGuid().ToString();
                model.MADISOYADI = $"{model.MADI} {model.MSOYADI}";
                 
                _context.MUSTERI.Add(model);
                _context.SaveChanges();

                return Json(new { success = true, customerId = model.MUSTERIGUID, message = "Müşteri başarıyla oluşturuldu" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }


        // Add this method to your FirmaController.cs file

        [HttpPost]
        public IActionResult CreateReservationFromModal([FromBody] ReservationCreateModel model)
        {
            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Oturum zaman aşımına uğradı. Lütfen tekrar giriş yapın." });
            }

            try
            {
                // Müşteri bilgilerini getir
                var musteri = _context.MUSTERI.FirstOrDefault(m => m.MUSTERIGUID == model.MusteriId);
                if (musteri == null)
                {
                    return Json(new { success = false, message = "Müşteri bulunamadı." });
                }

                // Araç bilgilerini getir
                 string firmaguid = "",aracguid="";
                 if(model.AracId != null)
                     aracguid=model.AracId ;

                var arac = _context.ARACLAR.FirstOrDefault(a => a.ARACGUID == model.AracId);
                if (arac == null)
                {
                     firmaguid = "";                    
                }
                else{
                    firmaguid = arac.FIRMAGUID;
                }

               

                // Tarih kontrolü
                if (!DateTime.TryParse(model.Tarih, out DateTime tarih))
                {
                    return Json(new { success = false, message = "Geçersiz tarih formatı." });
                }

                // Yeni rezervasyon oluştur
                var rezervasyon = new REZERVASYONModels
                {
                    REZGUID = Guid.NewGuid().ToString(),
                    MUSTERIGUID = model.MusteriId,
                    ARACGUID = aracguid,
                    FIRMAGUID =firmaguid,
                    NERDEN = model.Nereden,
                    NEREYE = model.Nereye,
                    REZTARIH = tarih,
                    REZSAAT = model.Saat,
                    UCUSNO = model.UcusNo,
                    UCRET =model.Ucret,
                    TAHMINIKM = 0, // Bu değer hesaplanabilir
                    IPTAL = 0,
                    KAYITKULL = username,
                    KAYITTARIH = DateTime.Now
                };

                // Veritabanına kaydet
                _context.REZERVASYON.Add(rezervasyon);
                _context.SaveChanges();

                return Json(new { success = true, message = "Rezervasyon başarıyla oluşturuldu.", rezervasyonId = rezervasyon.REZGUID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }


        [HttpGet]
        public IActionResult GetCustomers(string searchTerm = "")
        {
            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Oturum zaman aşımına uğradı. Lütfen tekrar giriş yapın." });
            }

            try
            {
                // Sorguyu oluştur
                var query = _context.MUSTERI.AsQueryable();

                // Arama terimi varsa filtreleme yap
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(m =>
                        (m.MADISOYADI != null && m.MADISOYADI.ToLower().Contains(searchTerm)) ||
                        (m.MADI != null && m.MADI.ToLower().Contains(searchTerm)) ||
                        (m.MSOYADI != null && m.MSOYADI.ToLower().Contains(searchTerm)) ||
                        (m.TELEFON1 != null && m.TELEFON1.Contains(searchTerm)) ||
                        (m.TELEFON2 != null && m.TELEFON2.Contains(searchTerm)) ||
                        (m.EMAIL != null && m.EMAIL.ToLower().Contains(searchTerm))
                    );
                }

                // Sıralama ve limitleme
                var musteriler = query
                    .OrderBy(m => m.MADISOYADI)
                    .Take(50) // Çok fazla veri dönmemesi için limit
                    .Select(m => new
                    {
                        id = m.MUSTERIGUID,
                        ad = m.MADISOYADI,
                        telefon = m.TELEFON1,
                        email = m.EMAIL
                    })
                    .ToList();

                return Json(new { success = true, data = musteriler });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }



        [HttpGet]
        public IActionResult GetVehicles()
        {
            // Oturum kontrolü
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Oturum zaman aşımına uğradı. Lütfen tekrar giriş yapın." });
            }

            // Firma kontrolü
            string userType = HttpContext.Session.GetString("UserType");
            string userGuid = HttpContext.Session.GetString("UserGUID");

            try
            {
                // Sorguyu oluştur
                var query = _context.ARACLAR.AsQueryable();

                // Admin değilse sadece kendi firmasının araçlarını getir
                if (userType == "Firma" && HttpContext.Session.GetInt32("IsAdmin") != 1)
                {
                    query = query.Where(a => a.FIRMAGUID == userGuid);
                }

                // Araçları getir
                var araclar = query
                    .Select(a => new
                    {
                        id = a.ARACGUID,
                        marka = a.MARKA,
                        model = a.MODEL,
                        plaka = a.PLAKA,
                        tip = a.TIP,
                        tipAdi = a.TIP == 1 ? "Ekonomik" :
                                 a.TIP == 2 ? "VIP" :
                                 a.TIP == 3 ? "Lüks" : "Diğer",
                        icon = a.TIP == 1 ? "fa-car" :
                               a.TIP == 2 ? "fa-car-side" :
                               a.TIP == 3 ? "fa-shuttle-van" : "fa-bus",
                        bgColor = a.TIP == 1 ? "bg-blue-100" :
                                  a.TIP == 2 ? "bg-green-100" :
                                  a.TIP == 3 ? "bg-purple-100" : "bg-gray-100",
                        textColor = a.TIP == 1 ? "text-blue-600" :
                                    a.TIP == 2 ? "text-green-600" :
                                    a.TIP == 3 ? "text-purple-600" : "text-gray-600"
                    })
                    .OrderBy(a => a.marka)
                    .ToList();

                return Json(new { success = true, data = araclar });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata oluştu: {ex.Message}" });
            }
        }



    }
}

// Rezervasyon model sınıfı
public class ReservationCreateModel
{
    public string MusteriId { get; set; }
    public string Nereden { get; set; }
    public string Nereye { get; set; }
    public string Tarih { get; set; }
    public string Saat { get; set; }
    public string UcusNo { get; set; }
    public string AracId { get; set; }
    public decimal Ucret { get; set; }
}