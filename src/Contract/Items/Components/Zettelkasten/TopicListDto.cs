namespace Misa.Contract.Items.Components.Zettelkasten;

public record TopicListDto(Guid Id, Guid? ParentId, string Title)
{
    public List<TopicListDto> Children { get; } = [];
}