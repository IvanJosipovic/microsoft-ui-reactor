using Duct.Interop.WinForms;

namespace WinFormsInterop.Sample;

partial class WinFormsOutsideForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        leftPanel = new Panel();
        title = new Label();
        description = new Label();
        inputLabel = new Label();
        textBox = new TextBox();
        button = new Button();
        logLabel = new Label();
        logList = new ListBox();
        splitter = new Splitter();
        island = new XamlIslandControl();
        leftPanel.SuspendLayout();
        SuspendLayout();

        // ── Left panel ──────────────────────────────────────────────
        leftPanel.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
        leftPanel.Dock = DockStyle.Left;
        leftPanel.Padding = new Padding(12);
        leftPanel.Width = 300;
        leftPanel.Controls.Add(logList);
        leftPanel.Controls.Add(logLabel);
        leftPanel.Controls.Add(button);
        leftPanel.Controls.Add(textBox);
        leftPanel.Controls.Add(inputLabel);
        leftPanel.Controls.Add(description);
        leftPanel.Controls.Add(title);

        // title
        title.AutoSize = false;
        title.Dock = DockStyle.Top;
        title.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
        title.ForeColor = System.Drawing.Color.White;
        title.Height = 36;
        title.Text = "WinForms Controls";

        // description
        description.AutoSize = false;
        description.Dock = DockStyle.Top;
        description.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
        description.Height = 80;
        description.Text = "This panel is native WinForms.\r\n\r\nThe right side is a XAML Island\r\nhosting a Duct component tree\r\nwith WinUI controls.";

        // inputLabel
        inputLabel.Dock = DockStyle.Top;
        inputLabel.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
        inputLabel.Height = 20;
        inputLabel.Text = "WinForms TextBox:";

        // textBox
        textBox.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Dock = DockStyle.Top;
        textBox.ForeColor = System.Drawing.Color.White;
        textBox.Text = "Type here (WinForms)";

        // button
        button.BackColor = System.Drawing.Color.FromArgb(0, 120, 212);
        button.Dock = DockStyle.Top;
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = System.Drawing.Color.White;
        button.Height = 35;
        button.Margin = new Padding(0, 8, 0, 0);
        button.Text = "WinForms Button \u2014 Click Me";

        // logLabel
        logLabel.Dock = DockStyle.Top;
        logLabel.ForeColor = System.Drawing.Color.FromArgb(140, 140, 140);
        logLabel.Height = 24;
        logLabel.Text = "Event Log:";

        // logList
        logList.BackColor = System.Drawing.Color.FromArgb(25, 25, 25);
        logList.BorderStyle = BorderStyle.None;
        logList.Dock = DockStyle.Fill;
        logList.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);

        // ── Splitter ────────────────────────────────────────────────
        splitter.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
        splitter.Dock = DockStyle.Left;
        splitter.Width = 4;

        // ── XAML Island ─────────────────────────────────────────────
        island.ComponentType = typeof(SampleDuctComponent);
        island.Dock = DockStyle.Fill;

        // ── Form ────────────────────────────────────────────────────
        BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
        ClientSize = new System.Drawing.Size(934, 561);
        ForeColor = System.Drawing.Color.White;
        Text = "WinForms hosts Duct";
        Controls.Add(island);
        Controls.Add(splitter);
        Controls.Add(leftPanel);

        leftPanel.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Panel leftPanel;
    private Label title;
    private Label description;
    private Label inputLabel;
    private TextBox textBox;
    private Button button;
    private Label logLabel;
    private ListBox logList;
    private Splitter splitter;
    private XamlIslandControl island;
}
