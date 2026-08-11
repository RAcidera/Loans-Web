using LoanManagementSystem.Domain.Identity;

namespace LoanManagementSystem.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
    void Add(User user);
}
