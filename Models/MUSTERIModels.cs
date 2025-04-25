using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VipTransfer.Web.Models
{
     public class MUSTERIModels
    {
        [Key]
        public string MUSTERIGUID { get; set; }
        public string EMAIL { get; set; }
        public string FIRMAGUID { get; set; }
        public string MADI { get; set; }
        public string MADISOYADI { get; set; }
        public string MADRES { get; set; }
        public string MFATURAADRES { get; set; }
        public Int64 MFATURALI { get; set; }
        public string MFIRMAADI { get; set; }
        public string MSOYADI { get; set; }
        public string MVERGIDAIRESI { get; set; }
        public Int64 MVERGINO { get; set; }
        public string TELEFON1 { get; set; }
        public string TELEFON2 { get; set; }
    }
}