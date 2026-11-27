using System.Windows;
using PrintShop.App.Infrastructure;
using PrintShop.Core.Models;

namespace PrintShop.App.Views.Dialogs;

public partial class MaterialEditWindow : Window
{
    private readonly Material? _source;

    public MaterialEditWindow(Material? material)
    {
        InitializeComponent();
        _source = material;
        Title = material == null ? "Новый материал" : "Редактирование материала";
        FormatBox.ItemsSource = PaperFormats.MaterialFormats;
        FormatBox.SelectedItem = material?.Format ?? PaperFormats.A3;
        if (material != null)
        {
            NameBox.Text = material.Name;
            DensityBox.Text = material.Density.ToString();
            PriceBox.Text = Input.Format(material.Price);
            QuantityBox.Text = Input.Format(material.Quantity);
            MinBox.Text = Input.Format(material.MinQuantity);
        }
        Loaded += (_, _) => NameBox.Focus();
    }

    public Material? Result { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Ui.Safe(() =>
        {
            Result = new Material
            {
                Id = _source?.Id ?? 0,
                Name = NameBox.Text,
                Density = Input.Int(DensityBox.Text, "Плотность"),
                Format = FormatBox.SelectedItem as string ?? throw new OrderValidationException("Выберите формат."),
                Price = Input.Decimal(PriceBox.Text, "Цена"),
                Quantity = Input.Decimal(QuantityBox.Text, "Остаток"),
                MinQuantity = Input.Decimal(MinBox.Text, "Минимальный запас")
            };
            DialogResult = true;
        });
    }
}
