using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            IAppPathProvider? paths = provider.GetRequiredService<IAppPathProvider>();
            options.UseSqlite($"Data Source={paths.DatabasePath}");
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
