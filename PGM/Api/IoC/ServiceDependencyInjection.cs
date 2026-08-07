using PGM.Core.Application.Interfaces;
using PGM.Core.Common.Attributes;
using PGM.Infrastructure.Persistence;
using PGM.Infrastructure.Repositories;

namespace PGM.Api.IoC;

/// <summary>
/// 集中註冊：
///   1. Infrastructure 具體實作（Connection Factory、DbSession；Phase 1 起加 UnitOfWork、Repositories）
///   2. Core 中掛 attribute 的 Service / Mapper 類別（掃描式 DI）
/// </summary>
public static class ServiceDependencyInjection
{
    public static IServiceCollection Register(this IServiceCollection services)
    {
        // ---------- Infrastructure ----------
        // Connection Factory：Scoped 就夠，若想全 App 共用也可改 Singleton
        services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

        // DbSession：Scoped，每 request 一份，同 request 內共享 connection + transaction
        services.AddScoped<IDbSession, DbSession>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IFunctionRepository, FunctionRepository>();
        services.AddScoped<IParameterRepository, ParameterRepository>();
        services.AddScoped<IAuthenticationLogRepository, AuthenticationLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ---------- Core：透過 attribute 掃描註冊 Service / Mapper ----------
        var coreAssembly = typeof(IRequestContext).Assembly;

        var registrations = coreAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t =>
                t.IsDefined(typeof(ScopedRegistrationAttribute), false) ||
                t.IsDefined(typeof(TransientRegistrationAttribute), false) ||
                t.IsDefined(typeof(SingletonRegistrationAttribute), false))
            .Select(t => new
            {
                Interface = t.GetInterface($"I{t.Name}"),
                Implementation = t
            })
            .Where(x => x.Interface != null);

        foreach (var reg in registrations)
        {
            if (reg.Implementation.IsDefined(typeof(ScopedRegistrationAttribute), false))
                services.AddScoped(reg.Interface!, reg.Implementation);

            if (reg.Implementation.IsDefined(typeof(TransientRegistrationAttribute), false))
                services.AddTransient(reg.Interface!, reg.Implementation);

            if (reg.Implementation.IsDefined(typeof(SingletonRegistrationAttribute), false))
                services.AddSingleton(reg.Interface!, reg.Implementation);
        }

        return services;
    }
}
