using AutoService.Data;
using AutoService.Services;
using AutoService.Services.Reports;
using AutoService.WinForms.Forms;
using AutoService.WinForms.UI;
using Microsoft.Extensions.DependencyInjection;

namespace AutoService.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // любые необработанные ошибки показываем сообщением, а не аварийным завершением
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Ui.ShowError(e.Exception);

        // ---- внедрение зависимостей: здесь «собирается» всё приложение ----
        var services = new ServiceCollection();
        var dbPath = Path.Combine(AppContext.BaseDirectory, "autoservice.db");
        services.AddAutoServiceData(dbPath);
        services.AddSingleton<ClientService>();
        services.AddSingleton<CatalogService>();
        services.AddSingleton<RepairRequestService>();
        services.AddSingleton<ReportService>();
        services.AddSingleton<AppServices>();
        services.AddTransient<MainForm>();

        using var provider = services.BuildServiceProvider();
        provider.InitializeAutoServiceDatabase();

        Application.Run(provider.GetRequiredService<MainForm>());
    }
}
