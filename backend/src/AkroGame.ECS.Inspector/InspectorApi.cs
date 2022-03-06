using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace AkroGame.ECS.Inspector
{
    public class InspectorApi
    {
        private readonly WebApplication app;

        private readonly InspectorConfig config;

        public InspectorApi(
            string[] args,
            InspectorConfig config,
            InspectorService inspectorService,
            ILoggerProvider logger
        )
        {
            this.config = config;

            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(logger);

            builder.Services.Configure<JsonOptions>(
                options =>
                {
                    options.SerializerOptions.IncludeFields = true;
                }
            );
            builder.Services.AddCors(
                options =>
                {
                    options.AddDefaultPolicy(
                        builder =>
                        {
                            // For now just enable everything
                            builder.AllowAnyHeader();
                            builder.AllowAnyMethod();
                            builder.AllowAnyOrigin();
                        }
                    );
                }
            );
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(
                options =>
                {
                    options.SwaggerDoc(
                        "v1",
                        new OpenApiInfo
                        {
                            Version = "v1",
                            Title = "Svelto ECS Inspector API",
                            Description = "Svelto ECS Inspector API"
                        }
                    );
                }
            );

            app = builder.Build();

            app.UseCors();
            app.UseSwagger();
            app.UseSwaggerUI();

            InspectorRoutes.RegisterRoutes(inspectorService, app);
        }

        public void Start()
        {
            app.RunAsync($"http://{config.BindHost}:{config.Port}");
        }
    }
}
