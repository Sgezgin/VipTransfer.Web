using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VipTransfer.Web.Models
{
   public class LOGINLOGModels
    {
        [Key]
        public string LLGUID { get; set; }
        public string HATA { get; set; }
        public string KULLADI { get; set; }
        public string PCADI { get; set; }
        public DateTime TARIHKISA { get; set; }
        public DateTime TARIHUZUN { get; set; }
    }
}