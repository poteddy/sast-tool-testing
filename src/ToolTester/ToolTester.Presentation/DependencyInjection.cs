using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


        
            //builder.Services.AddSingleton<ProjectRepository>();
            //builder.Services.AddSingleton<TaskRepository>();
            //builder.Services.AddSingleton<CategoryRepository>();
            //builder.Services.AddSingleton<TagRepository>();
            //builder.Services.AddSingleton<SeedDataService>();


            //builder.Services.AddSingleton<ProjectListPageModel>();
            //builder.Services.AddSingleton<ManageMetaPageModel>();

            //builder.Services.AddTransientWithShellRoute<ProjectDetailPage, ProjectDetailPageModel>("project");
            //builder.Services.AddTransientWithShellRoute<TaskDetailPage, TaskDetailPageModel>("task");
            return services;
        }
    }
}
