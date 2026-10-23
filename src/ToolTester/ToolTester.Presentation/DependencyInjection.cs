using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Services;
using ToolTester.Presentation.PageModels;
using ToolTester.Presentation.Services;

namespace ToolTester.Presentation
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentationServices(this IServiceCollection services)
        {
            services.AddSingleton<ModalErrorHandler>();
            services.AddSingleton<MainPageModel>();
            services.AddSingleton<CatalogPageModel>();
            services.AddSingleton<RelationshipsPageModel>();
            services.AddSingleton<JulietCoveragesPageModel>();
            services.AddSingleton<ParsePageModel>();
            services.AddSingleton<ReportPageModel>();
            services.AddSingleton<SemanticRulesViewModel>();
            // inside your existing AddPresentationServices() extension method add:            
            services.AddTransient<PageModels.ScansPageModel>();
            services.AddTransient<Pages.ScansPage>();
            services.AddTransient<PageModels.ScansPageModel>();
            services.AddTransient<ToolDeclaredCwesPage>();
            services.AddTransient<ToolDeclaredCwesPageModel>();

            services.AddSingleton<IScanDisplayNameProvider, ScanDisplayNameProvider>();
            services.AddScoped<IHeatmapQuery, ScannerHeatmapService>();
            services.AddScoped<IFalsePositiveQuery, FalsePositiveQuery>();
            services.AddScoped<IToolDeclaredCoverageService, ToolDeclaredCoverageService>();
            services.AddScoped<IReportQueryService, ReportQueryService>();
            services.AddSingleton<IClosedXmlReportExportService, ClosedXmlReportExportService>();
            return services;
        }
    }
}
