using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace VipTransfer.Web.Models
{
    public class ARACFOTOModels
    {
        [Key]
        public string FOTOGUID { get; set; }
        public string ARACGUID { get; set; }
        public string FOTOYOLU { get; set; }
        public int SIRANO { get; set; }
        public DateTime EKTARIH { get; set; }

        [ForeignKey("ARACGUID")]
        public virtual ARACLARModels ARAC { get; set; }
    }
}