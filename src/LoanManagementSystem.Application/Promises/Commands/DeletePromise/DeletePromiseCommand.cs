using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Promises.Commands.DeletePromise;

public sealed record DeletePromiseCommand(string PromiseId) : IRequest;

public sealed class DeletePromiseCommandHandler : IRequestHandler<DeletePromiseCommand>
{
    private readonly IPromiseToPayRepository _promiseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePromiseCommandHandler(IPromiseToPayRepository promiseRepository, IUnitOfWork unitOfWork)
    {
        _promiseRepository = promiseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeletePromiseCommand request, CancellationToken ct)
    {
        var id = PromiseToPayId.Parse(request.PromiseId);
        var promise = await _promiseRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(PromiseToPay), request.PromiseId);

        _promiseRepository.Remove(promise);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
