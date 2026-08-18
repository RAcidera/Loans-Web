using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Diary.Commands.UpdateDiaryEntry;

/// <summary>Requirements §13's editable fields only — never touches the financial snapshot (requirements §10; DiaryEntry.Update has no path to it).</summary>
public sealed record UpdateDiaryEntryCommand(
    string DiaryEntryId,
    string Title,
    string CategoryId,
    string Notes,
    string ModifiedBy,
    string? CustomerId = null,
    string? LoanId = null,
    string? EntryDate = null,
    string? EntryTime = null,
    string? ReminderDate = null,
    string? ReminderTime = null,
    string? Tags = null
) : IRequest<DiaryEntryDto>;

public sealed class UpdateDiaryEntryCommandHandler : IRequestHandler<UpdateDiaryEntryCommand, DiaryEntryDto>
{
    private readonly IDiaryRepository _diaryRepository;
    private readonly IDiaryCategoryRepository _categoryRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDiaryEntryCommandHandler(
        IDiaryRepository diaryRepository, IDiaryCategoryRepository categoryRepository,
        ICustomerRepository customerRepository, ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _diaryRepository = diaryRepository;
        _categoryRepository = categoryRepository;
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DiaryEntryDto> Handle(UpdateDiaryEntryCommand request, CancellationToken ct)
    {
        var id = Domain.Diary.DiaryEntryId.Parse(request.DiaryEntryId);
        var entry = await _diaryRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(DiaryEntry), request.DiaryEntryId);

        var categoryId = DiaryCategoryId.Parse(request.CategoryId);
        var category = await _categoryRepository.GetByIdAsync(categoryId, ct)
            ?? throw new NotFoundException(nameof(DiaryCategory), request.CategoryId);

        CustomerId? customerId = null;
        string? customerName = null;
        if (request.CustomerId is not null)
        {
            var parsedCustomerId = CustomerId.Parse(request.CustomerId);
            var customer = await _customerRepository.GetByIdAsync(parsedCustomerId, ct)
                ?? throw new NotFoundException(nameof(Customer), request.CustomerId);
            customerId = parsedCustomerId;
            customerName = customer.FullName;
        }

        LoanId? loanId = null;
        int? loanNumber = null;
        if (request.LoanId is not null)
        {
            var parsedLoanId = LoanId.Parse(request.LoanId);
            var loan = await _loanRepository.GetByIdAsync(parsedLoanId, ct)
                ?? throw new NotFoundException(nameof(Loan), request.LoanId);
            loanId = parsedLoanId;
            loanNumber = loan.LoanNumber;
        }

        var entryDate = request.EntryDate is not null ? DateOnly.Parse(request.EntryDate) : entry.EntryDate;
        var entryTime = request.EntryTime is not null ? TimeOnly.Parse(request.EntryTime) : entry.EntryTime;
        var reminderDate = request.ReminderDate is not null ? DateOnly.Parse(request.ReminderDate) : (DateOnly?)null;
        var reminderTime = request.ReminderTime is not null ? TimeOnly.Parse(request.ReminderTime) : (TimeOnly?)null;

        entry.Update(entryDate, entryTime, request.Title, categoryId, request.Notes, request.Tags, customerId, loanId, reminderDate, reminderTime, request.ModifiedBy);

        await _unitOfWork.SaveChangesAsync(ct); // also flushes DiaryEntryUpdatedDomainEvent (+ ReminderChanged/LinkedCustomerChanged/LinkedLoanChanged as applicable) → diary_audit_log

        return entry.ToDto(category, customerName, loanNumber);
    }
}
