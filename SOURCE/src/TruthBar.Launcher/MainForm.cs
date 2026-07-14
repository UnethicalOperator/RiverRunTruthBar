using System.Drawing;

namespace TruthBar.Launcher;

internal sealed class MainForm : Form
{
    private readonly Label _statusLabel;
    private readonly ProgressBar _progress;
    private readonly TextBox _details;
    private readonly Button _actionButton;
    private bool _running;

    public MainForm()
    {
        Text = "RiverRunTruthBar — by PlsDntChase";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(640, 380);
        MinimumSize = new Size(656, 419);
        MaximizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(24, 22, 22);
        ForeColor = Color.WhiteSmoke;

        var title = new Label
        {
            Text = "RiverRunTruthBar 2.2",
            Font = new Font(Font.FontFamily, 22f, FontStyle.Bold),
            ForeColor = Color.FromArgb(236, 75, 75),
            AutoSize = true,
            Location = new Point(24, 20)
        };
        Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Installs the verified IL2CPP mod and launches Liar's Bar — no injector needed.",
            AutoSize = true,
            Location = new Point(27, 65),
            ForeColor = Color.Gainsboro
        };
        Controls.Add(subtitle);

        var credit = new Label
        {
            Text = "Created by PlsDntChase / RiverRunCartel",
            AutoSize = true,
            Location = new Point(27, 86),
            ForeColor = Color.FromArgb(236, 75, 75)
        };
        Controls.Add(credit);

        _statusLabel = new Label
        {
            Text = "Preparing…",
            AutoEllipsis = true,
            Location = new Point(27, 112),
            Size = new Size(586, 24)
        };
        Controls.Add(_statusLabel);

        _progress = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25,
            Location = new Point(27, 140),
            Size = new Size(586, 20)
        };
        Controls.Add(_progress);

        _details = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(35, 32, 32),
            ForeColor = Color.Gainsboro,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(27, 174),
            Size = new Size(586, 135)
        };
        Controls.Add(_details);

        _actionButton = new Button
        {
            Text = "Install & Launch",
            Enabled = false,
            Location = new Point(423, 326),
            Size = new Size(190, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(126, 31, 31),
            ForeColor = Color.White
        };
        _actionButton.FlatAppearance.BorderColor = Color.FromArgb(210, 75, 75);
        _actionButton.Click += ActionButton_Click;
        Controls.Add(_actionButton);

        var hotkeys = new Label
        {
            Text = "In game: F1 menu  •  L refresh  •  P reset  •  F2 close",
            AutoSize = true,
            Location = new Point(27, 336),
            ForeColor = Color.Silver
        };
        Controls.Add(hotkeys);

        Shown += MainForm_Shown;
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        await RunInstallAsync();
    }

    private async void ActionButton_Click(object? sender, EventArgs e)
    {
        await RunInstallAsync();
    }

    private async Task RunInstallAsync()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _actionButton.Enabled = false;
        _progress.Style = ProgressBarStyle.Marquee;
        _progress.MarqueeAnimationSpeed = 25;
        _details.Clear();
        var progress = new Progress<string>(message =>
        {
            _statusLabel.Text = message;
            _details.AppendText(message + Environment.NewLine);
        });

        try
        {
            var result = await Task.Run(() => LauncherService.InstallAndLaunch(progress));
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = 100;
            _statusLabel.Text = "Installed, verified, and sent to Steam.";
            _details.AppendText(Environment.NewLine);
            _details.AppendText($"Game: {result.GameRoot}{Environment.NewLine}");
            _details.AppendText($"Files written: {result.Written}; already current: {result.Unchanged}; backed up: {result.BackedUp}{Environment.NewLine}");
            _details.AppendText($"Plugin SHA-256: {result.PluginHash}{Environment.NewLine}");
            _actionButton.Text = "Launch Again";
        }
        catch (Exception ex)
        {
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = 0;
            _statusLabel.Text = "Could not finish safely.";
            _details.AppendText(Environment.NewLine + ex.Message + Environment.NewLine);
            _actionButton.Text = "Retry";
        }
        finally
        {
            _running = false;
            _actionButton.Enabled = true;
        }
    }
}
