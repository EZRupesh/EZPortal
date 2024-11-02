using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace EZJOB.Models
{
    public partial class EZJob : DbContext
    {
        public EZJob()
            : base("name=EZJob")
        {
        }

        public virtual DbSet<PausedDetail> PausedDetails { get; set; }
        public virtual DbSet<CustomerJob> CustomerJobs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer<EZJob>(null);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PausedDetail>()
                .Property(e => e.PausedRemark)
                .IsUnicode(false);

            modelBuilder.Entity<PausedDetail>()
                .Property(e => e.ResumeRemark)
                .IsUnicode(false);
        }
    }
}
