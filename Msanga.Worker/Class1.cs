using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Msanga.Worker
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWorker(this IServiceCollection services, IConfiguration configuration)
        {
            //services.AddHostedService<Worker>();
            return services;
        }
    }
}
