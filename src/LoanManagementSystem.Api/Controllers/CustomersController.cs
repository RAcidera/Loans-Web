using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Customers.Commands.CreateCustomer;
using LoanManagementSystem.Application.Customers.Commands.DeleteCustomerDocument;
using LoanManagementSystem.Application.Customers.Commands.UpdateCustomer;
using LoanManagementSystem.Application.Customers.Commands.UploadCustomerDocument;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Application.Customers.Queries.GetCustomerById;
using LoanManagementSystem.Application.Customers.Queries.GetCustomerDocumentContent;
using LoanManagementSystem.Application.Customers.Queries.GetCustomerDocuments;
using LoanManagementSystem.Application.Customers.Queries.ExportCustomersXlsx;
using LoanManagementSystem.Application.Customers.Queries.GetCustomers;
using LoanManagementSystem.Application.Customers.Queries.GetCustomersPage;
using LoanManagementSystem.Application.Customers.Queries.GetCustomersTotals;
using LoanManagementSystem.Application.Loans.Queries.GetCustomerPaymentsPage;
using LoanManagementSystem.Application.Loans.Queries.GetCustomerReceivables;
using LoanManagementSystem.Application.Loans.Queries.GetLoansByCustomer;
using LoanManagementSystem.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize] // any authenticated user (Admin or Staff) — see per-action overrides below
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/customers — Angular's LoanRepository.getCustomers().</summary>
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetCustomersQuery(), ct));

    /// <summary>GET /api/customers/page?pageIndex=&amp;pageSize=&amp;search=&amp;sortBy=&amp;sortDir=&amp;status= — server-side paging for the Customers list table.</summary>
    [HttpGet("page")]
    public async Task<ActionResult<PagedResult<CustomerListItemDto>>> GetPage(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 10, [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null, [FromQuery] string? status = null,
        CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetCustomersPageQuery(pageIndex, pageSize, search, sortBy, sortDir, status), ct));

    /// <summary>GET /api/customers/page/totals — same filters as GetPage, no paging. Backs the Customers list's KPI strip.</summary>
    [HttpGet("page/totals")]
    public async Task<ActionResult<CustomerTotalsDto>> GetPageTotals(
        [FromQuery] string? search = null, [FromQuery] string? status = null, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetCustomersTotalsQuery(search, status), ct));

    /// <summary>GET /api/customers/export — same filters as GetPage, no paging. The Customers list's "Export" button, downloaded as .xlsx.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search = null, [FromQuery] string? status = null, CancellationToken ct = default)
    {
        var file = await _mediator.Send(new ExportCustomersXlsxQuery(search, status), ct);
        return File(file.Content, file.ContentType, file.OriginalFileName);
    }

    /// <summary>GET /api/customers/{id} — Angular's LoanRepository.getCustomerById().</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(string id, CancellationToken ct)
    {
        var customer = await _mediator.Send(new GetCustomerByIdQuery(id), ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>GET /api/customers/{id}/loans — Angular's LoanRepository.getLoansByCustomer().</summary>
    [HttpGet("{id}/loans")]
    public async Task<ActionResult<List<LoanDto>>> GetLoans(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetLoansByCustomerQuery(id), ct));

    /// <summary>GET /api/customers/{id}/receivables — Customer Profile "Financial Summary" card, same formulas as the dashboard's, scoped to this customer.</summary>
    [HttpGet("{id}/receivables")]
    public async Task<ActionResult<DashboardReceivablesDto>> GetReceivables(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetCustomerReceivablesQuery(id), ct));

    /// <summary>GET /api/customers/{id}/payments/page?pageIndex=&amp;pageSize= — server-side paging for the Customer Profile "Payment History" tab, newest first across all of this customer's loans.</summary>
    [HttpGet("{id}/payments/page")]
    public async Task<ActionResult<PagedResult<PaymentWithLoanDto>>> GetPaymentsPage(
        string id, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetCustomerPaymentsPageQuery(id, pageIndex, pageSize, sortBy, sortDir), ct));

    /// <summary>POST /api/customers — SRS 3.1 "Add ... customer profiles". Admin only; not yet called by the Angular UI (no Customers page built), but available for it.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var command = new CreateCustomerCommand(request.FullName, request.Address, request.ContactNumber, request.BorrowerType, request.NicknameAlias, request.Notes);
        var created = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.CustomerId }, created);
    }

    /// <summary>PUT /api/customers/{id} — SRS 3.1 "edit ... customer profiles". Admin only, same as create.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CustomerDto>> Update(string id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var command = new UpdateCustomerCommand(id, request.FullName, request.Address, request.ContactNumber, request.BorrowerType, request.NicknameAlias, request.Notes);
        var updated = await _mediator.Send(command, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>GET /api/customers/{id}/documents — spec 3.1 "Customer Documents Management", metadata list (never the file bytes — see GetDocument for that).</summary>
    [HttpGet("{id}/documents")]
    public async Task<ActionResult<List<CustomerDocumentDto>>> GetDocuments(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetCustomerDocumentsQuery(id), ct));

    /// <summary>GET /api/customers/{id}/documents/{documentId} — downloads the original file, byte-for-byte.</summary>
    [HttpGet("{id}/documents/{documentId}")]
    public async Task<IActionResult> GetDocument(string id, string documentId, CancellationToken ct)
    {
        var file = await _mediator.Send(new GetCustomerDocumentContentQuery(id, documentId), ct);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.OriginalFileName);
    }

    /// <summary>POST /api/customers/{id}/documents — multipart/form-data upload (JPG/PNG/PDF, spec 3.1). Admin only, same as editing the rest of the profile.</summary>
    [HttpPost("{id}/documents")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(DocumentValidation.MaxFileSizeBytes)]
    public async Task<ActionResult<CustomerDocumentDto>> UploadDocument(string id, IFormFile file, CancellationToken ct)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);

        var command = new UploadCustomerDocumentCommand(id, file.FileName, file.ContentType, stream.ToArray(), User.Identity?.Name ?? "unknown");
        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/customers/{id}/documents/{documentId} — Admin only, same as upload.</summary>
    [HttpDelete("{id}/documents/{documentId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDocument(string id, string documentId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCustomerDocumentCommand(id, documentId), ct);
        return NoContent();
    }
}

public sealed record CreateCustomerRequest(string FullName, string Address, string ContactNumber, string BorrowerType, string? NicknameAlias = null, string? Notes = null);
public sealed record UpdateCustomerRequest(string FullName, string Address, string ContactNumber, string BorrowerType, string? NicknameAlias = null, string? Notes = null);
