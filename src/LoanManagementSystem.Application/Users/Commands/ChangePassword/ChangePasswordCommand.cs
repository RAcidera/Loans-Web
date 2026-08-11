using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Security;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Users.Commands.ChangePassword;

/// <summary>Self-service only — Username identifies the caller, resolved from their own auth token, never a route parameter.</summary>
public sealed record ChangePasswordCommand(
    string Username,
    string CurrentPassword,
    string NewPassword
) : IRequest;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, ct)
            ?? throw new NotFoundException(nameof(User), request.Username);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new AuthenticationFailedException("Current password is incorrect.");

        user.ChangePasswordHash(_passwordHasher.Hash(request.NewPassword));
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
