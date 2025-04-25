using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VipTransfer.Web.Models.ViewModels
{
   public class RegisterViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir")]
        [StringLength(100, ErrorMessage = "Şifre en az {2} karakter uzunluğunda olmalıdır", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Ad gereklidir")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad gereklidir")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Telefon numarası gereklidir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string Phone { get; set; }

        // Kullanıcı türü (0: Müşteri, 1: Firma)
        public int UserType { get; set; }
    }
}