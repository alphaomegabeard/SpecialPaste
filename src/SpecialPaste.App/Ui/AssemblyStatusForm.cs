using SpecialPaste.Core;

namespace SpecialPaste.Ui;

public sealed class AssemblyStatusForm : Form
{
    private readonly PartsAssemblyService _service;
    private readonly ListView _list;

    public AssemblyStatusForm(PartsAssemblyService service)
    {
        _service = service;
        Text = "Special Paste - Assembly Status";
        Width = 700;
        Height = 400;

        _list = new ListView
        {
            View = View.Details,
            Dock = DockStyle.Fill,
            FullRowSelect = true
        };
        _list.Columns.Add("Package ID", 420);
        _list.Columns.Add("Received", 100);
        _list.Columns.Add("Total", 100);

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft };
        var refresh = new Button { Text = "Refresh" };
        refresh.Click += (_, _) => Reload();
        var clearSelected = new Button { Text = "Clear Selected" };
        clearSelected.Click += (_, _) => ClearSelected();
        var clearAll = new Button { Text = "Clear All" };
        clearAll.Click += (_, _) => { _service.ClearCache(); Reload(); };

        buttonPanel.Controls.Add(clearAll);
        buttonPanel.Controls.Add(clearSelected);
        buttonPanel.Controls.Add(refresh);

        Controls.Add(_list);
        Controls.Add(buttonPanel);

        Reload();
    }

    private void Reload()
    {
        _list.Items.Clear();
        foreach (var status in _service.ListStatus())
        {
            var item = new ListViewItem(status.packageId);
            item.SubItems.Add(status.received.ToString());
            item.SubItems.Add(status.total.ToString());
            _list.Items.Add(item);
        }
    }

    private void ClearSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        var id = _list.SelectedItems[0].Text;
        _service.ClearCache(id);
        Reload();
    }
}
