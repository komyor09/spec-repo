using System.Collections.ObjectModel;
using PrintShop.App.Infrastructure;
using PrintShop.Core.Data;
using PrintShop.Core.Models;

namespace PrintShop.App.ViewModels;

/// <summary>
/// Обобщённая ViewModel для разделов-справочников: список с поиском, добавление, изменение, удаление.
/// Конкретные разделы задают способ редактирования и правила поиска.
/// </summary>
public abstract class CrudViewModel<T> : SectionViewModel where T : EntityBase
{
    private T? _selected;
    private string _searchText = "";

    protected CrudViewModel(IRepository<T> repository)
    {
        Repository = repository;
        AddCommand = new RelayCommand(Add, () => CanEdit);
        EditCommand = new RelayCommand(Edit, () => CanEdit && Selected != null);
        DeleteCommand = new RelayCommand(Delete, () => CanEdit && Selected != null);
        SearchCommand = new RelayCommand(Refresh);
    }

    protected IRepository<T> Repository { get; }

    public ObservableCollection<T> Items { get; } = new();

    public T? Selected
    {
        get => _selected;
        set { if (Set(ref _selected, value)) OnSelectedChanged(); }
    }

    public string SearchText
    {
        get => _searchText;
        set { if (Set(ref _searchText, value)) Refresh(); }
    }

    /// <summary>Есть ли у текущего пользователя право изменять данные раздела.</summary>
    public abstract bool CanEdit { get; }

    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand SearchCommand { get; }

    /// <summary>Открывает форму редактирования; null — пользователь отменил.</summary>
    protected abstract T? ShowEditor(T? entity);

    /// <summary>Условие поиска по тексту (пустая строка — показывать всё).</summary>
    protected abstract bool Matches(T entity, string text);

    /// <summary>Описание записи для подтверждения удаления.</summary>
    protected virtual string Describe(T entity) => entity.ToString() ?? "";

    protected virtual void OnSelectedChanged() { }

    protected virtual void Save(T entity)
    {
        if (entity.IsNew) Repository.Add(entity);
        else Repository.Update(entity);
    }

    protected virtual void Remove(T entity) => Repository.Delete(entity.Id);

    public override void Refresh()
    {
        var id = Selected?.Id;
        var text = SearchText.Trim();
        Items.Clear();
        foreach (var item in Repository.GetAll().Where(x => Matches(x, text)))
            Items.Add(item);
        Selected = Items.FirstOrDefault(x => x.Id == id);
    }

    private void Add()
    {
        var created = ShowEditor(null);
        if (created == null) return;
        Save(created);
        Refresh();
        Selected = Items.FirstOrDefault(x => x.Id == created.Id);
    }

    private void Edit()
    {
        var edited = ShowEditor(Selected);
        if (edited == null) return;
        Save(edited);
        Refresh();
    }

    private void Delete()
    {
        var entity = Selected!;
        if (!Ui.Confirm($"Удалить запись «{Describe(entity)}»?\nЭто действие нельзя отменить.")) return;
        Remove(entity);
        Refresh();
    }

    protected static bool Has(string? source, string text) =>
        source?.Contains(text, StringComparison.OrdinalIgnoreCase) == true;
}
