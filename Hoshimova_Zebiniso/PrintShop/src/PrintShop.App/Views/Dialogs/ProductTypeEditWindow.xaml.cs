using System.Windows;
using PrintShop.App.Infrastructure;
using PrintShop.App.ViewModels;
using PrintShop.Core.Models;

namespace PrintShop.App.Views.Dialogs;

public partial class ProductTypeEditWindow : Window
{
    private readonly ProductType? _source;

    public ProductTypeEditWindow(ProductType? productType)
    {
        InitializeComponent();
        _source = productType;
        Title = productType == null ? "Новый вид продукции" : "Редактирование вида продукции";
        KindBox.ItemsSource = Enum.GetValues<ProductKind>().Select(k => new Option<ProductKind>(k, EnumNames.Of(k))).ToList();
        KindBox.SelectedValue = productType?.Kind ?? ProductKind.Flyer;
        if (productType != null)
        {
            NameBox.Text = productType.Name;
            PriceBox.Text = Input.Format(productType.BasePrice);
            UnitBox.Text = productType.Unit;
        }
        Loaded += (_, _) => NameBox.Focus();
    }

    public ProductType? Result { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Ui.Safe(() =>
        {
            Result = new ProductType
            {
                Id = _source?.Id ?? 0,
                Name = NameBox.Text,
                Kind = (ProductKind)KindBox.SelectedValue,
                BasePrice = Input.Decimal(PriceBox.Text, "Базовая цена"),
                Unit = UnitBox.Text
            };
            DialogResult = true;
        });
    }
}
