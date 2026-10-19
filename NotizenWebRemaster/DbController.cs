//using System.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Configuration;
using System.Drawing.Text;


namespace NotizenWebRemaster
{
    public class User
    {
        public int uid { get; set; }
        public string ?username { get; set; }

    }

    public class Artikel
    {
        public int id { get; set; } 
        public string ?name { get; set; }
        public decimal price { get; set; }
        public bool done { get; set; } 

    }

    public class NotizenContext : DbContext
    {
        
        public DbSet<Artikel> Artikels { get; set; }
        public NotizenContext() { }
        
        public NotizenContext(DbContextOptions<NotizenContext> options) : base(options)
        {


        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (options.IsConfigured) return;

            var constring = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
            
            options.UseNpgsql(constring);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Artikel> ( x =>
            {
                x.Property(x => x.id).UseIdentityAlwaysColumn();
                x.Property(x => x.done).HasDefaultValue(false);
            });
        }
    }
}
