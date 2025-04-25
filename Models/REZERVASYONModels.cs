using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VipTransfer.Web.Models
{
    public class REZERVASYONModels
    {
        [Key]
        public string REZGUID { get; set; }
        public string ALTFIRMA { get; set; }
        public string ARACGUID { get; set; }
        public string FIRMAGUID { get; set; }
        public Int64 IPTAL { get; set; }
        public string IPTALKULL { get; set; }
        public DateTime IPTALTARIH { get; set; }
        public string KAYITKULL { get; set; }
        public DateTime KAYITTARIH { get; set; }
        public string MUSTERIGUID { get; set; }
        public string NERDEN { get; set; }
        public string NEREYE { get; set; }
        public string REZSAAT { get; set; }
        public DateTime REZTARIH { get; set; }
        public double TAHMINIKM { get; set; }
        public double UCRET { get; set; }
        public string UCUSNO { get; set; }
    }
}