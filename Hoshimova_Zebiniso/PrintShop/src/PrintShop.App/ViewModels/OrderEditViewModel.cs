using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using PrintShop.App.Infrastructure;
using PrintShop.Core.Models;
using PrintShop.Core.Models.Products;
using PrintShop.Core.Services;

namespace PrintShop.App.ViewModels;

/// <summary>Флажок выбора доп. услуги в форме позиции.</summary>
public class ServiceChoice : ObservableObject
{
    private bool _isChecked;
    private readonly Action _changed;

    public ServiceChoice(AdditionalService service, Action changed)
    {
        Service = service;
        _changed = changed;
    }

    public AdditionalService Service { get; }
    public string Caption => $"{Service.Name} — {Service.Price:0.00} сом {Service.ChargeTypeName}";

    public bool IsChecked
    {
        get => _isChecked;
        set { if (Set(ref _isChecked, value)) _changed(); }
    }
}

/// <summary>Строка таблицы позиций в форме заказа.</summary>
public record OrderItemRow(OrderItem Item, int Index)
{
    public string Description => Item.Description;
    public int Quantity => Item.Quantity;
    public string Services => Item.ServicesText;
    public decimal Discount => Item.Discount;
    public decimal Price => Item.Price;
}

/// <summary>ViewModel формы создания/редактирования заказа.</summary>
public class OrderEditViewModel : ObservableObject
{
    private readonly AppServices _app;
    private readonly IPriceCalculator _calc;

    private Customer? _customer;
    private DateTime _dueDate;
    private string _comment;
    private string _layoutPath;

    // Поля редактора позиции
    private ProductType? _productType;
    private Material? _material;
    private string? _format;
    private Option<ColorMode>? _color;
    private string _quantityText = "100";
    private string _pagesText = "8";
    private string _widthText = "1";
    private string _heightText = "1";
    private bool _dieCut;
    private string _preview = "";
    private bool _previewIsError;
    private OrderItemRow? _selectedItem;
    private OrderItem? _editingItem;

    public OrderEditViewModel(AppServices app, Order order)
    {
        _app = app;
        _calc = app.Calculator;
        Order = order;

        foreach (var c in app.Customers.GetAll()) Customers.Add(c);
        ProductTypes = app.ProductTypes.GetAll();
        _allMaterials = app.Materials.GetAll();
        Colors = Enum.GetValues<ColorMode>().Select(c => new Option<ColorMode>(c, EnumNames.Of(c))).ToList();
        foreach (var s in app.Services.GetAll()) Services.Add(new ServiceChoice(s, UpdatePreview));

        _customer = order.HasCustomer ? Customers.FirstOrDefault(c => c.Id == order.Customer.Id) : null;
        _dueDate = order.DueDate;
        _comment = order.Comment;
        _layoutPath = order.LayoutPath;
        _color = Colors[(int)ColorMode.ColorOneSide];

        AddItemCommand = new RelayCommand(AddOrUpdateItem);
        EditItemCommand = new RelayCommand(LoadItemToEditor, () => SelectedItem != null);
        RemoveItemCommand = new RelayCommand(RemoveItem, () => SelectedItem != null);
        ClearEditorCommand = new RelayCommand(ClearEditor);
        NewCustomerCommand = new RelayCommand(NewCustomer, () => app.Auth.Has(AppPermission.EditCustomers));
        BrowseLayoutCommand = new RelayCommand(BrowseLayout);
        OpenLayoutCommand = new RelayCommand(OpenLayout, () => File.Exists(LayoutPath));

        ProductType = ProductTypes.FirstOrDefault();
        RefreshItems();
    }

    public Order Order { get; }
    public string Title => Order.IsNew ? $"Новый заказ №{Order.Number}" : $"Заказ №{Order.Number} — редактирование";

    // ---------------- Шапка заказа ----------------

    public ObservableCollection<Customer> Customers { get; } = new();
    public Customer? Customer { get => _customer; set => Set(ref _customer, value); }
    public DateTime CreatedAt => Order.CreatedAt;
    public DateTime DueDate { get => _dueDate; set => Set(ref _dueDate, value); }
    public string Comment { get => _comment; set => Set(ref _comment, value); }
    public string LayoutPath
    {
        get => _layoutPath;
        set { if (Set(ref _layoutPath, value)) OnPropertyChanged(nameof(LayoutFileName)); }
    }
    public string LayoutFileName => string.IsNullOrEmpty(LayoutPath) ? "файл не прикреплён" : Path.GetFileName(LayoutPath);

    // ---------------- Позиции ----------------

