namespace RestaurantApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var instance = new Mutex(true, "RestaurantApp_2026", out bool first);
        if (!first)
        {
            MessageBox.Show("Программа уже запущена.", "Ресторан");
            return;
        }

        try
        {
            Application.Run(new MainForm(RestaurantStore.Load()));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or System.Text.Json.JsonException or InvalidDataException)
        {
            MessageBox.Show("Не удалось открыть данные ресторана.\n" + error.Message,
                "Ресторан", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
