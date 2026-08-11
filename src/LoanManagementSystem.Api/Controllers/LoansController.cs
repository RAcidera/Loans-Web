using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Application.Loans.Commands.ChangeLoanClassification;
using LoanManagementSystem.Application.Loans.Commands.CreateLoan;
using LoanManagementSystem.Application.Loans.Commands.DeleteExtension;
using LoanManagementSystem.Application.Loans.Commands.DeleteLoanDocument;
using LoanManagementSystem.Application.Loans.Commands.DeletePayment;
using LoanManagementSystem.Application.Loans.Commands.ExtendLoan;
using LoanManagementSystem.Application.Loans.Commands.RecordPayment;
using LoanManagementSystem.Application.Loans.Commands.UpdateExtension;
using LoanManagementSystem.Application.Loans.Commands.UpdateLoan;
using LoanManagementSystem.Application.Loans.Commands.UpdatePayment;
using LoanManagementSystem.Application.Loans.Commands.UploadLoanDocument;
using LoanManagementSystem.Application.Loans.Commands.WriteOffLoan;
using LoanManagementSystem.Application.Loans.Queries.GenerateLoanSoa;
using LoanManagementSystem.Application.Loans.Queries.GetLoanAuditLog;
using LoanManagementSystem.Application.Loans.Queries.GetLoanDetail;
using LoanManagementSystem.Application.Loans.Queries.GetLoanDocumentContent;
using LoanManagementSystem.Application.Loans.Queries.GetLoanDocuments;
using LoanManagementSystem.Application.Loans.Queries.GetLoanLedger;
using LoanManagementSystem.Application.Loans.Queries.GetLoans;
using LoanManagementSystem.Application.Loans.Queries.GetLoansPage;
using LoanManagementSystem.Application.Loans.Queries.GetLoansTotals;
using LoanManagementSystem.Application.Loans.Queries.GetRecentPayments;
using LoanManagementSystem.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

[ApiController]
[Route("api/loans")]
[Authorize] // any authenticated user (Admin or Staff) — see per-action overrides below
public class LoansController : ControllerBase
{
    private readonly IMediator _mediator;

