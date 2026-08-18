using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Diary.Queries.GetDiaryEntry;

public sealed record GetDiaryEntryQuery(string DiaryEntryId) : IRequest<DiaryEntryDto>;

public sealed class GetDiaryEntryQueryHandler : IRequestHandler<GetDiaryEntryQuery, DiaryEntryDto>
{
    private readonly IDiaryRepository _diaryRepository;
    private readonly IDiaryCategoryRepository _categoryRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;

    public GetDiaryEntryQueryHandler(
        IDiaryRepository diaryRepository, IDiaryCategoryRepository categoryRepository,
        ICustomerRepository customerRepository, ILoanRepository loanRepository)
    {
        _diaryRepository = diaryRepository;
        _categoryRepository = categoryRepository;
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
    }

    public async Task<DiaryEntryDto> Handle(GetDiaryEntryQuery request, CancellationToken ct)
    {
        var id = DiaryEntryId.Parse(request.DiaryEntryId);
        var entry = await _diaryRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(DiaryEntry), request.DiaryEntryId);

        var category = await _categoryRepository.GetByIdAsync(entry.CategoryId, ct)
            ?? throw new NotFoundException(nameof(DiaryCategory), entry.CategoryId.ToString());

        string? customerName = null;
        if (entry.CustomerId is { } customerId)
            customerName = (await _customerRepository.GetByIdAsync(customerId, ct))?.FullName;

        int? loanNumber = null;
        if (entry.LoanId is { } loanId)
            loanNumber = (await _loanRepository.GetByIdAsync(loanId, ct))?.LoanNumber;

        return entry.ToDto(category, customerName, loanNumber);
    }
}
