using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VipTransfer.Web.Models;

namespace VipTransfer.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<REZERVASYONModels> REZERVASYON { get; set; }
        public DbSet<MUSTERIModels> MUSTERI { get; set; }
        public DbSet<LOGINLOGModels> LOGINLOG { get; set; }
        public DbSet<KULLANICIModels> KULLANICI { get; set; }
        public DbSet<FIRMAModels> FIRMA { get; set; }
        public DbSet<ARACLARModels> ARACLAR { get; set; }
        public DbSet<ARACFOTOModels> ARACFOTO { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // REZERVASYON tablosu için
            modelBuilder.Entity<REZERVASYONModels>()
                .HasKey(r => r.REZGUID);

            // MUSTERI tablosu için
            modelBuilder.Entity<MUSTERIModels>()
                .HasKey(m => m.MUSTERIGUID);

            // LOGINLOG tablosu için
            modelBuilder.Entity<LOGINLOGModels>()
                .HasKey(l => l.LLGUID);

            // KULLANICI tablosu için
            modelBuilder.Entity<KULLANICIModels>()
                .HasKey(k => k.KADI);

            // FIRMA tablosu için
            modelBuilder.Entity<FIRMAModels>()
                .HasKey(f => f.FIRMAGUID);

            // ARACLAR tablosu için
            modelBuilder.Entity<ARACLARModels>()
                .HasKey(a => a.ARACGUID);
        }
    }
}