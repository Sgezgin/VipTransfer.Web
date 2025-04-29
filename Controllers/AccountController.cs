using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using VipTransfer.Web.Data;
using VipTransfer.Web.Helper;
using VipTransfer.Web.Models;
using VipTransfer.Web.Models.ViewModels;

namespace VipTransfer.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Login sayfasını göster
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Login işlemini gerçekleştir
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Şifreyi hashle
                //string hashedPassword = HashPassword(model.Password);

                try
                {
                    string hashedPassword = "";
                    using (PasswordCryptography psw = new PasswordCryptography())
                    {
                        hashedPassword = psw.TextSifrele(model.Password.Replace("==", ""));

                    }
                    // Kullanıcıyı kontrol et
                    var user = _context.KULLANICI
                        .FirstOrDefault(u => u.KADI == model.Username && u.KSIFRE == hashedPassword);

                    if (user != null)
                    {
                        // Login başarılı, oturum bilgilerini tut
                        HttpContext.Session.SetString("Username", user.KADI);
                        HttpContext.Session.SetInt32("IsAdmin", (int)user.ADMIN);

                        if (!string.IsNullOrEmpty(user.MUSTERIGUID))
                        {
                            HttpContext.Session.SetString("UserType", "Musteri");
                            HttpContext.Session.SetString("UserGUID", user.MUSTERIGUID);
                        }
                        else if (!string.IsNullOrEmpty(user.FIRMAGUID))
                        {
                            HttpContext.Session.SetString("UserType", "Firma");
                            HttpContext.Session.SetString("UserGUID", user.FIRMAGUID);
                        }

                        // Login log kaydını oluştur
                        LOGINLOGModels log = new LOGINLOGModels
                        {
                            LLGUID = Guid.NewGuid().ToString(),
                            KULLADI = user.KADI,
                            PCADI = Request.Host.Value,
                            TARIHKISA = DateTime.Now.Date,
                            TARIHUZUN = DateTime.Now
                        };
                        _context.LOGINLOG.Add(log);
                        _context.SaveChanges();

                        // Kullanıcı tipine göre yönlendir
                        if (user.ADMIN == 1)
                        {
                            return RedirectToAction("Dashboard", "Firma");
                        }
                        else if (!string.IsNullOrEmpty(user.FIRMAGUID))
                        {
                            return RedirectToAction("Reservations", "Firma");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        // Hatalı giriş
                        ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı");

                        // Hatalı giriş log kaydı
                        LOGINLOGModels log = new LOGINLOGModels
                        {
                            LLGUID = Guid.NewGuid().ToString(),
                            KULLADI = model.Username,
                            PCADI = Request.Host.Value,
                            HATA = "Kullanıcı adı veya şifre hatalı",
                            TARIHKISA = DateTime.Now.Date,
                            TARIHUZUN = DateTime.Now
                        };
                        _context.LOGINLOG.Add(log);
                        _context.SaveChanges();
                    }

                }
                catch (Exception ex)
                {
                    // Hata bilgilerini detaylı bir şekilde yazdırın
                    Console.WriteLine($"Hata Türü: {ex.GetType().Name}");
                    Console.WriteLine($"Hata Mesajı: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                    // İç içe hatalar varsa onları da yazdırın
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"İç Hata: {ex.InnerException.Message}");
                    }

                    // Veya loglama sisteminize yazabilirsiniz
                    //_logger.LogError(ex, "Kullanıcı girişi sırasında hata oluştu");

                    // Kullanıcıya uygun bir hata mesajı gösterin
                   // return View("Error", new ErrorViewModel { Message = "Kullanıcı girişi sırasında bir hata oluştu." });
                }
            }

            return View(model);
        }

        // Register sayfasını göster
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Register işlemini gerçekleştir
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kullanıcı adının kullanılıp kullanılmadığını kontrol et
                if (_context.KULLANICI.Any(u => u.KADI == model.Username))
                {
                    ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılıyor");
                    return View(model);
                }

                // E-posta adresinin kullanılıp kullanılmadığını kontrol et
                if (_context.MUSTERI.Any(m => m.EMAIL == model.Email))
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor");
                    return View(model);
                }

                // Yeni kullanıcı oluştur
                string userGuid = Guid.NewGuid().ToString();
                string firmaGuid = null;

                string sifre = "";

                using (PasswordCryptography psw = new PasswordCryptography())
                {
                    sifre = psw.TextSifrele(model.Password.Replace("==", ""));

                }

                if (model.UserType == 0) // Müşteri
                {
                    // Müşteri kaydı oluştur
                    var musteri = new MUSTERIModels
                    {
                        MUSTERIGUID = userGuid,
                        EMAIL = model.Email,
                        MADI = model.FirstName,
                        MSOYADI = model.LastName,
                        MADISOYADI = $"{model.FirstName} {model.LastName}",
                        TELEFON1 = model.Phone
                    };
                    _context.MUSTERI.Add(musteri);
                }
                else // Firma
                {
                    // Firma kaydı oluştur
                    firmaGuid = Guid.NewGuid().ToString();
                    var firma = new FIRMAModels
                    {
                        FIRMAGUID = firmaGuid,
                        FIRMAADI = $"{model.FirstName} {model.LastName}",
                        EMAIL = model.Email,
                        TELEFON1 = model.Phone,
                        AKTIF = 1,
                        TIPI = 1,
                        LISANSBAS = DateTime.Now,
                        LISANSBIT = DateTime.Now.AddYears(1)
                    };
                    _context.FIRMA.Add(firma);
                }

                // Kullanıcı kaydı oluştur
                var kullanici = new KULLANICIModels
                {
                    KADI = model.Username,
                    KSIFRE = sifre,// HashPassword(model.Password),
                    ADMIN = 0,
                    MUSTERIGUID = model.UserType == 0 ? userGuid : null,
                    FIRMAGUID = model.UserType == 1 ? firmaGuid : null
                };
                _context.KULLANICI.Add(kullanici);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }

            return View(model);
        }

        // Çıkış yap
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Şifre hashleme metodu
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}