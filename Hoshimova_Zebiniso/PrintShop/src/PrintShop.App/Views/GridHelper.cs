using PrintShop.App.Infrastructure;

namespace PrintShop.App.Views;

/// <summary>Общий обработчик двойного щелчка по таблице: выполняет команду «Изменить» у ViewModel раздела.</summary>
internal static class GridHelper
{
    public static void EditOnDoubleClick(object? dataContext)
    {
        var prop = dataContext?.GetType().GetProperty("EditCommand");
        if (prop?.GetValue(dataContext) is RelayCommand cmd && cmd.CanExecute(null))
            cmd.Execute(null);
    }
}