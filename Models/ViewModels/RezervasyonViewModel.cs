using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VipTransfer.Web.Models.ViewModels
{
    public class RezervasyonViewModel
    {
        public string REZGUID { get; set; }

        [Required(ErrorMessage = "Nereden bilgisi gereklidir")]
        public string Nereden { get; set; }

        [Required(ErrorMessage = "Nereye bilgisi gereklidir")]
        public string Nereye { get; set; }

        [Required(ErrorMessage = "Tarih seçimi gereklidir")]
        [DataType(DataType.Date)]
        public DateTime RezervasyonTarihi { get; set; }

        [Required(ErrorMessage = "Saat seçimi gereklidir")]
        public string RezervasyonSaati { get; set; }

        public string UcusNo { get; set; }

        public double TahminiKm { get; set; }
        public double Ucret { get; set; }

        // Araç seçimi için
        public string SeciliAracGUID { get; set; }
        public List<ARACLARModels> AracListesi { get; set; }

        // Firma seçimi için (admin kullanıcısı için)
        public string SeciliFirmaGUID { get; set; }
        public List<FIRMAModels> FirmaListesi { get; set; }
    }
}