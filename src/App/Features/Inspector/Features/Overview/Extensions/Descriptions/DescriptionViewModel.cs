using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Features;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Base;

namespace Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Extensions.Descriptions;

public partial class DescriptionViewModel(InspectorOverViewModel parent) : ViewModelBase
{
    public InspectorOverViewModel Parent { get; } = parent;

    public ObservableCollection<DescriptionItemViewModel> Descriptions { get; } = [];

    [ObservableProperty] private string _description = string.Empty;

    public bool IsDescriptionValid => !string.IsNullOrWhiteSpace(Description);

    partial void OnDescriptionChanged(string value)
        => OnPropertyChanged(nameof(IsDescriptionValid));

    public void Load()
    {
        Descriptions.Clear();

        foreach (var d in Parent.Parent.State.Item.Entity.Descriptions
                     .GroupBy(x => x.Id)
                     .Select(g => g.First()))
        {
            Descriptions.Add(new DescriptionItemViewModel(this, d));
        }
    }

    [RelayCommand]
    private async Task AddDescription()
    {
        if (!IsDescriptionValid)
            return;

        try
        {
            var dto = new DescriptionCreateDto(
                Parent.Parent.State.Item.Id,
                Description.Trim()
            );

            var created = await Parent.Parent.Gateway.CreateDescriptionAsync(dto);
            if (created is null)
                return;

            if (Descriptions.Any(x => x.Id == created.Id))
                return;

            Descriptions.Add(new DescriptionItemViewModel(this, created));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            Description = string.Empty;
        }
    }
}

public sealed partial class DescriptionItemViewModel : ViewModelBase
{
    private DescriptionViewModel Parent { get; }

    public Guid Id { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _content;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _editText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ToggleEditToolTip))]
    private bool _isEditOpen;

    public bool CanSave
        => IsEditOpen
           && !string.IsNullOrWhiteSpace(EditText)
           && EditText.Trim() != Content;

    public string ToggleEditToolTip => IsEditOpen ? "Cancel" : "Edit";

    public DescriptionItemViewModel(DescriptionViewModel parent, DescriptionDto dto)
    {
        Parent = parent;

        Id = dto.Id;
        CreatedAtUtc = dto.CreatedAtUtc;

        _content = dto.Content;
        _editText = dto.Content;
    }

    [RelayCommand]
    private void ToggleEdit()
    {
        if (IsEditOpen)
        {
            IsEditOpen = false;
            EditText = Content;
            return;
        }

        IsEditOpen = true;
        EditText = Content;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSave)
            return;

        try
        {
            var newText = EditText.Trim();

            var dto = new DescriptionUpdateDto(
                Id: Id,
                Content: newText
            );

            await Parent.Parent.Parent.Gateway.UpdateDescriptionAsync(dto);

            Content = newText;
            IsEditOpen = false;
            EditText = newText;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [RelayCommand]
    private async Task Delete()
    {
        try
        {
            await Parent.Parent.Parent.Gateway.DeleteDescriptionAsync(Id);
            Parent.Descriptions.Remove(this);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}