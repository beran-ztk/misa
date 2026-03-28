using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Misa.Core.Features.Items.Zettelkasten;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

/// <summary>
/// Single source of truth for building the knowledge item tree.
/// Converts server DTOs into <see cref="KnowledgeIndexNodeVm"/> trees that both the
/// main index and the trash view consume.
/// </summary>
internal static class KnowledgeTreeBuilder
{
    /// <summary>
    /// Converts a pre-ordered hierarchical DTO list (as returned by the server for active items)
    /// into a tree of node view-models.
    /// </summary>
    public static IReadOnlyList<KnowledgeIndexNodeVm> FromHierarchical(
        IEnumerable<KnowledgeIndexEntryDto> roots,
        Func<Guid, string, Task>? onRename)
        => roots.Select(dto => ConvertNode(dto, onRename)).ToList();

    private static KnowledgeIndexNodeVm ConvertNode(
        KnowledgeIndexEntryDto dto,
        Func<Guid, string, Task>? onRename)
    {
        var vm = new KnowledgeIndexNodeVm(onRename)
        {
            Id         = dto.Id,
            Workflow   = dto.Workflow,
            Title      = dto.Title,
            ParentId   = dto.ParentId,
            IsExpanded = dto.IsExpanded
        };

        foreach (var child in dto.Children)
            vm.Children.Add(ConvertNode(child, onRename));

        return vm;
    }

    /// <summary>
    /// Builds a tree from a flat list of deleted items, linking each node to its parent
    /// via <see cref="KnowledgeIndexEntryDto.ParentId"/>.
    /// Items whose parent is absent from the list (or has no parent) become roots.
    /// </summary>
    public static IReadOnlyList<KnowledgeIndexNodeVm> FromDeletedFlat(
        IEnumerable<DeletedKnowledgeEntryDto> flat)
    {
        var all = flat.ToList();

        var byId = all.ToDictionary(d => d.Id, d => new KnowledgeIndexNodeVm(null)
        {
            Id         = d.Id,
            Workflow   = d.Workflow,
            Title      = d.Title,
            ParentId   = d.ParentId,
            DeletedAt  = d.DeletedAt,
            IsExpanded = true   // expand by default so the tree structure is visible
        });

        var roots = new List<KnowledgeIndexNodeVm>();

        foreach (var dto in all)
        {
            var vm = byId[dto.Id];

            if (dto.ParentId.HasValue && byId.TryGetValue(dto.ParentId.Value, out var parent))
                parent.Children.Add(vm);
            else
                roots.Add(vm);
        }

        return roots;
    }
}
