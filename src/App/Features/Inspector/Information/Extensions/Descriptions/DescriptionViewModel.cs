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
using Misa.Ui.Avalonia.Common.Mapping;
using Misa.Ui.Avalonia.Features.Inspector.Information.Base;

namespace Misa.Ui.Avalonia.Features.Inspector.Information.Extensions.Descriptions;

public partial class DescriptionViewModel(DetailInformationViewModel parent) : ViewModelBase
{
    public DetailInformationViewModel Parent { get; } = parent;

    public ObservableCollection<DescriptionItemViewModel> Descriptions { get; } = [];

    [ObservableProperty] private string _description = string.Empty;

    public bool IsDescriptionValid => !string.IsNullOrWhiteSpace(Description);

    partial void OnDescriptionChanged(string value)
        => OnPropertyChanged(nameof(IsDescriptionValid));

    public void Load()
    {
        Descriptions.Clear();

        foreach (var d in Parent.Parent.Item.Entity.Descriptions
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
            var dto = new DescriptionCreateDto(Parent.Parent.Item.Id, Description.Trim());

            using var request = new HttpRequestMessage(HttpMethod.Post, "entities/description")
            {
                Content = JsonContent.Create(dto)
            };

            using var response = await Parent.Parent.NavigationService.NavigationStore
                .HttpClient
                .SendAsync(request, CancellationToken.None);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Result<DescriptionDto>>();
            if (result?.Value is null)
                return;

            // Schutz: falls der gleiche Eintrag schon existiert, nicht nochmal hinzufügen
            if (Descriptions.Any(x => x.Id == result.Value.Id))
                return;

            Descriptions.Add(new DescriptionItemViewModel(this, result.Value));
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

        var newText = EditText.Trim();

        var dto = new DescriptionUpdateDto(
            Id: Id,
            Content: newText
        );

        using var request = new HttpRequestMessage(HttpMethod.Put, "entities/description")
        {
            Content = JsonContent.Create(dto)
        };

        using var response = await Parent.Parent.Parent.NavigationService.NavigationStore
            .HttpClient
            .SendAsync(request, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        Content = newText;
        IsEditOpen = false;
        EditText = newText;
    }

    [RelayCommand]
    private async Task Delete()
    {
        using var response = await Parent.Parent.Parent.NavigationService.NavigationStore
            .HttpClient
            .DeleteAsync($"entities/description/{Id}", CancellationToken.None);

        response.EnsureSuccessStatusCode();

        Parent.Descriptions.Remove(this);
    }
}
