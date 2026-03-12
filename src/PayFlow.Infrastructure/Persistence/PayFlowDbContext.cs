using Microsoft.EntityFrameworkCore;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence
{
    public class PayFlowDbContext : DbContext
    {
        public PayFlowDbContext(DbContextOptions<PayFlowDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Wallet> Wallets { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Automatically picks up all IEntityTypeConfiguration<T> classes
            // inside this assembly — no need to register each one manually
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PayFlowDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}