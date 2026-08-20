using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.Settings;
using MediatR;

namespace LoanManagementSystem.Application.Settings.Commands.UpdateBusinessTimeZone;

public sealed record UpdateBusinessTimeZoneCommand(string TimeZoneId) : IRequest<AppSettingsDto>;

public sealed class UpdateBusinessTimeZoneCommandHandler : IRequestHandler<UpdateBusinessTimeZoneCommand, AppSettingsDto>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBusinessTimeZoneCache _timeZoneCache;
    private readonly IAppDateTimeService _appDateTime;

    public UpdateBusinessTimeZoneCommandHandler(
        ISettingsRepository settingsRepository, IUnitOfWork unitOfWork,
        IBusinessTimeZoneCache timeZoneCache, IAppDateTimeService appDateTime)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _timeZoneCache = timeZoneCache;
        _appDateTime = appDateTime;
    }

    public async Task<AppSettingsDto> Handle(UpdateBusinessTimeZoneCommand request, CancellationToken ct)
    {
        try
        {
            BusinessTimeZoneCalculator.ResolveTimeZone(request.TimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new DomainException($"'{request.TimeZoneId}' is not a recognized IANA time zone id.");
        }

        var setting = await _settingsRepository.GetByKeyAsync(AppSetting.Keys.BusinessTimeZone, ct);
        if (setting is null)
        {
            setting = AppSetting.Create(AppSetting.Keys.BusinessTimeZone, request.TimeZoneId);
            _settingsRepository.Add(setting);
        }
        else
        {
            setting.UpdateValue(request.TimeZoneId);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Refresh the shared in-memory cache immediately so this change is
        // live for the very next request, not after some cache TTL — see
        // IBusinessTimeZoneCache's own doc comment.
        await _timeZoneCache.RefreshAsync(_settingsRepository, ct);

        return new AppSettingsDto(_appDateTime.BusinessTimeZoneId, _appDateTime.Today.ToString("yyyy-MM-dd"));
    }
}
