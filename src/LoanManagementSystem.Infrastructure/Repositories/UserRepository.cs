using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    // AsNoTracking: pure read path (list view) — see CustomerRepository.GetAllAsync for the same rationale.
    public Task<List<User>> GetAllAsync(CancellationToken ct = default) =>
        _db.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync(ct);

    public void Add(User user) => _db.Users.Add(user);
}
