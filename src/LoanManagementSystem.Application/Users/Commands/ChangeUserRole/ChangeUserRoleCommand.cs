using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Users.Commands.ChangeUserRole;

public sealed record ChangeUserRoleCommand(string UserId, string NewRole) : IRequest<UserDto>;

public sealed class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeUserRoleCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> Handle(ChangeUserRoleCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(UserId.Parse(request.UserId), ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (!Enum.TryParse<UserRole>(request.NewRole, ignoreCase: true, out var role))
            throw new DomainException($"Unknown role '{request.NewRole}'.");

        user.ChangeRole(role);
        await _unitOfWork.SaveChangesAsync(ct);

        return user.ToDto();
    }
}
