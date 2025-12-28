namespace XtraSoft.Infrastructure
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;
        public EfUnitOfWork(ApplicationDbContext db) => _db = db;
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }

    public interface IUnitOfWork 
    { 
        Task<int> SaveChangesAsync(CancellationToken ct = default); 
    }
}
