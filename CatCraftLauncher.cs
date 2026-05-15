// CatCraftLauncher.cs
// 编译命令：csc /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Net.Http.dll CatCraftLauncher.cs
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
        public const string Version = "Dev 1.0";
        public static string CurrentDir => AppDomain.CurrentDomain.BaseDirectory;
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
            var def = new { game_root_dir = AppInfo.CurrentDir, tips_at_close = true };
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
                tips_at_close = GetBool("tips_at_close", true)
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
                BackColor = Color.FromArgb(30, 0, 0, 0)  // 半透明黑色
            };
            // 圆角
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
            // 重新计算导航栏居中位置
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

    // 强制下载对话框
    // 强制下载对话框（无取消按钮，禁止关闭）
	public class DownloadDialog : Form
	{
		private ProgressBar progressBar;
		private bool complete = false;
		public bool Complete => complete;
	
		public DownloadDialog()
		{
			this.Text = "正在下载";
			this.Size = new Size(400, 150);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.ControlBox = false;  // 禁用关闭按钮
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
			progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 30, Minimum = 0, Maximum = 100 };
			
			this.Controls.Add(label);
			this.Controls.Add(progressBar);
		}
	
		// 禁止 Alt+F4 关闭
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (!complete)
			{
				e.Cancel = true;
			}
			base.OnFormClosing(e);
		}
	
		public async Task StartDownload(string url, string path)
		{
			string dir = Path.GetDirectoryName(path);
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
	
			using (var client = new HttpClient())
			{
				try
				{
					using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
					{
						response.EnsureSuccessStatusCode();
						long total = response.Content.Headers.ContentLength ?? -1;
						using (var stream = await response.Content.ReadAsStreamAsync())
						using (var file = File.OpenWrite(path))
						{
							byte[] buffer = new byte[8192];
							long downloaded = 0;
							int read;
							while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
							{
								await file.WriteAsync(buffer, 0, read);
								downloaded += read;
								if (total > 0)
								{
									int percent = (int)(downloaded * 100 / total);
									progressBar.Value = percent;
								}
							}
						}
					}
					complete = true;
					this.DialogResult = DialogResult.OK;
				}
				catch
				{
					if (File.Exists(path)) File.Delete(path);
					MessageBox.Show("下载失败，即将重试...", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					this.DialogResult = DialogResult.Cancel;
				}
				finally { this.Close(); }
			}
		}
	}

    // 贡献名单滚动对话框
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
                if (first) 
                { 
                    AddLabel("开发人员", 18, Color.Black, true, ref y); 
                    first = false; 
                }
                AddLabel(d.GetString(), 12, Color.White, false, ref y);
            }
            var thanks = data.RootElement.GetProperty("致谢人员").EnumerateArray();
            first = true;
            foreach (var t in thanks)
            {
                if (first) 
                { 
                    AddLabel("致谢人员", 18, Color.Black, true, ref y); 
                    first = false; 
                }
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
            // 先添加到容器，这样才能获取实际宽度
            contentPanel.Controls.Add(lbl);
            // 计算居中位置
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

    // 启动页面
    public class LaunchPage : UserControl
    {
        public LaunchPage()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;

            var versionLabel = new Label
            {
                Text = "当前选择版本: v1.0.0 经典版",
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
            selectBtn.Click += (s, e) => new VersionDialog().ShowDialog();

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

    // 下载页面
    public class DownloadPage : UserControl
    {
        public DownloadPage()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;
            this.AutoScroll = true;

            var title = new Label
            {
                Text = "📦 小猫挖矿 · 版本库",
                Font = new Font("微软雅黑", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 124, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.Transparent
            };
            var sub = new Label
            {
                Text = "下方版本列表仅为展示，下载按钮无任何功能",
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.Transparent
            };
            this.Controls.Add(sub);
            this.Controls.Add(title);

            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.Transparent
            };

            var versions = new[] { "v1.0.0 经典版", "v1.1.0 效率提升", "v1.2.0 稳定推荐", "v2.0.0-beta 前瞻版" };
            foreach (var v in versions)
            {
                var row = new Panel
                {
                    Width = this.Width - 50,
                    Height = 60,
                    BackColor = Color.FromArgb(245, 124, 0),
                    Margin = new Padding(5)
                };
                // 圆角
                GraphicsPath path = new GraphicsPath();
                path.AddArc(0, 0, 30, 30, 180, 90);
                path.AddArc(row.Width - 30, 0, 30, 30, 270, 90);
                path.AddArc(row.Width - 30, row.Height - 30, 30, 30, 0, 90);
                path.AddArc(0, row.Height - 30, 30, 30, 90, 90);
                row.Region = new Region(path);

                var name = new Label
                {
                    Text = $"🐱 小猫挖矿 {v}",
                    ForeColor = Color.Black,
                    Font = new Font("微软雅黑", 12, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(20, 20),
                    BackColor = Color.Transparent
                };
                var btn = new Button
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
                flow.Controls.Add(row);
            }
            this.Controls.Add(flow);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // 调整每行宽度
            foreach (Control ctl in this.Controls)
            {
                if (ctl is FlowLayoutPanel flow)
                {
                    foreach (Control row in flow.Controls)
                    {
                        row.Width = this.Width - 50;
                        // 调整按钮位置
                        foreach (Control sub in row.Controls)
                        {
                            if (sub is Button btn)
                            {
                                btn.Location = new Point(row.Width - 110, 14);
                            }
                        }
                        // 更新圆角区域
                        GraphicsPath path = new GraphicsPath();
                        path.AddArc(0, 0, 30, 30, 180, 90);
                        path.AddArc(row.Width - 30, 0, 30, 30, 270, 90);
                        path.AddArc(row.Width - 30, row.Height - 30, 30, 30, 0, 90);
                        path.AddArc(0, row.Height - 30, 30, 30, 90, 90);
                        row.Region = new Region(path);
                    }
                    break;
                }
            }
        }
    }

    // 设置页面
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
            // 圆角
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

            // 居中
            card.Location = new Point((this.Width - 450) / 2, (this.Height - 250) / 2);
            card.Anchor = AnchorStyles.None;

            exitCheck.Checked = cfg.ExitConfirm;
            rootBox.Text = cfg.GameRoot;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (card != null)
            {
                card.Location = new Point((this.Width - 450) / 2, (this.Height - 250) / 2);
            }
        }
    }

    // 更多页面
    public class MorePage : UserControl
    {
        private ConfigManager cfg;
        private RoundedButton creditsBtn;
        private Label versionLabel;

        public MorePage(ConfigManager config)
        {
            cfg = config;
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;

            versionLabel = new Label
            {
                Text = $"版本号：{AppInfo.Version}",
                Font = new Font("微软雅黑", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.Transparent
            };
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
            creditsBtn.Location = new Point((this.Width - 200) / 2, (this.Height - 50) / 2);
            creditsBtn.Anchor = AnchorStyles.None;
            creditsBtn.Click += async (s, e) => await ShowCredits();

            this.Controls.Add(versionLabel);
            this.Controls.Add(creditsBtn);
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (creditsBtn != null)
            {
                creditsBtn.Location = new Point((this.Width - 200) / 2, (this.Height - 50) / 2);
            }
        }
    }

    // 版本选择模拟对话框
    public class VersionDialog : Form
    {
        public VersionDialog()
        {
            this.Text = "选择小猫挖矿版本";
            this.Size = new Size(300, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            var list = new ListBox { Dock = DockStyle.Fill };
            foreach (var v in new[] { "v1.0.0 经典版", "v1.1.0 效率提升", "v1.2.0 稳定推荐", "v2.0.0-beta 前瞻版" })
                list.Items.Add(v);
            var ok = new Button { Text = "确定", Dock = DockStyle.Bottom, Height = 30 };
            ok.Click += (s, e) => this.Close();
            this.Controls.Add(list);
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
			// 先设置图标（必须在窗口显示前）
			try
			{
				string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
				if (File.Exists(iconPath))
				{
					this.Icon = new Icon(iconPath);
				}
			}
			catch { }
			
			this.Text = $"CatCraft Launcher - {AppInfo.Version}";
			this.Size = new Size(900, 600);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Color.FromArgb(240, 242, 245);
			
			config = new ConfigManager();
			this.Shown += async (s, e) => { this.Hide(); await CheckAndDownload(); };
		}

        private async Task CheckAndDownload()
		{
			string exePath = config.GameExePath;
			if (File.Exists(exePath))
			{
				SetupUI();
				this.Show();
				return;
			}
		
			while (true)
			{
				var dialog = new DownloadDialog();
				dialog.Show();
				await dialog.StartDownload("https://m1954420.772988.xyz/ccl/catcraft.exe", exePath);
				if (dialog.Complete)
				{
					SetupUI();
					this.Show();
					return;
				}
				// 下载失败，继续循环
			}
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

            AddTab("启动", new LaunchPage());
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