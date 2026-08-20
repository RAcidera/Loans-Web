using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Diary.Commands.CreateDiaryEntry;

/// <summary>
/// Requirements §6/§7 — CreatedBy is not part of the request body; the
/// controller supplies it from the authenticated user, same pattern as
/// CustomersController.UploadDocument's uploadedBy parameter. EntryDate/
/// EntryTime default to "now" (requirements §6's defaults) when omitted,
/// but the caller may override either.
/// </summary>
public sealed record CreateDiaryEntryCommand(
    string Title,
    string CategoryId,
    string Notes,
    bool CaptureFinancialSnapshot,
    string CreatedBy,
    string? CustomerId = null,
    string? LoanId = null,
    string? EntryDate = null,
    string? EntryTime = null,
    string? ReminderDate = null,
    string? ReminderTime = null,
    string? Tags = null
) : IRequest<DiaryEntryDto>;

public sealed class CreateDiaryEntryCommandHandler : IRequestHandler<CreateDiaryEntryCommand, DiaryEntryDto>
{
    private readonly IDiaryRepository _diaryRepository;
    private readonly IDiaryCategoryRepository _categoryRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IFinancialSnapshotService _financialSnapshotService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppDateTimeService _appDateTime;

    public CreateDiaryEntryCommandHandler(
        IDiaryRepository diaryRepository, IDiaryCategoryRepository categoryRepository, ICustomerRepository customerRepository,
        ILoanRepository loanRepository, IFinancialSnapshotService financialSnapshotService, IUnitOfWork unitOfWork,
        IAppDateTimeService appDateTime)
    {
        _diaryRepository = diaryRepository;
        _categoryRepository = categoryRepository;
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
        _financialSnapshotService = financialSnapshotService;
        _unitOfWork = unitOfWork;
        _appDateTime = appDateTime;
    }

    public async Task<DiaryEntryDto> Handle(CreateDiaryEntryCommand request, CancellationToken ct)
    {
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

        var entryDate = request.EntryDate is not null ? DateOnly.Parse(request.EntryDate) : _appDateTime.Today;
        var entryTime = request.EntryTime is not null ? TimeOnly.Parse(request.EntryTime) : _appDateTime.TimeOfDay;
        var reminderDate = request.ReminderDate is not null ? DateOnly.Parse(request.ReminderDate) : (DateOnly?)null;
        var reminderTime = request.ReminderTime is not null ? TimeOnly.Parse(request.ReminderTime) : (TimeOnly?)null;

        var entry = DiaryEntry.Create(
            entryDate, entryTime, request.Title, categoryId, request.Notes, request.Tags,
            customerId, loanId, reminderDate, reminderTime, request.CreatedBy);

        if (request.CaptureFinancialSnapshot)
        {
            var position = await _financialSnapshotService.GetCurrentFinancialPositionAsync(entryDate, ct);
            entry.CaptureSnapshot(
                position.GrossReceivables, position.CollectibleReceivables, position.BadLoanReceivables, position.CashOnHand,
                position.ActiveLoanCount, position.OverdueLoanCount, position.BadLoanCount,
                position.CollectionsToday, position.CollectionsMonthToDate, position.LoanReleasesToday, position.LoanReleasesMonthToDate);
        }

        _diaryRepository.Add(entry);
        await _unitOfWork.SaveChangesAsync(ct); // also flushes DiaryEntryCreatedDomainEvent (+ FinancialSnapshotCapturedDomainEvent) → diary_audit_log

        return entry.ToDto(category, customerName, loanNumber);
    }
}
