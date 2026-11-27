using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Application.Bookings;
using TravelAgency.Application.Common.Diagnostics;
using TravelAgency.Application.Documents;
using TravelAgency.Application.Interfaces;
using TravelAgency.Infrastructure.Authentication;
using TravelAgency.Infrastructure.Documents;
using TravelAgency.Infrastructure.Persistence;
using TravelAgency.Infrastructure.Repositories;
using TravelAgency.Infrastructure.Storage;
using TravelAgency.Infrastructure.Messaging;
using MassTransit;

namespace TravelAgency.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TravelAgencyDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(TravelAgencyDbContext).Assembly.FullName)));

        
        services.AddScoped<ITourRepository, TourRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        services.AddScoped<IBookingApplicationService, BookingApplicationService>();
        services.AddSingleton<IFileStorageService, LocalObjectStorageService>();
        services.AddSingleton<IVoucherPdfGenerator, QuestPdfVoucherGenerator>();
        services.AddScoped<IDocubinetApplicationService, DocubinetApplicationService>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            options.InstanceName = "TravelAgency:";
        });
        services.AddScoped<TourRepository>();
        services.AddScoped<ITourRepository, CachedTourRepository>();
        services.AddSingleton<TravelAgencyMetrics>();

        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.AddConsumer<GenerateVoucherOnBookingCreatedConsumer>();

            busConfigurator.UsingRabbitMq((context, cfg) =>
            {
                var rabbitHost = configuration["RabbitMq:Host"] ?? "localhost";
                var rabbitUser = configuration["RabbitMq:Username"] ?? "admin";
                var rabbitPass = configuration["RabbitMq:Password"] ?? "admin";

                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });
                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}