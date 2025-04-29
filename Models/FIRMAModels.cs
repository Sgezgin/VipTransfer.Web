using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VipTransfer.Web.Models
{
    public class FIRMAModels
    {
        [Key]
        public string FIRMAGUID { get; set; }
        public decimal AKTIF { get; set; }
        public string? BAGLIFIRMAGUID { get; set; }

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string EMAIL { get; set; }

        [Required(ErrorMessage = "Firma Adı adresi gereklidir")]
        public string FIRMAADI { get; set; }
        public decimal KDVORAN { get; set; }
        public DateTime LISANSBAS { get; set; }
        public DateTime LISANSBIT { get; set; }
        public string LISANSKODU { get; set; }
        public string? TELEFON1 { get; set; }
        public string? TELEFON2 { get; set; }
        public string? TELEFON3 { get; set; }
        public decimal TIPI { get; set; }

          [Required(ErrorMessage = "Vergi Dairesi gereklidir")]
        public string VERGIDAIRESI { get; set; }

          [Required(ErrorMessage = "Vergi Numarası gereklidir")]
        public string VERGINO { get; set; }
    }
}