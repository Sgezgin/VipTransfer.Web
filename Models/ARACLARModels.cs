using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VipTransfer.Web.Models
{
  public class ARACLARModels
    {
        [Key]
        public string ARACGUID { get; set; }
        public string FIRMAGUID { get; set; }
        public string MARKA { get; set; }
        public string MODEL { get; set; }
        public string PLAKA { get; set; }
        public string SERI { get; set; }
        public Int64 TIP { get; set; }
    }
}