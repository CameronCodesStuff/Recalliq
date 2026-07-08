using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace RecallIQ.Setup;

public sealed class InstallerForm : Form
{
    private readonly string _solutionDir;
    private readonly string _slnFile;
    private readonly string _uiProject;
    private readonly string _logFile;
    private readonly List<StepInfo> _steps;
    private readonly CancellationTokenSource _cts = new();

    private Panel _sidePanel = null!;
    private Panel _contentPanel = null!;
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private RichTextBox _logBox = null!;
    private ProgressBar _progressBar = null!;
    private Label _stepLabel = null!;
    private Button _actionButton = null!;
    private Button _cancelButton = null!;
    private Button _logToggleButton = null!;
    private Panel _stepListPanel = null!;
    private Label[] _stepLabels = null!;
    private PictureBox[] _stepIcons = null!;
    private CheckBox _launchCheckBox = null!;
    private bool _logExpanded;
    private int _currentStep = -1;
    private string _binDir = "";
    private string _dotnetPath = "dotnet";
    private bool _buildSucceeded;
    private InstallerState _state = InstallerState.Ready;

    private static readonly Color BgDark = Color.FromArgb(18, 18, 24);
    private static readonly Color BgPanel = Color.FromArgb(24, 24, 36);
    private static readonly Color BgSide = Color.FromArgb(14, 14, 20);
    private static readonly Color AccentCyan = Color.FromArgb(0, 210, 255);
    private static readonly Color AccentGreen = Color.FromArgb(0, 230, 118);
    private static readonly Color AccentRed = Color.FromArgb(255, 82, 82);
    private static readonly Color AccentYellow = Color.FromArgb(255, 202, 40);
    private static readonly Color TextPrimary = Color.FromArgb(230, 230, 240);
    private static readonly Color TextSecondary = Color.FromArgb(140, 140, 165);
    private static readonly Color TextDim = Color.FromArgb(80, 80, 100);

    enum InstallerState { Ready, Running, Completed, Failed }

    record StepInfo(string Name, string Description);

    public InstallerForm()
    {
        _solutionDir = FindSolutionDir();
        _slnFile = Path.Combine(_solutionDir, "RecallIQ.sln");
        _uiProject = Path.Combine(_solutionDir, "RecallIQ.UI");
        _logFile = Path.Combine(_solutionDir, "recalliq-setup.log");

        _steps =
        [
            new("Environment", "Checking Windows version and architecture"),
            new(".NET SDK", "Locating .NET 8+ SDK installation"),
            new("Solution", "Verifying project files and structure"),
            new("Restore", "Downloading NuGet packages"),
            new("Build", "Compiling RecallIQ solution"),
            new("Setup", "Creating app directories and config"),
            new("Ready", "Installation complete")
        ];

        InitializeUI();
        File.WriteAllText(_logFile, $"[{DateTime.Now}] RecallIQ Setup started\r\n");
    }

