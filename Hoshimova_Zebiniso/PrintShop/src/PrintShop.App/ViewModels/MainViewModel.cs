using System.Collections.ObjectModel;
using PrintShop.App.Infrastructure;
using PrintShop.Core.Services;

namespace PrintShop.App.ViewModels;

/// <summary>Пункт бокового меню главного окна.</summary>
public class NavItem
{
    public NavItem(string icon, SectionViewModel viewModel)
    {
        Icon = icon;
        ViewModel = viewModel;
    }

    public string Icon { get; }
    public SectionViewModel ViewModel { get; }
    public string Title => ViewModel.Title;
}

/// <summary>ViewModel главного окна: меню разделов (с учётом роли), текущий раздел, резервное копирование.</summary>
public class MainViewModel : ObservableObject
{
    private readonly AppServices _app;
    private NavItem? _selected;

    public MainViewModel(AppServices app)
    {
        _app = app;
        var all = new (AppSection Section, string Icon, Func<SectionViewModel> Create)[]
        {
            (AppSection.Orders, "📋", () => new OrdersViewModel(app)),
            (AppSection.Customers, "👥", () => new CustomersViewModel(app)),
            (AppSection.Products, "🏷", () => new ProductsViewModel(app)),
            (AppSection.Materials, "📦", () => new MaterialsViewModel(app)),
            (AppSection.Reports, "📊", () => new ReportsViewModel(app)),
            (AppSection.Users, "🔑", () => new UsersViewModel(app)),
        };
        // Пункты меню показываются в зависимости от роли пользователя
        foreach (var (section, icon, create) in all.Where(x => app.Auth.CanAccess(x.Section)))
            Sections.Add(new NavItem(icon, create()));

        BackupCommand = new RelayCommand(Backup, () => app.Auth.Has(AppPermission.Backup));
        RestoreCommand = new RelayCommand(Restore, () => app.Auth.Has(AppPermission.Backup));

        Selected = Sections.FirstOrDefault();
    }

    public ObservableCollection<NavItem> Sections { get; } = new();

    public NavItem? Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value) && value != null)
                Ui.Safe(value.ViewModel.Refresh);
        }
    }

    public string UserName => _app.Auth.CurrentUser?.FullName ?? "";
    public string UserRole => _app.Auth.CurrentUser?.RoleName ?? "";
    public bool CanBackup => _app.Auth.Has(AppPermission.Backup);

    public RelayCommand BackupCommand { get; }
    public RelayCommand RestoreCommand { get; }

    private void Backup()
    {
        var path = DialogProvider.Service.SaveFile("База данных SQLite (*.db)|*.db", _app.Backup.SuggestFileName());
        if (path == null) return;
        _app.Backup.Backup(path);
        Ui.Info($"Резервная копия базы данных сохранена:\n{path}");
    }

    private void Restore()
    {
        var path = DialogProvider.Service.OpenFile("База данных SQLite (*.db)|*.db");
        if (path == null) return;
        if (!Ui.Confirm("Текущие данные будут заменены данными из резервной копии.\n" +
                        "Перед восстановлением будет автоматически создана копия текущей базы.\n\nПродолжить?"))
            return;
        _app.Backup.Restore(path);
        Selected?.ViewModel.Refresh();
        Ui.Info("База данных восстановлена из резервной копии.");
    }
}