    public LoansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/loans — Angular's LoanRepository.getLoans(). Backs the dashboard table.</summary>
    [HttpGet]
    public async Task<ActionResult<List<LoanDto>>> GetAll(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetLoansQuery(), ct));

    /// <summary>GET /api/loans/page?pageIndex=&amp;pageSize=&amp;search=&amp;sortBy=&amp;sortDir=&amp;status=&amp;classification=&amp;loanDateFrom=&amp;loanDateTo=&amp;dueDateFrom=&amp;dueDateTo=&amp;badLoansOnly=&amp;overdueOnly= — server-side paging + filtering for the Loans list table (spec "Loan Search and Filtering").</summary>
    [HttpGet("page")]
    public async Task<ActionResult<PagedResult<LoanDto>>> GetPage(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 10, [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null,
        [FromQuery] string? status = null, [FromQuery] string? classification = null,
        [FromQuery] DateOnly? loanDateFrom = null, [FromQuery] DateOnly? loanDateTo = null,
        [FromQuery] DateOnly? dueDateFrom = null, [FromQuery] DateOnly? dueDateTo = null,
        [FromQuery] bool badLoansOnly = false, [FromQuery] bool overdueOnly = false, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetLoansPageQuery(
            pageIndex, pageSize, search, sortBy, sortDir, status, classification,
            loanDateFrom, loanDateTo, dueDateFrom, dueDateTo, badLoansOnly, overdueOnly), ct));

    /// <summary>GET /api/loans/page/totals — same filters as GetPage, no paging. Backs the Loans list's footer totals row.</summary>
    [HttpGet("page/totals")]
    public async Task<ActionResult<LoanTotalsDto>> GetPageTotals(
        [FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] string? classification = null,
        [FromQuery] DateOnly? loanDateFrom = null, [FromQuery] DateOnly? loanDateTo = null,
        [FromQuery] DateOnly? dueDateFrom = null, [FromQuery] DateOnly? dueDateTo = null,
        [FromQuery] bool badLoansOnly = false, [FromQuery] bool overdueOnly = false, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetLoansTotalsQuery(
            search, status, classification, loanDateFrom, loanDateTo, dueDateFrom, dueDateTo, badLoansOnly, overdueOnly), ct));

    /// <summary>GET /api/loans/{id} — Angular's LoanRepository.getLoanById(), used inside GetLoanDetailUseCase.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LoanDto>> GetById(string id, CancellationToken ct)
    {
        var detail = await _mediator.Send(new GetLoanDetailQuery(id), ct);
        return detail.Loan is null ? NotFound() : Ok(detail.Loan);
    }

    /// <summary>
    /// GET /api/loans/{id}/detail — a convenience endpoint returning loan +
    /// extensions + payments in one round trip. Angular's frontend composes
    /// this itself via GetLoanDetailUseCase's forkJoin of three calls; this
    /// endpoint exists for any other client that would rather not.
    /// </summary>
    [HttpGet("{id}/detail")]
    public async Task<ActionResult<LoanDetailDto>> GetDetail(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetLoanDetailQuery(id), ct));

    /// <summary>GET /api/loans/{id}/extensions — Angular's LoanRepository.getExtensions().</summary>
    [HttpGet("{id}/extensions")]
    public async Task<ActionResult<List<LoanExtensionDto>>> GetExtensions(string id, CancellationToken ct)
    {
        var detail = await _mediator.Send(new GetLoanDetailQuery(id), ct);
        return Ok(detail.Extensions);
    }

    /// <summary>GET /api/loans/{id}/payments — Angular's LoanRepository.getPayments().</summary>
    [HttpGet("{id}/payments")]
    public async Task<ActionResult<List<PaymentDto>>> GetPayments(string id, CancellationToken ct)
    {
        var detail = await _mediator.Send(new GetLoanDetailQuery(id), ct);
        return Ok(detail.Payments);
    }

    /// <summary>GET /api/payments/recent?limit= is routed here too, kept for discoverability — see PaymentsController for the actual route.</summary>

    /// <summary>POST /api/loans — SRS 3.2, originates a new loan. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LoanDto>> Create(CreateLoanCommand command, CancellationToken ct)
    {
        var created = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.LoanId }, created);
    }

    /// <summary>POST /api/loans/{id}/payments — Angular's LoanRepository.recordPayment(), SRS 3.4. Admin or Staff (collectors record payments in the field).</summary>
    [HttpPost("{id}/payments")]
    public async Task<ActionResult<PaymentDto>> RecordPayment(string id, RecordPaymentRequest request, CancellationToken ct)
    {
        var command = new RecordPaymentCommand(id, request.AmountPaid, request.PaymentMethod, request.Notes ?? string.Empty, request.ReferenceNumber);
        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>PUT /api/loans/{id}/payments/{paymentId} — edits a payment, rolling its amount change into the loan's Balance/Status.</summary>
    [HttpPut("{id}/payments/{paymentId}")]
    public async Task<ActionResult<PaymentDto>> UpdatePayment(string id, string paymentId, UpdatePaymentRequest request, CancellationToken ct)
    {
        var command = new UpdatePaymentCommand(id, paymentId, request.AmountPaid, request.PaymentMethod, request.Notes, request.ReferenceNumber, request.PaymentDate);
        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/loans/{id}/payments/{paymentId} — removes a payment, rolling it back out of the loan's Balance/Status.</summary>
    [HttpDelete("{id}/payments/{paymentId}")]
    public async Task<ActionResult<LoanDto>> DeletePayment(string id, string paymentId, CancellationToken ct) =>
        Ok(await _mediator.Send(new DeletePaymentCommand(id, paymentId), ct));

    /// <summary>POST /api/loans/{id}/extensions — Angular's LoanRepository.extendLoan(), SRS 3.3. Admin only — extending terms/fees is a business decision, not a collection-desk one.</summary>
    [HttpPost("{id}/extensions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LoanExtensionDto>> Extend(string id, ExtendLoanRequest request, CancellationToken ct)
    {
        var command = new ExtendLoanCommand(id, request.ExtensionDays, request.AdditionalInterestAmount, request.Remarks ?? string.Empty, request.AdditionalChargesAmount);
        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>PUT /api/loans/{id}/extensions/{extensionId} — edits an extension, rolling its old contribution out of DueDate/TotalInterest/TotalExtensionCharges first. Admin only, same rationale as Extend.</summary>
    [HttpPut("{id}/extensions/{extensionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LoanExtensionDto>> UpdateExtension(string id, string extensionId, UpdateExtensionRequest request, CancellationToken ct)
    {
        var command = new UpdateExtensionCommand(id, extensionId, request.ExtensionDays, request.AdditionalInterestAmount, request.Remarks, request.AdditionalChargesAmount, request.ExtensionDate);
        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/loans/{id}/extensions/{extensionId} — removes an extension, reverting DueDate/TotalInterest/TotalExtensionCharges. Admin only, same rationale as Extend.</summary>
    [HttpDelete("{id}/extensions/{extensionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LoanDto>> DeleteExtension(string id, string extensionId, CancellationToken ct) =>
        Ok(await _mediator.Send(new DeleteExtensionCommand(id, extensionId), ct));

    /// <summary>PUT /api/loans/{id} — spec's "Edit Loan" button on the Loan Details page: overrides Loan Date/Due Date/Interest Rate/Interest Amount/Remarks post-creation. Admin only, same rationale as Extend.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LoanDto>> Update(string id, UpdateLoanRequest request, CancellationToken ct)
    {
        var command = new UpdateLoanCommand(id, User.Identity?.Name ?? "unknown", request.StartDate, request.DueDate, request.InterestRate, request.InterestAmount, request.Remarks);
        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>PUT /api/loans/{id}/classification — spec's "Change Classification" button on the Loan Details page. Any authenticated user, same as recording a payment: it's a day-to-day risk-tracking update, not a business decision like Extend.</summary>
    [HttpPut("{id}/classification")]
    public async Task<ActionResult<LoanDto>> ChangeClassification(string id, ChangeLoanClassificationRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new ChangeLoanClassificationCommand(id, request.Classification, User.Identity?.Name ?? "unknown"), ct));

    /// <summary>POST /api/loans/{id}/write-off — removes a loan from active collection. Admin only, same rationale as Extend: a business decision, not a collection-desk one.</summary>
    [HttpPost("{id}/write-off")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LoanDto>> WriteOff(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new WriteOffLoanCommand(id, User.Identity?.Name ?? "unknown"), ct));

    /// <summary>GET /api/loans/{id}/ledger — the SRS's "Additional Recommendation: Loan Ledger", backs the Payments/Extensions tabs' Running Balance columns.</summary>
    [HttpGet("{id}/ledger")]
    public async Task<ActionResult<List<LoanLedgerEntryDto>>> GetLedger(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetLoanLedgerQuery(id), ct));

    /// <summary>GET /api/loans/{id}/audit-log — Loan Details "Audit Log" tab.</summary>
    [HttpGet("{id}/audit-log")]
    public async Task<ActionResult<List<LoanAuditLogEntryDto>>> GetAuditLog(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetLoanAuditLogQuery(id), ct));

    /// <summary>GET /api/loans/{id}/soa — spec's Statement of Account PDF, sourced from the Phase 6 ledger for authoritative running balances.</summary>
    [HttpGet("{id}/soa")]
    public async Task<IActionResult> GenerateSoa(string id, CancellationToken ct)
    {
        var file = await _mediator.Send(new GenerateLoanSoaQuery(id), ct);
        return File(file.Content, file.ContentType, file.OriginalFileName);
    }

    /// <summary>GET /api/loans/{id}/documents — Loan Details "Documents" tab, metadata list (never the file bytes — see GetDocument for that).</summary>
    [HttpGet("{id}/documents")]
    public async Task<ActionResult<List<LoanDocumentDto>>> GetDocuments(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetLoanDocumentsQuery(id), ct));

    /// <summary>GET /api/loans/{id}/documents/{documentId} — downloads the original file, byte-for-byte.</summary>
    [HttpGet("{id}/documents/{documentId}")]
    public async Task<IActionResult> GetDocument(string id, string documentId, CancellationToken ct)
    {
        var file = await _mediator.Send(new GetLoanDocumentContentQuery(id, documentId), ct);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.OriginalFileName);
    }

    /// <summary>POST /api/loans/{id}/documents — multipart/form-data upload (JPG/PNG/PDF). Admin only, same rationale as Extend.</summary>
    [HttpPost("{id}/documents")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(DocumentValidation.MaxFileSizeBytes)]
    public async Task<ActionResult<LoanDocumentDto>> UploadDocument(string id, IFormFile file, CancellationToken ct)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);

        var command = new UploadLoanDocumentCommand(id, file.FileName, file.ContentType, stream.ToArray(), User.Identity?.Name ?? "unknown");
        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/loans/{id}/documents/{documentId} — Admin only, same as upload.</summary>
    [HttpDelete("{id}/documents/{documentId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDocument(string id, string documentId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteLoanDocumentCommand(id, documentId), ct);
        return NoContent();
    }
}

public sealed record RecordPaymentRequest(decimal AmountPaid, string PaymentMethod, string? Notes, string? ReferenceNumber = null);

public sealed record UpdatePaymentRequest(decimal AmountPaid, string PaymentMethod, string? Notes, string? ReferenceNumber = null, string? PaymentDate = null);

public sealed record ExtendLoanRequest(int ExtensionDays, decimal AdditionalInterestAmount, string? Remarks, decimal AdditionalChargesAmount = 0);

public sealed record UpdateExtensionRequest(int ExtensionDays, decimal AdditionalInterestAmount, string? Remarks, decimal AdditionalChargesAmount = 0, string? ExtensionDate = null);

public sealed record UpdateLoanRequest(string? StartDate = null, string? DueDate = null, decimal? InterestRate = null, decimal? InterestAmount = null, string? Remarks = null);

public sealed record ChangeLoanClassificationRequest(string Classification);
