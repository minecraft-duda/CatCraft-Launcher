// CatCraftLauncher.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CatCraftLauncher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public static class AppInfo
    {
        public const string Version = "Dev 2.1.1";
        public static string CurrentDir => AppDomain.CurrentDomain.BaseDirectory;
    }

    // 修正：使用 static readonly 替代 const，使数组可被外部安全引用
    public static class VersionList
    {
        public static readonly string[] list = { "3.1-pre1", "3.1-pre2" };
    }

    // 配置管理
    public class ConfigManager
    {
        private string configFile;
        private JsonDocument config;

        public ConfigManager()
        {
            string configDir = Path.Combine(AppInfo.CurrentDir, ".ccl");
            configFile = Path.Combine(configDir, "option.json");
            Load();
        }

        private void Load()
        {
            if (File.Exists(configFile))
            {
                try { config = JsonDocument.Parse(File.ReadAllText(configFile)); }
                catch { CreateDefault(); }
            }
            else CreateDefault();
        }

        private void CreateDefault()
        {
            var def = new { game_root_dir = AppInfo.CurrentDir, tips_at_close = true, current_version = VersionList.list.Length > 0 ? VersionList.list[0] : "v1.0.0" };
            config = JsonDocument.Parse(JsonSerializer.Serialize(def));
            Save();
        }

        public void Save()
        {
            string dir = Path.GetDirectoryName(configFile);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var obj = new
            {
                game_root_dir = GetString("game_root_dir", AppInfo.CurrentDir),
                tips_at_close = GetBool("tips_at_close", true),
                current_version = GetString("current_version", VersionList.list.Length > 0 ? VersionList.list[0] : "v1.0.0")
            };
            File.WriteAllText(configFile, JsonSerializer.Serialize(obj));
        }

        public string GetString(string key, string def)
        {
            if (config.RootElement.TryGetProperty(key, out JsonElement e) && e.ValueKind == JsonValueKind.String)
                return e.GetString();
            return def;
        }

        public bool GetBool(string key, bool def)
        {
            if (config.RootElement.TryGetProperty(key, out JsonElement e) && (e.ValueKind == JsonValueKind.True || e.ValueKind == JsonValueKind.False))
                return e.GetBoolean();
            return def;
        }

        public void Set(string key, object value)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(config.RootElement.GetRawText());
            dict[key] = value;
            config = JsonDocument.Parse(JsonSerializer.Serialize(dict));
            Save();
        }

        public string GameRoot { get => GetString("game_root_dir", AppInfo.CurrentDir); set => Set("game_root_dir", value); }
        public bool ExitConfirm { get => GetBool("tips_at_close", true); set => Set("tips_at_close", value); }
        public string CurrentVersion { get => GetString("current_version", VersionList.list.Length > 0 ? VersionList.list[0] : "v1.0.0"); set => Set("current_version", value); }
        public string GameExePath => Path.Combine(GameRoot, ".catcraft", "version", "catcraft.exe");
        public string CreatorJsonPath => Path.Combine(GameRoot, ".ccl", "creator.json");
    }

    // 圆角按钮
    class RoundedButton : Button
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            GraphicsPath path = new GraphicsPath();
            int radius = 30;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(Width - radius, Height - radius, radius, radius, 0, 90);
            path.AddArc(0, Height - radius, radius, radius, 90, 90);
            this.Region = new Region(path);
            base.OnPaint(e);
        }
    }

    // 圆角面板
    class RoundedPanel : Panel
    {
        public int Radius = 30;
        protected override void OnPaint(PaintEventArgs e)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, Radius, Radius, 180, 90);
            path.AddArc(Width - Radius, 0, Radius, Radius, 270, 90);
            path.AddArc(Width - Radius, Height - Radius, Radius, Radius, 0, 90);
            path.AddArc(0, Height - Radius, Radius, Radius, 90, 90);
            this.Region = new Region(path);
            base.OnPaint(e);
        }
    }

    // 橙色顶栏（带半透明黑色遮罩的 Logo 和导航按钮）
    class OrangeTopBar : Panel
    {
        public TabControl ParentTabControl { get; set; }

        public OrangeTopBar()
        {
            this.Height = 64;
            this.BackColor = Color.FromArgb(245, 124, 0);
            this.Dock = DockStyle.Top;

            // Logo 文字 - 带半透明黑色遮罩
            Panel logoPanel = new Panel
            {
                Size = new Size(160, 40),
                Location = new Point(20, 12),
                BackColor = Color.FromArgb(30, 0, 0, 0)
            };
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, 32, 32, 180, 90);
            path.AddArc(160 - 32, 0, 32, 32, 270, 90);
            path.AddArc(160 - 32, 40 - 32, 32, 32, 0, 90);
            path.AddArc(0, 40 - 32, 32, 32, 90, 90);
            logoPanel.Region = new Region(path);

            Label logo = new Label
            {
                Text = "CatCraft Launcher",
                Font = new Font("微软雅黑", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            logoPanel.Controls.Add(logo);
            this.Controls.Add(logoPanel);

            // 导航按钮容器 - 居中
            FlowLayoutPanel navPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point((this.Width - 300) / 2, 12),
                Size = new Size(300, 40),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top
            };

            string[] tabs = { "启动", "下载", "设置", "更多" };
            foreach (string tabName in tabs)
            {
                Button btn = new Button
                {
                    Text = tabName,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.White,
                    Font = new Font("微软雅黑", 10, FontStyle.Bold),
                    Size = new Size(60, 40),
                    Tag = tabName
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) =>
                {
                    Button b = (Button)s;
                    if (ParentTabControl != null)
                    {
                        for (int i = 0; i < ParentTabControl.TabPages.Count; i++)
                        {
                            if (ParentTabControl.TabPages[i].Text == b.Text)
                            {
                                ParentTabControl.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                };
                navPanel.Controls.Add(btn);
            }
            this.Controls.Add(navPanel);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            foreach (Control ctl in this.Controls)
            {
                if (ctl is FlowLayoutPanel panel)
                {
                    panel.Location = new Point((this.Width - panel.Width) / 2, 12);
                    break;
                }
            }
        }
    }

    // 强制下载对话框（保留框架）
    public class DownloadDialog : Form
    {
        private bool complete = false;
        public bool Complete => complete;

        public DownloadDialog()
        {
            this.Text = "正在下载";
            this.Size = new Size(400, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ControlBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            var label = new Label
            {
                Text = "正在下载游戏启动时的必要资源...\n请勿关闭此窗口",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50,
                Font = new Font("微软雅黑", 9)
            };
            this.Controls.Add(label);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!complete) e.Cancel = true;
            base.OnFormClosing(e);
        }

        public async Task StartDownload(string url, string path)
        {
            complete = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
            await Task.CompletedTask;
        }
    }

    // 贡献名单滚动对话框
    public class CreditsDialog : Form
    {
        private Timer timer;
        private Panel contentPanel;
        private int scrollY;
        private int contentHeight;

        public CreditsDialog(JsonDocument data)
        {
            this.Text = $"贡献名单 - {AppInfo.Version}";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 124, 0);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = false };
            scrollPanel.BackColor = Color.FromArgb(245, 124, 0);
            this.Controls.Add(scrollPanel);

            contentPanel = new Panel { AutoSize = true, BackColor = Color.FromArgb(245, 124, 0) };
            scrollPanel.Controls.Add(contentPanel);

            int y = 20;
            var devs = data.RootElement.GetProperty("开发人员").EnumerateArray();
            bool first = true;
            foreach (var d in devs)
            {
                if (first) { AddLabel("开发人员", 18, Color.Black, true, ref y); first = false; }
                AddLabel(d.GetString(), 12, Color.White, false, ref y);
            }
            var thanks = data.RootElement.GetProperty("致谢人员").EnumerateArray();
            first = true;
            foreach (var t in thanks)
            {
                if (first) { AddLabel("致谢人员", 18, Color.Black, true, ref y); first = false; }
                AddLabel(t.GetString(), 12, Color.White, false, ref y);
            }
            y += 400;
            contentPanel.Size = new Size(500, y);
            timer = new Timer { Interval = 20 };
            timer.Tick += (s, e) =>
            {
                scrollY -= 2;
                contentPanel.Location = new Point(0, scrollY);
                if (scrollY + contentHeight <= 0) { timer.Stop(); this.Close(); }
            };
        }

        private void AddLabel(string text, int fontSize, Color color, bool bold, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("微软雅黑", fontSize, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(lbl);
            lbl.Location = new Point((500 - lbl.Width) / 2, y);
            y += lbl.Height + 10;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            contentHeight = contentPanel.Height;
            scrollY = this.ClientSize.Height;
            contentPanel.Location = new Point(0, scrollY);
            timer.Start();
        }
    }

    // ========== 启动页面 ==========
    public class LaunchPage : UserControl
    {
        private Label versionLabel;
        private ConfigManager config;

        public LaunchPage(ConfigManager cfg)
        {
            config = cfg;
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;

            // 使用 ConfigManager 中保存的当前版本
            string currentVersion = config.CurrentVersion;
            versionLabel = new Label
            {
                Text = $"当前选择版本: {currentVersion}",
                Font = new Font("微软雅黑", 12),
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 50,
                BackColor = Color.Transparent
            };

            var launchBtn = new RoundedButton
            {
                Text = "🚀 启动游戏",
                Size = new Size(220, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 124, 0),
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 10, FontStyle.Bold)
            };
            launchBtn.FlatAppearance.BorderSize = 0;
            var selectBtn = new RoundedButton
            {
                Text = "⚙️ 选择版本",
                Size = new Size(220, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 124, 0),
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 10, FontStyle.Bold)
            };
            selectBtn.FlatAppearance.BorderSize = 0;
            selectBtn.Click += (s, e) =>
            {
                var dialog = new VersionDialog(config.CurrentVersion);
                if (dialog.ShowDialog() == DialogResult.OK && dialog.SelectedVersion != null)
                {
                    // 保存选中的版本到配置
                    config.CurrentVersion = dialog.SelectedVersion;
                    versionLabel.Text = $"当前选择版本: {dialog.SelectedVersion}";
                }
            };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20),
                BackColor = Color.Transparent
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            panel.Controls.Add(versionLabel, 0, 0);
            panel.Controls.Add(launchBtn, 0, 1);
            panel.Controls.Add(selectBtn, 0, 2);
            versionLabel.Anchor = AnchorStyles.None;
            launchBtn.Anchor = AnchorStyles.None;
            selectBtn.Anchor = AnchorStyles.None;
            this.Controls.Add(panel);
        }
    }

    // ========== 下载页面 ==========
    public class DownloadPage : UserControl
    {
        public DownloadPage()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;

            // 版本列表容器（FlowLayoutPanel，带滚动条）
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.Transparent
            };

            // 使用全局版本列表 VersionList.list
            foreach (var v in VersionList.list)
            {
                Panel row = CreateVersionRow(v);
                flow.Controls.Add(row);
            }

            // 使用 TableLayoutPanel 精确控制三行布局
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Clear();
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // 标题行高度
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));   // 副标题行高度
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // 版本列表填充剩余

            // 可在此添加标题和副标题（原代码被注释，保留风格）
            // mainLayout.Controls.Add(title, 0, 0);
            // mainLayout.Controls.Add(sub, 0, 1);
            mainLayout.Controls.Add(flow, 0, 2);

            this.Controls.Add(mainLayout);
        }

        // 创建单个版本项（圆角卡片）
        private Panel CreateVersionRow(string versionName)
        {
            Panel row = new Panel
            {
                Width = this.Width - 50,
                Height = 60,
                BackColor = Color.FromArgb(245, 124, 0),
                Margin = new Padding(5)
            };
            ApplyRoundCorners(row, 30);

            Label name = new Label
            {
                Text = $"🐱 小猫挖矿 {versionName}",
                ForeColor = Color.Black,
                Font = new Font("微软雅黑", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20),
                BackColor = Color.Transparent
            };
            Button btn = new Button
            {
                Text = "⬇️ 下载",
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(245, 124, 0),
                Location = new Point(row.Width - 110, 14)
            };
            btn.FlatAppearance.BorderSize = 0;
            row.Controls.Add(name);
            row.Controls.Add(btn);
            return row;
        }

        private void ApplyRoundCorners(Control ctrl, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(ctrl.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(ctrl.Width - radius, ctrl.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, ctrl.Height - radius, radius, radius, 90, 90);
            ctrl.Region = new Region(path);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // 动态调整每个版本行的宽度和按钮位置
            foreach (Control ctl in this.Controls)
            {
                if (ctl is TableLayoutPanel mainLayout)
                {
                    var flow = mainLayout.GetControlFromPosition(0, 2) as FlowLayoutPanel;
                    if (flow != null)
                    {
                        foreach (Control row in flow.Controls)
                        {
                            row.Width = this.Width - 50;
                            foreach (Control sub in row.Controls)
                            {
                                if (sub is Button btn)
                                    btn.Location = new Point(row.Width - 110, 14);
                            }
                            ApplyRoundCorners(row, 30);
                        }
                    }
                    break;
                }
            }
        }
    }

    // ========== 设置页面（不变） ==========
    public class SettingsPage : UserControl
    {
        private ConfigManager cfg;
        private CheckBox exitCheck;
        private TextBox rootBox;
        private Panel card;

        public SettingsPage(ConfigManager config)
        {
            cfg = config;
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;

            card = new Panel
            {
                Size = new Size(450, 250),
                BackColor = Color.White
            };
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, 32, 32, 180, 90);
            path.AddArc(450 - 32, 0, 32, 32, 270, 90);
            path.AddArc(450 - 32, 250 - 32, 32, 32, 0, 90);
            path.AddArc(0, 250 - 32, 32, 32, 90, 90);
            card.Region = new Region(path);

            var title = new Label
            {
                Text = "⚙️ 启动器设置",
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 124, 0),
                Location = new Point(30, 20),
                AutoSize = true
            };
            var line = new Label
            {
                Text = "────────────────────",
                ForeColor = Color.LightGray,
                Location = new Point(30, 50),
                AutoSize = true
            };
            exitCheck = new CheckBox { Text = "退出前确认", Location = new Point(30, 80), AutoSize = true };
            var dirLabel = new Label { Text = "📁 游戏根目录:", Location = new Point(30, 120), AutoSize = true };
            rootBox = new TextBox { Location = new Point(150, 115), Width = 220 };
            var browseBtn = new Button { Text = "浏览...", Location = new Point(380, 113), Size = new Size(60, 25) };
            browseBtn.Click += (s, e) => { using (var fbd = new FolderBrowserDialog()) if (fbd.ShowDialog() == DialogResult.OK) rootBox.Text = fbd.SelectedPath; };
            var saveBtn = new Button
            {
                Text = "保存设置",
                Location = new Point(150, 170),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 124, 0),
                ForeColor = Color.White
            };
            saveBtn.FlatAppearance.BorderSize = 0;
            saveBtn.Click += (s, e) =>
            {
                cfg.ExitConfirm = exitCheck.Checked;
                cfg.GameRoot = rootBox.Text;
                MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            card.Controls.Add(title);
            card.Controls.Add(line);
            card.Controls.Add(exitCheck);
            card.Controls.Add(dirLabel);
            card.Controls.Add(rootBox);
            card.Controls.Add(browseBtn);
            card.Controls.Add(saveBtn);
            this.Controls.Add(card);

            card.Location = new Point((this.Width - 450) / 2, (this.Height - 250) / 2);
            card.Anchor = AnchorStyles.None;

            exitCheck.Checked = cfg.ExitConfirm;
            rootBox.Text = cfg.GameRoot;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (card != null) card.Location = new Point((this.Width - 450) / 2, (this.Height - 250) / 2);
        }
    }

    // ========== 更多页面 ==========
    public class MorePage : UserControl
    {
        private ConfigManager cfg;
        private RoundedButton creditsBtn;

        public MorePage(ConfigManager config)
        {
            cfg = config;
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;

            // 版本号标签
            Label versionLabel = new Label
            {
                Text = $"版本号：{AppInfo.Version}",
                Font = new Font("微软雅黑", 16, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // 贡献名单按钮
            creditsBtn = new RoundedButton
            {
                Text = "📜 贡献名单",
                Size = new Size(200, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 124, 0),
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 10, FontStyle.Bold)
            };
            creditsBtn.FlatAppearance.BorderSize = 0;
            creditsBtn.Click += async (s, e) => await ShowCredits();

            // 使用 TableLayoutPanel 垂直居中显示
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20)
            };
            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));  // 上留白
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // 版本号行
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // 按钮行
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));  // 下留白

            layout.Controls.Add(versionLabel, 0, 1);
            creditsBtn.Anchor = AnchorStyles.None;
            layout.Controls.Add(creditsBtn, 0, 2);

            this.Controls.Add(layout);
        }

        private async Task ShowCredits()
        {
            string jsonPath = cfg.CreatorJsonPath;
            string json = null;
            try
            {
                using (var client = new HttpClient())
                {
                    var resp = await client.GetAsync("https://m1954420.772988.xyz/ccl/creator.json");
                    resp.EnsureSuccessStatusCode();
                    json = await resp.Content.ReadAsStringAsync();
                    File.WriteAllText(jsonPath, json);
                }
            }
            catch
            {
                if (File.Exists(jsonPath)) json = File.ReadAllText(jsonPath);
                else { MessageBox.Show("无法获取贡献名单", "错误"); return; }
            }
            var doc = JsonDocument.Parse(json);
            var dialog = new CreditsDialog(doc);
            dialog.ShowDialog();
        }
    }

    // 版本选择模拟对话框（已改造，使用 VersionList.list，并支持默认选中指定版本）
    public class VersionDialog : Form
    {
        public string SelectedVersion { get; private set; }

        public VersionDialog(string currentVersion)
        {
            this.Text = "选择小猫挖矿版本";
            this.Size = new Size(300, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            var listBox = new ListBox { Dock = DockStyle.Fill };
            // 使用全局版本列表生成选项
            int selectedIndex = -1;
            for (int i = 0; i < VersionList.list.Length; i++)
            {
                string v = VersionList.list[i];
                listBox.Items.Add($"{v}");
                if (v == currentVersion)
                    selectedIndex = i;
            }
            
            // 默认选中当前版本
            if (selectedIndex >= 0)
                listBox.SelectedIndex = selectedIndex;
            else if (listBox.Items.Count > 0)
                listBox.SelectedIndex = 0;

            var ok = new Button { Text = "确定", Dock = DockStyle.Bottom, Height = 30 };
            ok.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    string selected = listBox.SelectedItem.ToString();
                    SelectedVersion = selected;
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(listBox);
            this.Controls.Add(ok);
        }
    }

    // 主窗体
    public class MainForm : Form
    {
        private ConfigManager config;
        private TabControl tabControl;
        private OrangeTopBar topBar;

        public MainForm()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath)) this.Icon = new Icon(iconPath);
            }
            catch { }

            this.Text = $"CatCraft Launcher - {AppInfo.Version}";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 242, 245);

            config = new ConfigManager();
            SetupUI();
        }

        private void SetupUI()
        {
            topBar = new OrangeTopBar();
            this.Controls.Add(topBar);

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(0, 0),
                Margin = new Padding(0),
                ItemSize = new Size(0, 1),
                Appearance = TabAppearance.FlatButtons,
                SizeMode = TabSizeMode.Fixed,
                Alignment = TabAlignment.Top
            };
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += (s, e) => { };

            topBar.ParentTabControl = tabControl;

            AddTab("启动", new LaunchPage(config));
            AddTab("下载", new DownloadPage());
            AddTab("设置", new SettingsPage(config));
            AddTab("更多", new MorePage(config));

            this.Controls.Add(tabControl);
            topBar.BringToFront();
            tabControl.SelectedIndex = 0;
        }

        private void AddTab(string title, UserControl content)
        {
            var page = new TabPage(title);
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            tabControl.TabPages.Add(page);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (config.ExitConfirm)
            {
                if (MessageBox.Show("确定要退出 CatCraft Launcher 吗？", "确认退出", MessageBoxButtons.YesNo) == DialogResult.No)
                    e.Cancel = true;
            }
            base.OnFormClosing(e);
        }
    }
}