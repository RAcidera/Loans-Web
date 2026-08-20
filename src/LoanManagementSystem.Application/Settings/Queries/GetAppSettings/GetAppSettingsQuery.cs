using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using MediatR;

namespace LoanManagementSystem.Application.Settings.Queries.GetAppSettings;

public sealed record GetAppSettingsQuery : IRequest<AppSettingsDto>;

public sealed class GetAppSettingsQueryHandler : IRequestHandler<GetAppSettingsQuery, AppSettingsDto>
{
    private readonly IAppDateTimeService _appDateTime;

    public GetAppSettingsQueryHandler(IAppDateTimeService appDateTime)
    {
        _appDateTime = appDateTime;
    }

    public Task<AppSettingsDto> Handle(GetAppSettingsQuery request, CancellationToken ct) =>
        Task.FromResult(new AppSettingsDto(
            BusinessTimeZoneId: _appDateTime.BusinessTimeZoneId,
            CurrentBusinessDate: _appDateTime.Today.ToString("yyyy-MM-dd")));
}
