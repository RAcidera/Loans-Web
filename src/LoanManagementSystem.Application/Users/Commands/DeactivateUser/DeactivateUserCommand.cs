using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Users.Commands.DeactivateUser;

/// <summary>CallerUsername is the acting Admin, resolved from their own auth token — used only to block self-deactivation.</summary>
public sealed record DeactivateUserCommand(string UserId, string CallerUsername) : IRequest;

public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(UserId.Parse(request.UserId), ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (string.Equals(user.Username, request.CallerUsername, StringComparison.Ordinal))
            throw new DomainException("You cannot deactivate your own account.");

        user.Deactivate();
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
