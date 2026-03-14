using Misa.Contract.Items;

namespace Misa.Contract.Items.Components.Relations;

public sealed record ItemLookupDto(Guid Id, string Title, WorkflowDto Workflow);
