using MassTransit;
using TravelAgency.Notifications.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();

    // Регистрируем наш Consumer уведомлений
    busConfigurator.AddConsumer<SendNotificationOnBookingCreatedConsumer>();

    busConfigurator.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var rabbitUser = builder.Configuration["RabbitMq:Username"] ?? "travelagency";
        var rabbitPass = builder.Configuration["RabbitMq:Password"] ?? "travelagency123";

        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));

        // MassTransit создаст ОТДЕЛЬНУЮ очередь send-notification-on-booking-created
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();