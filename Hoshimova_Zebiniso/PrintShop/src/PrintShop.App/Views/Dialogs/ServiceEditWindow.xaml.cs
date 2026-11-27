using System.Windows;
using PrintShop.App.Infrastructure;
using PrintShop.App.ViewModels;
using PrintShop.Core.Models;

namespace PrintShop.App.Views.Dialogs;

public partial class ServiceEditWindow : Window
{
    private readonly AdditionalService? _source;

    public ServiceEditWindow(AdditionalService? service)
    {
        InitializeComponent();
        _source = service;
        Title = service == null ? "Новая услуга" : "Редактирование услуги";
        ChargeBox.ItemsSource = Enum.GetValues<ServiceChargeType>()
            .Select(c => new Option<ServiceChargeType>(c, EnumNames.Of(c))).ToList();
        ChargeBox.SelectedValue = service?.ChargeType ?? ServiceChargeType.PerUnit;
        if (service != null)
        {
            NameBox.Text = service.Name;
            PriceBox.Text = Input.Format(service.Price);
        }
        Loaded += (_, _) => NameBox.Focus();
    }

    public AdditionalService? Result { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Ui.Safe(() =>
        {
            Result = new AdditionalService
            {
                Id = _source?.Id ?? 0,
                Name = NameBox.Text,
                Price = Input.Decimal(PriceBox.Text, "Цена"),
                ChargeType = (ServiceChargeType)ChargeBox.SelectedValue
            };
            DialogResult = true;
        });
    }
}
