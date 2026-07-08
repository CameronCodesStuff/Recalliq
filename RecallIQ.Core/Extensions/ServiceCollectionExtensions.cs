using Microsoft.Extensions.DependencyInjection;

namespace RecallIQ.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRecallIQCore(this IServiceCollection services)
    {
        return services;
    }
}
