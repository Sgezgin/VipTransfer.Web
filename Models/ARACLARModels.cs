using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    public decimal TIP { get; set; }

    public string? RENK { get; set; }
    public int? YILI { get; set; }
    public string? YAKITTURU { get; set; }
    public int? KOLTUKSAYISI { get; set; }
    public int? BAGAJKAPASITESI { get; set; }
    public string? OZELNITELIKLER { get; set; }
    public decimal? KILOMETREDEGER { get; set; }

    // Navigation property
    [ForeignKey("FIRMAGUID")]
    public virtual FIRMAModels FIRMA { get; set; }
  }
}