    private static string FindSolutionDir()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6; i++)
        {
            if (File.Exists(Path.Combine(dir, "RecallIQ.sln")))
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return AppContext.BaseDirectory;
    }

    private void InitializeUI()
    {
        Text = "RecallIQ Setup";
        Size = new Size(780, 540);
        MinimumSize = new Size(700, 480);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgDark;
        ForeColor = TextPrimary;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        DoubleBuffered = true;

        _sidePanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = BgSide,
            Padding = new Padding(16, 20, 16, 20)
        };

        var logoLabel = new Label
        {
            Text = "RecallIQ",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = AccentCyan,
            AutoSize = false,
            Size = new Size(188, 40),
            Location = new Point(16, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _sidePanel.Controls.Add(logoLabel);

        var tagLine = new Label
        {
            Text = "AI-Powered File Search",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextSecondary,
            AutoSize = false,
            Size = new Size(188, 18),
            Location = new Point(16, 58),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _sidePanel.Controls.Add(tagLine);

        var divider = new Panel
        {
            Size = new Size(170, 1),
            Location = new Point(16, 86),
            BackColor = Color.FromArgb(40, 40, 60)
        };
        _sidePanel.Controls.Add(divider);

        _stepListPanel = new Panel
        {
            Location = new Point(16, 100),
            Size = new Size(188, _steps.Count * 36),
            BackColor = Color.Transparent
        };

        _stepLabels = new Label[_steps.Count];
        _stepIcons = new PictureBox[_steps.Count];
        for (int i = 0; i < _steps.Count; i++)
        {
            _stepIcons[i] = new PictureBox
            {
                Size = new Size(18, 18),
                Location = new Point(0, i * 36 + 2),
                BackColor = Color.Transparent
            };
            _stepIcons[i].Paint += (s, e) => PaintStepIcon(s as PictureBox, e);
            _stepListPanel.Controls.Add(_stepIcons[i]);

            _stepLabels[i] = new Label
            {
                Text = _steps[i].Name,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = TextDim,
                AutoSize = false,
                Size = new Size(160, 22),
                Location = new Point(26, i * 36),
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = i
            };
            _stepListPanel.Controls.Add(_stepLabels[i]);
        }
        _sidePanel.Controls.Add(_stepListPanel);

        var versionLabel = new Label
        {
            Text = "v1.0.0",
            Font = new Font("Segoe UI", 8f),
            ForeColor = TextDim,
            Dock = DockStyle.Bottom,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0)
        };
        _sidePanel.Controls.Add(versionLabel);

        Controls.Add(_sidePanel);

        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgDark,
            Padding = new Padding(28, 24, 28, 20)
        };

        _titleLabel = new Label
        {
            Text = "Welcome to RecallIQ Setup",
            Font = new Font("Segoe UI Semibold", 16f),
            ForeColor = TextPrimary,
            AutoSize = false,
            Size = new Size(500, 36),
            Location = new Point(28, 24)
        };
        _contentPanel.Controls.Add(_titleLabel);

        _subtitleLabel = new Label
        {
            Text = "This wizard will build and configure RecallIQ on your machine.\nEverything runs locally — no data leaves your computer.",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = TextSecondary,
            AutoSize = false,
            Size = new Size(500, 44),
            Location = new Point(28, 64)
        };
        _contentPanel.Controls.Add(_subtitleLabel);

        _stepLabel = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9f),
            ForeColor = AccentCyan,
            AutoSize = false,
            Size = new Size(480, 22),
            Location = new Point(30, 120),
            Visible = false
        };
        _contentPanel.Controls.Add(_stepLabel);

        _progressBar = new ProgressBar
        {
            Location = new Point(28, 146),
            Size = new Size(492, 6),
            Style = ProgressBarStyle.Continuous,
            Maximum = _steps.Count * 100,
            Visible = false
        };
        _contentPanel.Controls.Add(_progressBar);

        _logBox = new RichTextBox
        {
            Location = new Point(28, 168),
            Size = new Size(492, 0),
            BackColor = Color.FromArgb(12, 12, 18),
            ForeColor = TextSecondary,
            Font = new Font("Cascadia Code", 8.5f, FontStyle.Regular, GraphicsUnit.Point),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Visible = false,
            DetectUrls = false
        };
        _contentPanel.Controls.Add(_logBox);

        _logToggleButton = new Button
        {
            Text = "Show Log",
            FlatStyle = FlatStyle.Flat,
            Size = new Size(80, 28),
            Location = new Point(440, 116),
            BackColor = Color.Transparent,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8f),
            Cursor = Cursors.Hand,
            Visible = false
        };
        _logToggleButton.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 70);
        _logToggleButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 60);
        _logToggleButton.Click += (_, _) => ToggleLog();
        _contentPanel.Controls.Add(_logToggleButton);

        _launchCheckBox = new CheckBox
        {
            Text = "Launch RecallIQ when finished",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = TextSecondary,
            Checked = true,
            AutoSize = true,
            Location = new Point(28, 420),
            Visible = false,
            BackColor = Color.Transparent
        };
        _contentPanel.Controls.Add(_launchCheckBox);

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.Transparent,
            Padding = new Padding(28, 8, 28, 8)
        };

        _cancelButton = new Button
        {
            Text = "Cancel",
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 38),
            Dock = DockStyle.Left,
            BackColor = Color.Transparent,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 10f),
            Cursor = Cursors.Hand
        };
        _cancelButton.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 80);
        _cancelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 60);
        _cancelButton.Click += (_, _) => OnCancel();

        _actionButton = new Button
        {
            Text = "Install",
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 38),
            Dock = DockStyle.Right,
            BackColor = AccentCyan,
            ForeColor = Color.FromArgb(10, 10, 20),
            Font = new Font("Segoe UI Semibold", 10f),
            Cursor = Cursors.Hand
        };
        _actionButton.FlatAppearance.BorderSize = 0;
        _actionButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 230, 255);
        _actionButton.Click += (_, _) => OnAction();

        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_actionButton);
        _contentPanel.Controls.Add(buttonPanel);

        Controls.Add(_contentPanel);
    }

    private void PaintStepIcon(PictureBox? pb, PaintEventArgs e)
    {
        if (pb == null) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int idx = _stepListPanel.Controls.IndexOf(pb) / 2;
        var r = new Rectangle(2, 2, 14, 14);

        if (idx < _currentStep)
        {
            using var brush = new SolidBrush(AccentGreen);
            e.Graphics.FillEllipse(brush, r);
            using var pen = new Pen(BgSide, 2f);
            e.Graphics.DrawLine(pen, 5, 9, 7, 11);
            e.Graphics.DrawLine(pen, 7, 11, 12, 6);
        }
        else if (idx == _currentStep)
        {
            using var pen = new Pen(AccentCyan, 2f);
            e.Graphics.DrawEllipse(pen, r);
            using var brush = new SolidBrush(AccentCyan);
            e.Graphics.FillEllipse(brush, new Rectangle(5, 5, 8, 8));
        }
        else
        {
            using var pen = new Pen(TextDim, 1.5f);
            e.Graphics.DrawEllipse(pen, r);
        }
    }

    private void MarkStepFailed(int idx)
    {
        if (idx < 0 || idx >= _stepLabels.Length) return;
        _stepLabels[idx].ForeColor = AccentRed;
        _stepIcons[idx].Invalidate();
    }

    private void UpdateStepVisuals(int newStep)
    {
        _currentStep = newStep;
        for (int i = 0; i < _steps.Count; i++)
        {
            _stepLabels[i].ForeColor = i < newStep ? AccentGreen
                : i == newStep ? TextPrimary
                : TextDim;
            _stepLabels[i].Font = i == newStep
                ? new Font("Segoe UI Semibold", 9.5f)
                : new Font("Segoe UI", 9.5f);
            _stepIcons[i].Invalidate();
        }
    }

    private void AppendLog(string message, Color? color = null)
    {
        if (InvokeRequired) { Invoke(() => AppendLog(message, color)); return; }
        var c = color ?? TextSecondary;
        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.SelectionLength = 0;
        _logBox.SelectionColor = c;
        _logBox.AppendText(message + "\n");
        _logBox.ScrollToCaret();
        File.AppendAllText(_logFile, $"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
    }

    private void SetStatus(string text)
    {
        if (InvokeRequired) { Invoke(() => SetStatus(text)); return; }
        _stepLabel.Text = text;
        _subtitleLabel.Text = text;
    }

    private void SetProgress(int step, int pct)
    {
        if (InvokeRequired) { Invoke(() => SetProgress(step, pct)); return; }
        _progressBar.Value = Math.Min(step * 100 + pct, _progressBar.Maximum);
    }

    private void ToggleLog()
    {
        _logExpanded = !_logExpanded;
        _logBox.Visible = _logExpanded;
        _logBox.Size = _logExpanded ? new Size(492, 240) : new Size(492, 0);
        _logToggleButton.Text = _logExpanded ? "Hide Log" : "Show Log";
    }

    private async void OnAction()
    {
        if (_state == InstallerState.Completed)
        {
            if (_launchCheckBox.Checked && _buildSucceeded)
                LaunchApp();
            Close();
            return;
        }

        if (_state == InstallerState.Failed)
        {
            _state = InstallerState.Ready;
            _currentStep = -1;
            _logBox.Clear();
            _progressBar.Value = 0;
        }

        _state = InstallerState.Running;
        _actionButton.Enabled = false;
        _actionButton.BackColor = Color.FromArgb(60, 60, 80);
        _actionButton.Text = "Installing...";
        _stepLabel.Visible = true;
        _progressBar.Visible = true;
        _logToggleButton.Visible = true;

        try
        {
            await Task.Run(() => RunInstall(_cts.Token));
        }
        catch (OperationCanceledException)
        {
            SetStatus("Installation cancelled.");
            AppendLog("Cancelled by user.", AccentYellow);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            AppendLog($"EXCEPTION: {ex}", AccentRed);
        }

        Invoke(() =>
        {
            _actionButton.Enabled = true;
            if (_buildSucceeded)
            {
                _state = InstallerState.Completed;
                _actionButton.Text = "Finish";
                _actionButton.BackColor = AccentGreen;
                _titleLabel.Text = "Installation Complete";
                _launchCheckBox.Visible = true;
                UpdateStepVisuals(_steps.Count);
            }
            else
            {
                _state = InstallerState.Failed;
                _actionButton.Text = "Retry";
                _actionButton.BackColor = AccentRed;
                _titleLabel.Text = "Installation Failed";
                if (!_logExpanded) ToggleLog();
            }
        });
    }

    private void OnCancel()
    {
        if (_state == InstallerState.Running)
        {
            _cts.Cancel();
            return;
        }
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts.Cancel();
        base.OnFormClosing(e);
    }

    private void RunInstall(CancellationToken ct)
    {
        Step0_CheckWindows(ct);
        Step1_CheckDotnet(ct);
        Step2_CheckSolution(ct);
        Step3_Restore(ct);
        Step4_Build(ct);
        Step5_Setup(ct);
        Step6_Done(ct);
    }

    private void AdvanceStep(int idx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Invoke(() =>
        {
            UpdateStepVisuals(idx);
            _titleLabel.Text = _steps[idx].Name;
            SetStatus(_steps[idx].Description);
            SetProgress(idx, 0);
        });
    }

    private void Step0_CheckWindows(CancellationToken ct)
    {
        AdvanceStep(0, ct);
        AppendLog("Checking Windows version...");

        var os = Environment.OSVersion;
        if (os.Version.Major < 10)
        {
            AppendLog($"Windows {os.Version} detected — Windows 10+ required.", AccentRed);
            Invoke(() => MarkStepFailed(0));
            _buildSucceeded = false;
            throw new InvalidOperationException("Windows 10 or later is required.");
        }
        AppendLog($"Windows {os.Version} detected.", AccentGreen);

        if (!Environment.Is64BitOperatingSystem)
        {
            AppendLog("64-bit Windows is required.", AccentRed);
            Invoke(() => MarkStepFailed(0));
            throw new InvalidOperationException("64-bit Windows is required.");
        }
        AppendLog("64-bit architecture confirmed.", AccentGreen);
        SetProgress(0, 100);
    }

    private void Step1_CheckDotnet(CancellationToken ct)
    {
        AdvanceStep(1, ct);
        AppendLog("Searching for .NET SDK...");

        string[] candidates =
        [
            "dotnet",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "dotnet.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "dotnet", "dotnet.exe"),
            @"C:\dotnet\dotnet.exe",
            @"D:\dotnet\dotnet.exe"
        ];

        foreach (var candidate in candidates)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var (exitCode, output) = RunProcess(candidate, "--list-sdks", ct, timeoutMs: 10000);
                if (exitCode != 0) continue;

                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var ver = line.Trim().Split(' ')[0];
                    if (int.TryParse(ver.Split('.')[0], out int major) && major >= 8)
                    {
                        _dotnetPath = candidate;
                        AppendLog($"Found .NET SDK {ver} at: {candidate}", AccentGreen);
                        SetProgress(1, 100);
                        return;
                    }
                }
                AppendLog($"SDK found at {candidate} but no .NET 8+ version detected.", AccentYellow);
            }
            catch
            {
                continue;
            }
        }

        AppendLog(".NET 8+ SDK not found on this system.", AccentRed);
        AppendLog("Download from: https://dotnet.microsoft.com/download/dotnet/8.0", AccentCyan);
        Invoke(() =>
        {
            MarkStepFailed(1);
            var result = MessageBox.Show(
                ".NET 8 SDK is required but was not found.\n\nOpen the download page?",
                "RecallIQ Setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
                Process.Start(new ProcessStartInfo("https://dotnet.microsoft.com/download/dotnet/8.0") { UseShellExecute = true });
        });
        throw new InvalidOperationException(".NET 8+ SDK is required.");
    }

    private void Step2_CheckSolution(CancellationToken ct)
    {
        AdvanceStep(2, ct);
        AppendLog($"Solution directory: {_solutionDir}");

        string[] required =
        [
            _slnFile,
            Path.Combine(_uiProject, "RecallIQ.UI.csproj"),
            Path.Combine(_uiProject, "App.xaml.cs"),
            Path.Combine(_solutionDir, "RecallIQ.Core", "RecallIQ.Core.csproj"),
            Path.Combine(_solutionDir, "RecallIQ.Storage", "RecallIQ.Storage.csproj"),
            Path.Combine(_solutionDir, "RecallIQ.AI", "RecallIQ.AI.csproj"),
            Path.Combine(_solutionDir, "RecallIQ.Indexing", "RecallIQ.Indexing.csproj"),
            Path.Combine(_solutionDir, "RecallIQ.Search", "RecallIQ.Search.csproj")
        ];

        bool allFound = true;
        foreach (var file in required)
        {
            ct.ThrowIfCancellationRequested();
            if (File.Exists(file))
            {
                AppendLog($"  Found: {Path.GetFileName(file)}", AccentGreen);
            }
            else
            {
                AppendLog($"  MISSING: {file}", AccentRed);
                allFound = false;
            }
        }

        if (!allFound)
        {
            Invoke(() => MarkStepFailed(2));
            throw new InvalidOperationException("Solution files are missing. Make sure you extracted the full ZIP.");
        }

        AppendLog("All project files verified.", AccentGreen);
        SetProgress(2, 100);
    }

    private void Step3_Restore(CancellationToken ct)
    {
        AdvanceStep(3, ct);
        AppendLog("Restoring NuGet packages...");

        var (exitCode, output) = RunProcess(_dotnetPath, $"restore \"{_slnFile}\" --verbosity normal", ct, timeoutMs: 300000);
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
                AppendLog("  " + trimmed, trimmed.Contains("error", StringComparison.OrdinalIgnoreCase) ? AccentRed : TextDim);
        }

        if (exitCode != 0)
        {
            AppendLog("NuGet restore failed.", AccentRed);
            Invoke(() => MarkStepFailed(3));
            throw new InvalidOperationException("NuGet restore failed. Check the log for details.");
        }

        AppendLog("Packages restored successfully.", AccentGreen);
        SetProgress(3, 100);
    }

    private void Step4_Build(CancellationToken ct)
    {
        AdvanceStep(4, ct);

        (string config, string platform, string label)[] strategies =
        [
            ("Release", "x64", "Release x64"),
            ("Debug", "x64", "Debug x64"),
            ("Release", "AnyCPU", "Release AnyCPU")
        ];

        foreach (var (config, platform, label) in strategies)
        {
            ct.ThrowIfCancellationRequested();
            AppendLog($"Trying build: {label}...");

            var platformArg = platform == "AnyCPU" ? "" : $" -p:Platform={platform}";
            var (exitCode, output) = RunProcess(
                _dotnetPath,
                $"build \"{_slnFile}\" -c {config}{platformArg} --no-restore --verbosity normal",
                ct, timeoutMs: 600000);

            int errors = 0, warnings = 0;
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed.Contains(": error ", StringComparison.OrdinalIgnoreCase))
                {
                    errors++;
                    AppendLog("  " + trimmed, AccentRed);
                }
                else if (trimmed.Contains(": warning ", StringComparison.OrdinalIgnoreCase))
                {
                    warnings++;
                    if (warnings <= 10)
                        AppendLog("  " + trimmed, AccentYellow);
                }
                else if (trimmed.Contains("Build succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    AppendLog("  " + trimmed, AccentGreen);
                }
            }

            if (exitCode == 0)
            {
                var tfm = "net8.0-windows10.0.22621.0";
                var platformDir = platform == "AnyCPU" ? config : $"{platform}\\{config}";
                _binDir = Path.Combine(_uiProject, "bin", platformDir, tfm);

                if (File.Exists(Path.Combine(_binDir, "RecallIQ.UI.exe")))
                {
                    AppendLog($"Build succeeded: {label} ({errors} errors, {warnings} warnings)", AccentGreen);
                    AppendLog($"Output: {_binDir}", TextSecondary);
                    _buildSucceeded = true;
                    SetProgress(4, 100);
                    return;
                }

                AppendLog($"Build reported success but exe not found at expected path.", AccentYellow);
                AppendLog($"  Checked: {_binDir}", TextDim);

                var altBin = Path.Combine(_uiProject, "bin", config, tfm);
                if (File.Exists(Path.Combine(altBin, "RecallIQ.UI.exe")))
                {
                    _binDir = altBin;
                    AppendLog($"Found exe at alternate path: {altBin}", AccentGreen);
                    _buildSucceeded = true;
                    SetProgress(4, 100);
                    return;
                }
            }

            AppendLog($"Build strategy {label} failed ({errors} errors).", AccentYellow);
        }

        AppendLog("Trying dotnet publish as last resort...", AccentYellow);
        var publishDir = Path.Combine(_solutionDir, "publish");
        var (pubExit, pubOutput) = RunProcess(
            _dotnetPath,
            $"publish \"{Path.Combine(_uiProject, "RecallIQ.UI.csproj")}\" -c Release -r win-x64 --self-contained false -o \"{publishDir}\" --verbosity normal",
            ct, timeoutMs: 600000);

        foreach (var line in pubOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Contains(": error ", StringComparison.OrdinalIgnoreCase))
                AppendLog("  " + trimmed, AccentRed);
        }

        if (pubExit == 0 && File.Exists(Path.Combine(publishDir, "RecallIQ.UI.exe")))
        {
            _binDir = publishDir;
            _buildSucceeded = true;
            AppendLog("Publish succeeded.", AccentGreen);
            SetProgress(4, 100);
            return;
        }

        Invoke(() => MarkStepFailed(4));
        throw new InvalidOperationException("All build strategies failed. Check the log for compiler errors.");
    }

    private void Step5_Setup(CancellationToken ct)
    {
        AdvanceStep(5, ct);

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecallIQ");

        string[] dirs = [appData, Path.Combine(appData, "logs")];
        foreach (var dir in dirs)
        {
            ct.ThrowIfCancellationRequested();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AppendLog($"Created: {dir}", TextSecondary);
            }
        }

        var modelsDir = Path.Combine(_solutionDir, "models");
        var tessDir = Path.Combine(_solutionDir, "tessdata");
        if (!Directory.Exists(modelsDir)) Directory.CreateDirectory(modelsDir);
        if (!Directory.Exists(tessDir)) Directory.CreateDirectory(tessDir);

        bool hasModel = File.Exists(Path.Combine(modelsDir, "all-MiniLM-L6-v2.onnx"))
            || File.Exists(Path.Combine(modelsDir, "model.onnx"));
        bool hasTess = File.Exists(Path.Combine(tessDir, "eng.traineddata"));

        if (!hasModel)
            AppendLog("No ONNX model found — hash fallback embeddings will be used.", AccentYellow);
        else
            AppendLog("ONNX model detected.", AccentGreen);

        if (!hasTess)
            AppendLog("No Tesseract data found — OCR will be disabled.", AccentYellow);
        else
            AppendLog("Tesseract data detected.", AccentGreen);

        AppendLog("Directories configured.", AccentGreen);
        SetProgress(5, 100);
    }

    private void Step6_Done(CancellationToken ct)
    {
        AdvanceStep(6, ct);
        AppendLog("RecallIQ is ready.", AccentGreen);
        AppendLog($"Executable: {Path.Combine(_binDir, "RecallIQ.UI.exe")}", TextSecondary);
        SetProgress(6, 100);
        SetStatus("Installation complete. Click Finish to continue.");
    }

    private void LaunchApp()
    {
        var exePath = Path.Combine(_binDir, "RecallIQ.UI.exe");
        if (!File.Exists(exePath)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = _binDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _dotnetPath,
                    Arguments = $"exec \"{Path.Combine(_binDir, "RecallIQ.UI.dll")}\"",
                    WorkingDirectory = _binDir,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show(
                    $"Could not launch RecallIQ automatically.\n\nPlease run manually:\n{exePath}\n\nError: {ex.Message}",
                    "RecallIQ Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }

    private (int exitCode, string output) RunProcess(string fileName, string arguments, CancellationToken ct, int timeoutMs = 60000)
    {
        using var proc = new Process();
        proc.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _solutionDir
        };

        var output = new System.Text.StringBuilder();
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        using var reg = ct.Register(() => { try { proc.Kill(true); } catch { } });

        if (!proc.WaitForExit(timeoutMs))
        {
            try { proc.Kill(true); } catch { }
            return (-1, output + "\n[TIMEOUT]");
        }

        proc.WaitForExit();
        return (proc.ExitCode, output.ToString());
    }
}
