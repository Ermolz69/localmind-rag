using KnowledgeApp.Application.Buckets;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddBucketApplication(this IServiceCollection services)
    {
        services.AddScoped<IBucketResolver, BucketResolver>();
        services.AddScoped<BucketRequestValidator>();
        services.AddScoped<CreateBucketHandler>();
        services.AddScoped<DeleteBucketHandler>();
        services.AddScoped<GetBucketsHandler>();
        services.AddScoped<UpdateBucketHandler>();

        return services;
    }
}
