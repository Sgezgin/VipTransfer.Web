using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VipTransfer.Web.Models
{
    public class REZERVASYONModels
    {
        [Key]
        public string REZGUID { get; set; }
        public string? ALTFIRMA { get; set; }
        public string? ARACGUID { get; set; }
        public string? FIRMAGUID { get; set; }
        public decimal? IPTAL { get; set; }
        public string? IPTALKULL { get; set; }
        public DateTime? IPTALTARIH { get; set; }
        public string KAYITKULL { get; set; }
        public DateTime KAYITTARIH { get; set; }
        public string MUSTERIGUID { get; set; }
        public string NERDEN { get; set; }
        public string NEREYE { get; set; }
        public string REZSAAT { get; set; }
        public DateTime REZTARIH { get; set; }
        public decimal? TAHMINIKM { get; set; }
        public decimal? UCRET { get; set; }
        public string? UCUSNO { get; set; }

        [ForeignKey("ARACGUID")]
        public virtual ARACLARModels ARAC { get; set; }
    }
}