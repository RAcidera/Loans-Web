using LoanManagementSystem.Application.Common.Pdf;
using LoanManagementSystem.Application.Common.Security;
using LoanManagementSystem.Application.Common.Xlsx;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Pdf;
using LoanManagementSystem.Infrastructure.Persistence;
using LoanManagementSystem.Infrastructure.Repositories;
using LoanManagementSystem.Infrastructure.Security;
using LoanManagementSystem.Infrastructure.Xlsx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoanManagementSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' in configuration.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<ICashLedgerRepository, CashLedgerRepository>();
        services.AddScoped<ILoanLedgerRepository, LoanLedgerRepository>();
        services.AddScoped<ILoanAuditLogRepository, LoanAuditLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDiaryRepository, DiaryRepository>();
        services.AddScoped<IDiaryCategoryRepository, DiaryCategoryRepository>();
        services.AddScoped<IDiaryAuditLogRepository, DiaryAuditLogRepository>();
        services.AddScoped<IPromiseToPayRepository, PromiseToPayRepository>();
        services.AddScoped<IPromiseAuditLogRepository, PromiseAuditLogRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IStatementOfAccountPdfGenerator, QuestPdfStatementOfAccountGenerator>();
        services.AddScoped<ILoansXlsxExportGenerator, ClosedXmlLoansExportGenerator>();
        services.AddScoped<IPaymentsXlsxExportGenerator, ClosedXmlPaymentsExportGenerator>();
        services.AddScoped<ICustomersXlsxExportGenerator, ClosedXmlCustomersExportGenerator>();
        services.AddScoped<ICashLedgerXlsxExportGenerator, ClosedXmlCashLedgerExportGenerator>();
        services.AddScoped<IInterestEarnedReportXlsxExportGenerator, ClosedXmlInterestEarnedReportExportGenerator>();
        services.AddScoped<IInterestEarnedReportPdfGenerator, QuestPdfInterestEarnedReportGenerator>();

        return services;
    }
}
