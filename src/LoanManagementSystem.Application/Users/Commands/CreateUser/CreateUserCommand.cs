using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Security;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Username,
    string Password,
    string Role
) : IRequest<UserDto>;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        if (await _userRepository.GetByUsernameAsync(request.Username, ct) is not null)
            throw new DomainException("Username already taken.");

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            throw new DomainException($"Unknown role '{request.Role}'.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Username, passwordHash, role);

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(ct);

        return user.ToDto();
    }
}
