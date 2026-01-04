using System;

namespace Misa.Ui.Avalonia.Interfaces;

public interface IEntityDetail
{
    public Guid? SelectedEntity { get; set; }
    public void ReloadList();
}