using AutoMapper;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ToolTester.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var myassemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.Contains("ToolTester")).ToList();
             services.AddAutoMapper(myassemblies);

            // services.AddValidatorsFromAssemblies(myassemblies);
            // services.AddTransient(typeof(IRequestExceptionHandler<,,>), typeof(GlobalRequestExceptionHandler<,,>));
            services.AddMediatR(config =>
            {
                config.MaxTypesClosing = 0;
                config.RegisterGenericHandlers = true;
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                //config.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));                
              //  config.AddOpenBehavior(typeof(ValidationBehaviour<,>));
                // config.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));
                // config.AddOpenBehavior(typeof(CachingBehaviour<,>));
                //config.AddOpenBehavior(typeof(CacheInvalidationBehaviour<,>));
               // config.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
                config.AddOpenBehavior(typeof(RequestExceptionProcessorBehavior<,>));


            });
            return services;
        }
    }
}
