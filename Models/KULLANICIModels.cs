using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VipTransfer.Web.Models
{
   public class KULLANICIModels
    {
        [Key]
        public string KADI { get; set; }
        public decimal ADMIN { get; set; }
        public string? FIRMAGUID { get; set; }
        public string KSIFRE { get; set; }
        public string? MUSTERIGUID { get; set; }
    }
}