    public ObservableCollection<OrderItemRow> Items { get; } = new();
    public OrderItemRow? SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }
    public decimal Discount => Order.Discount;
    public decimal Total => Order.TotalPrice;
    public decimal Subtotal => Order.TotalPrice + Order.Discount;

    // ---------------- Редактор позиции ----------------

    private readonly IReadOnlyList<Material> _allMaterials;
    public IReadOnlyList<ProductType> ProductTypes { get; }
    public ObservableCollection<Material> Materials { get; } = new();
    public ObservableCollection<string> Formats { get; } = new();
    public IReadOnlyList<Option<ColorMode>> Colors { get; }
    public ObservableCollection<ServiceChoice> Services { get; } = new();

    public ProductType? ProductType
    {
        get => _productType;
        set
        {
            if (!Set(ref _productType, value)) return;
            OnProductTypeChanged();
        }
    }

    public Material? Material { get => _material; set { if (Set(ref _material, value)) UpdatePreview(); } }
    public string? Format { get => _format; set { if (Set(ref _format, value)) UpdatePreview(); } }
    public Option<ColorMode>? Color { get => _color; set { if (Set(ref _color, value)) UpdatePreview(); } }
    public string QuantityText { get => _quantityText; set { if (Set(ref _quantityText, value)) UpdatePreview(); } }
    public string PagesText { get => _pagesText; set { if (Set(ref _pagesText, value)) UpdatePreview(); } }
    public string WidthText { get => _widthText; set { if (Set(ref _widthText, value)) UpdatePreview(); } }
    public string HeightText { get => _heightText; set { if (Set(ref _heightText, value)) UpdatePreview(); } }
    public bool DieCut { get => _dieCut; set { if (Set(ref _dieCut, value)) UpdatePreview(); } }

    public bool IsBooklet => ProductType?.Kind == ProductKind.Booklet;
    public bool IsBanner => ProductType?.Kind == ProductKind.Banner;
    public bool IsSticker => ProductType?.Kind == ProductKind.Sticker;
    public bool HasFormat => !IsBanner;

    public string Preview { get => _preview; private set => Set(ref _preview, value); }
    public bool PreviewIsError { get => _previewIsError; private set => Set(ref _previewIsError, value); }
    public string AddButtonText => _editingItem == null ? "＋ Добавить позицию" : "✔ Сохранить позицию";

    public RelayCommand AddItemCommand { get; }
    public RelayCommand EditItemCommand { get; }
    public RelayCommand RemoveItemCommand { get; }
    public RelayCommand ClearEditorCommand { get; }
    public RelayCommand NewCustomerCommand { get; }
    public RelayCommand BrowseLayoutCommand { get; }
    public RelayCommand OpenLayoutCommand { get; }

    private void OnProductTypeChanged()
    {
        // Для баннера — только рулонные материалы, для остальных — листовые
        Materials.Clear();
        foreach (var m in _allMaterials.Where(m => m.IsRoll == IsBanner)) Materials.Add(m);
        _material = Materials.FirstOrDefault();
        OnPropertyChanged(nameof(Material));

        // Допустимые форматы берутся из объекта соответствующего класса (полиморфное свойство AllowedFormats)
        Formats.Clear();
        if (ProductType != null && _material != null)
        {
            var sample = ProductFactory.Create(ProductType, _material);
            foreach (var f in sample.AllowedFormats) Formats.Add(f);
            _format = sample.Format;
        }
        OnPropertyChanged(nameof(Format));

        foreach (var n in new[] { nameof(IsBooklet), nameof(IsBanner), nameof(IsSticker), nameof(HasFormat) })
            OnPropertyChanged(n);
        UpdatePreview();
    }

    /// <summary>Создание объекта изделия по данным редактора.</summary>
    private PrintProduct BuildProduct()
    {
        if (ProductType == null) throw new OrderValidationException("Выберите вид продукции.");
        if (Material == null) throw new OrderValidationException("Нет подходящего материала. Добавьте материал в справочник.");

        var p = ProductFactory.Create(ProductType, Material);
        p.Quantity = Input.Int(QuantityText, "Тираж");
        if (HasFormat && Format != null) p.Format = Format;
        if (Color != null && !IsBanner) p.Color = Color.Value;

        switch (p) // сопоставление с образцом по типу изделия
        {
            case Booklet b:
                b.PageCount = Input.Int(PagesText, "Количество страниц");
                break;
            case Banner bn:
                bn.Width = Input.Decimal(WidthText, "Ширина");
                bn.Height = Input.Decimal(HeightText, "Высота");
                break;
            case Sticker st:
                st.DieCut = DieCut;
                break;
        }
        foreach (var s in Services.Where(s => s.IsChecked)) p.AddService(s.Service);
        p.CalculateMaterialUsage(); // проверка совместимости формата и материала
        return p;
    }

    private void UpdatePreview()
    {
        try
        {
            var p = BuildProduct();
            var b = _calc.CalculateItem(p);
            var usage = p.CalculateMaterialUsage();
            Preview = $"Печать и материал: {b.PrintAndMaterial:N2} сом   (материал: {usage:0.##} {p.Material.Unit})\n" +
                      $"Доп. услуги: {b.Services:N2} сом\n" +
                      $"Скидка за тираж: {b.DiscountPercent:0}% (−{b.DiscountAmount:N2} сом)\n" +
                      $"Стоимость позиции: {b.Total:N2} сом   •   за 1 шт: {(p.Quantity > 0 ? b.Total / p.Quantity : 0):N2} сом";
            PreviewIsError = false;
        }
        catch (Exception ex) when (ex is OrderValidationException or FormatException or OverflowException)
        {
            Preview = "⚠ " + ex.Message;
            PreviewIsError = true;
        }
    }

    private void AddOrUpdateItem()
    {
        var product = BuildProduct();
        if (_editingItem != null)
            Order.RemoveItem(_editingItem);
        Order.AddItem(new OrderItem(product));
        ClearEditor();
        RefreshItems();
    }

    private void LoadItemToEditor()
    {
        var item = SelectedItem!.Item;
        var p = item.Product;
        ProductType = ProductTypes.FirstOrDefault(t => t.Id == p.ProductType.Id);
        Material = Materials.FirstOrDefault(m => m.Id == p.Material.Id) ?? Materials.FirstOrDefault();
        if (HasFormat) Format = p.Format;
        Color = Colors.First(c => c.Value == p.Color);
        QuantityText = p.Quantity.ToString();
        if (p is Booklet b) PagesText = b.PageCount.ToString();
        if (p is Banner bn) { WidthText = Input.Format(bn.Width); HeightText = Input.Format(bn.Height); }
        if (p is Sticker st) DieCut = st.DieCut;
        foreach (var s in Services) s.IsChecked = p.Services.Any(x => x.Id == s.Service.Id);
        _editingItem = item;
        OnPropertyChanged(nameof(AddButtonText));
        UpdatePreview();
    }

    private void ClearEditor()
    {
        _editingItem = null;
        foreach (var s in Services) s.IsChecked = false;
        DieCut = false;
        OnPropertyChanged(nameof(AddButtonText));
        UpdatePreview();
    }

    private void RemoveItem()
    {
        var item = SelectedItem!.Item;
        if (!Ui.Confirm($"Удалить позицию «{item.Description}»?")) return;
        if (_editingItem == item) ClearEditor();
        Order.RemoveItem(item);
        RefreshItems();
    }

    private void RefreshItems()
    {
        _calc.Recalculate(Order);
        Items.Clear();
        int i = 1;
        foreach (var item in Order.Items) Items.Add(new OrderItemRow(item, i++));
        OnPropertyChanged(nameof(Discount));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Subtotal));
    }

    private void NewCustomer()
    {
        var c = DialogProvider.Service.EditCustomer(null);
        if (c == null) return;
        _app.Customers.Add(c);
        Customers.Add(c);
        Customer = c;
    }

    private void BrowseLayout()
    {
        var path = DialogProvider.Service.OpenFile("Макеты (*.pdf;*.jpg;*.png;*.tif;*.cdr;*.ai;*.psd)|*.pdf;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.cdr;*.ai;*.psd|Все файлы (*.*)|*.*");
        if (path == null) return;

        // Копируем макет в папку программы, чтобы он не потерялся
        var dir = Path.Combine(AppContext.BaseDirectory, "Data", "Layouts", Order.Number);
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, Path.GetFileName(path));
        if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            File.Copy(path, target, overwrite: true);
        LayoutPath = target;
    }

    private void OpenLayout() =>
        Process.Start(new ProcessStartInfo(LayoutPath) { UseShellExecute = true });

    /// <summary>Перенос данных формы в заказ и сохранение. true — успешно.</summary>
    public bool Save()
    {
        return Ui.Safe(() =>
        {
            Order.Customer = Customer ?? throw new OrderValidationException("Выберите клиента.");
            Order.DueDate = DueDate;
            Order.Comment = Comment?.Trim() ?? "";
            Order.LayoutPath = LayoutPath ?? "";
            _app.Orders.Save(Order);
        });
    }
}
