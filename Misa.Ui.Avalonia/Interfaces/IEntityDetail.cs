using Misa.Contract.Entities;
using Misa.Contract.Items;

namespace Misa.Ui.Avalonia.Interfaces;

public interface IEntityDetail
{
    public ReadEntityDto? SelectedEntity { get; set; }
}