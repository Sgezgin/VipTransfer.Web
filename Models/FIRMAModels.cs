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
        public Int64 AKTIF { get; set; }
        public string BAGLIFIRMAGUID { get; set; }
        public string EMAIL { get; set; }
        public string FIRMAADI { get; set; }
        public Int64 KDVORAN { get; set; }
        public DateTime LISANSBAS { get; set; }
        public DateTime LISANSBIT { get; set; }
        public string LISANSKODU { get; set; }
        public string TELEFON1 { get; set; }
        public string TELEFON2 { get; set; }
        public string TELEFON3 { get; set; }
        public Int64 TIPI { get; set; }
        public string VERGIDAIRESI { get; set; }
        public string VERGINO { get; set; }
    }
}