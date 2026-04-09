using Microsoft.EntityFrameworkCore;
using DocManagerAI.Models;

namespace DocManagerAI.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User>            Users            { get; set; }
        public DbSet<Document>        Documents        { get; set; }
        public DbSet<ApprovalHistory> ApprovalHistories { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApprovalHistory>()
                .HasOne(a => a.Document)
                .WithMany(d => d.ApprovalHistories)
                .HasForeignKey(a => a.DocumentId);

            modelBuilder.Entity<Document>()
                .Property(d => d.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Document>()
                .Property(d => d.VAT)
                .HasColumnType("decimal(18,2)");
        }
    }
}
