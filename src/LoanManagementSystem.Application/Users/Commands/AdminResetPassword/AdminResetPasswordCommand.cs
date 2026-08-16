using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Security;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Users.Commands.AdminResetPassword;

/// <summary>
/// An admin setting a NEW password for someone else's account (they forgot
/// it) — deliberately a separate command from ChangePasswordCommand rather
/// than a shared one with an optional CurrentPassword, same reasoning as
/// the controller's separate "me/password" vs "{id}/..." routes: the two
/// have different authorization stories (self-service, proving you know
/// the old password vs admin-only, no old password check at all) and
/// collapsing them into one command would make it easy to accidentally
/// expose the no-verification path to a non-admin caller.
/// </summary>
public sealed record AdminResetPasswordCommand(string UserId, string NewPassword) : IRequest;

public sealed class AdminResetPasswordCommandHandler : IRequestHandler<AdminResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public AdminResetPasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AdminResetPasswordCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(UserId.Parse(request.UserId), ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.ChangePasswordHash(_passwordHasher.Hash(request.NewPassword));
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
