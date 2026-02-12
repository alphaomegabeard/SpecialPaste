using SpecialPaste.Models;

namespace SpecialPaste.Ui;

public sealed class SettingsForm : Form
{
    private readonly NumericUpDown _lineWidth;
    private readonly NumericUpDown _chunkSizeMb;
    private readonly CheckBox _compression;
    private readonly ComboBox _overwrite;

    public AppSettings UpdatedSettings { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        Text = "Special Paste Settings";
        Width = 420;
        Height = 260;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 2, Padding = new Padding(12) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        panel.Controls.Add(new Label { Text = "Base64 line width", AutoSize = true }, 0, 0);
        _lineWidth = new NumericUpDown { Minimum = 60, Maximum = 300, Value = settings.Base64LineWidth, Dock = DockStyle.Fill };
        panel.Controls.Add(_lineWidth, 1, 0);

        panel.Controls.Add(new Label { Text = "Chunk size (MB)", AutoSize = true }, 0, 1);
        _chunkSizeMb = new NumericUpDown { Minimum = 1, Maximum = 50, Value = Math.Max(1, settings.ChunkSizeBytes / (1024 * 1024)), Dock = DockStyle.Fill };
        panel.Controls.Add(_chunkSizeMb, 1, 1);

        panel.Controls.Add(new Label { Text = "Enable compression", AutoSize = true }, 0, 2);
        _compression = new CheckBox { Checked = settings.EnableCompression, Dock = DockStyle.Left };
        panel.Controls.Add(_compression, 1, 2);

        panel.Controls.Add(new Label { Text = "When file exists", AutoSize = true }, 0, 3);
        _overwrite = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        _overwrite.Items.AddRange(Enum.GetNames(typeof(OverwriteBehavior)));
        _overwrite.SelectedItem = settings.OverwriteBehavior.ToString();
        panel.Controls.Add(_overwrite, 1, 3);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => Save();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        panel.Controls.Add(buttons, 0, 4);
        panel.SetColumnSpan(buttons, 2);

        Controls.Add(panel);

        UpdatedSettings = settings;
    }

    private void Save()
    {
        UpdatedSettings = new AppSettings
        {
            Base64LineWidth = (int)_lineWidth.Value,
            ChunkSizeBytes = (int)_chunkSizeMb.Value * 1024 * 1024,
            EnableCompression = _compression.Checked,
            OverwriteBehavior = Enum.Parse<OverwriteBehavior>(_overwrite.SelectedItem?.ToString() ?? "RenameWithSuffix")
        };
    }
}
