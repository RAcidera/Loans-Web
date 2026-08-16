using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Users.Commands.ActivateUser;

/// <summary>Reactivates a deactivated user. No self-protection needed here (unlike DeactivateUserCommand) — reactivating your own account isn't a way to lock yourself out.</summary>
public sealed record ActivateUserCommand(string UserId) : IRequest;

public sealed class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ActivateUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(UserId.Parse(request.UserId), ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.Activate();
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
