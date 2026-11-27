using System.IO;
using System.Windows;
using System.Windows.Threading;
using PrintShop.App.Infrastructure;
using PrintShop.App.ViewModels;
using PrintShop.App.Views;
using PrintShop.App.Views.Dialogs;
using PrintShop.Core.Data;
using PrintShop.Core.Services;

namespace PrintShop.App;

public partial class App : Application
{
    /// <summary>Сервисы приложения (БД, репозитории, бизнес-логика).</summary>
    public static AppServices Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Глобальный перехват исключений: программа не должна аварийно завершаться
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) Ui.ShowException(ex);
        };

        DialogProvider.Service = new DialogService();

        try
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "Data", "printshop.db");
            Services = new AppServices(dbPath);
            if (Services.Database.WasCreated)
                DataSeeder.Seed(Services);
        }
        catch (Exception ex)
        {
            Ui.Error("Не удалось открыть базу данных:\n" + ex.Message);
            Shutdown(1);
            return;
        }

        ShowLogin();
    }

    /// <summary>Показывает окно входа; после успешного входа открывает главное окно.</summary>
    public void ShowLogin()
    {
        var login = new LoginWindow();
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        var main = new MainWindow();
        MainWindow = main;
        main.Closed += (_, _) =>
        {
            if (main.IsLogout) ShowLogin();
            else Shutdown();
        };
        main.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Ui.ShowException(e.Exception);
        e.Handled = true;
    }
}
