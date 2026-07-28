namespace PayFlow.Infrastructure.Persistence
{
    public class PayFlowDbContext(DbContextOptions<PayFlowDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }

        public DbSet<Wallet> Wallets { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Automatically picks up all IEntityTypeConfiguration<T> classes
            // inside this assembly — no need to register each one manually
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PayFlowDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}