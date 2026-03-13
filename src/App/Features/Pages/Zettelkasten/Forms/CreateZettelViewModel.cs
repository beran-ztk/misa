using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten.Forms;

public sealed partial class CreateZettelViewModel(Guid? topicId, string description, ZettelkastenGateway gateway)
    : ViewModelBase, IHostedForm<Result>
{
    [ObservableProperty] private string _zettelTitle = string.Empty;
    [ObservableProperty] private string? _zettelContent;

    public string FormTitle { get; } = "Create Zettel";
    public string? FormDescription { get; } = description;

    public async Task<Result<Result>> SubmitAsync()
    {
        if (string.IsNullOrEmpty(ZettelTitle) || topicId is null)
            return Result<Result>.Failure();

        var request = new CreateZettelRequest(ZettelTitle, ZettelContent, topicId.Value);
        var result = await gateway.CreateZettelAsync(request);

        if (!result.IsSuccess)
            return Result<Result>.Failure();

        return Result<Result>.Ok(Result.Ok());
    }
}
