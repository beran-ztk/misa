using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten.Forms;

public sealed partial class CreateTopicViewModel(Guid? parentId, ZettelkastenGateway gateway)
    : ViewModelBase, IHostedForm<Result>
{
    [ObservableProperty] private string _topicTitle = string.Empty;

    public async Task<Result<Result>> SubmitAsync()
    {
        if (string.IsNullOrEmpty(TopicTitle))
            return Result<Result>.Failure();
        
        var request = new CreateTopicRequest(TopicTitle, parentId);
        var result = await gateway.CreateTopicAsync(request);

        if (!result.IsSuccess)
            return Result<Result>.Failure();

        return Result<Result>.Ok(Result.Ok());
    }
}