using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PrintShop.App.Infrastructure;

/// <summary>Базовый класс ViewModel с поддержкой INotifyPropertyChanged.</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

/// <summary>Команда для привязки кнопок к методам ViewModel.</summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute()) { }

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <summary>Все исключения перехватываются и показываются пользователю.</summary>
    public void Execute(object? parameter) => Ui.Safe(() => _execute(parameter));
}

/// <summary>Базовый класс для ViewModel разделов главного окна.</summary>
public abstract class SectionViewModel : ObservableObject
{
    public abstract string Title { get; }

    /// <summary>Загрузка/обновление данных раздела.</summary>
    public abstract void Refresh();
}
