namespace XtraSoft.Infrastructure
{    
    public class EfRepository<T> : EfIRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        public EfRepository(ApplicationDbContext db) => _db = db;
    }

    public interface EfIRepository<T> where T : class { }
}
