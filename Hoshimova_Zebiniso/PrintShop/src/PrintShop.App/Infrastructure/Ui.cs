using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite;
using PrintShop.Core.Models;

namespace PrintShop.App.Infrastructure;

/// <summary>Вспомогательные методы для сообщений пользователю.</summary>
public static class Ui
{
    public const string AppTitle = "Типография: заказы на печать";

    private static Window? Owner =>
        Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;

    public static void Info(string text) =>
        Show(text, MessageBoxButton.OK, MessageBoxImage.Information);

    public static void Warning(string text) =>
        Show(text, MessageBoxButton.OK, MessageBoxImage.Warning);

    public static void Error(string text) =>
        Show(text, MessageBoxButton.OK, MessageBoxImage.Error);

    public static bool Confirm(string text) =>
        Show(text, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static MessageBoxResult Show(string text, MessageBoxButton buttons, MessageBoxImage icon)
    {
        var owner = Owner;
        return owner != null && owner.IsLoaded
            ? MessageBox.Show(owner, text, AppTitle, buttons, icon)
            : MessageBox.Show(text, AppTitle, buttons, icon);
    }

    /// <summary>Выполняет действие, перехватывая исключения и показывая понятное сообщение.</summary>
    public static bool Safe(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            ShowException(ex);
            return false;
        }
    }

    public static void ShowException(Exception ex)
    {
        switch (ex)
        {
            case OrderValidationException v:
                Warning(v.Message);
                break;
            case SqliteException s:
                Error("Ошибка базы данных: " + s.Message);
                break;
            case IOException or UnauthorizedAccessException:
                Error("Ошибка работы с файлом: " + ex.Message);
                break;
            case FormatException or OverflowException:
                Warning("Неверный формат числа. Проверьте введённые значения.");
                break;
            default:
                Error("Непредвиденная ошибка: " + ex.Message);
                break;
        }
    }
}
