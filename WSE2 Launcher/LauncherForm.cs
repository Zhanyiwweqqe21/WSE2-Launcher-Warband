using ConfigController;
using ModuleHelper;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using WSE2_CLI_Options;

namespace WSE2_Launcher
{
    public partial class LauncherForm : Form
    {
        //private PerPixelAlphaForm background;
        private FormBackground background;
        private string WarbandPath;

        /// <summary>
        /// ReadSettings defaults to the conf file in Documents/Mount&Blade...
        /// </summary>
        private RglSettings Settings = RglLoader.ReadSettings();

        public LauncherForm()
        {
            InitializeComponent();

            //remove this form's background, because it causes weird graphics
            this.BackgroundImage = null;
            this.background = new FormBackground(Properties.Resources.background, this);

            // TODO set warband path to pwd ins
            string pwd = System.IO.Directory.GetCurrentDirectory();
            WarbandPath = pwd;

            if (!Directory.Exists(Path.Combine(WarbandPath, "Modules")))
            {
                MessageBox.Show("Could not find Modules directory. Are you sure that this launcher was placed in the Warband folder?");
            }


            // Initializes the modules select box
            ModuleEntry[] moduleEntries = ModuleList.GetModuleEntries(System.IO.Path.Combine(WarbandPath, "Modules"));
            moduleSelectBox.Items.AddRange(moduleEntries);
            if (moduleEntries.Length < 1)
            {
                MessageBox.Show(String.Format("No modules found in \"{0}\\{1}\"!", WarbandPath, "Modules"));
                Close();
            }

            // Sets the moduleSelectBox to default module, or Native
            string defaultModule = Settings.bDefaultModule.Get();
            InitializeModuleSelectBoxOrFirst(defaultModule);
        }

        private void InitializeModuleSelectBoxOrFirst(string defaultModule)
        {
            bool found = false;
            // Looks for defaultModule in moduleEntries
            int i = 0;
            foreach (ModuleEntry me in moduleSelectBox.Items)
            {
                if (me.Name == defaultModule)
                {
                    found = true;
                    break;
                }
                i++;
            }
            if (!found)
            {
                i = 0;
            }

            moduleSelectBox.SelectedIndex = i;
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }


        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void minimizeButton_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void configureLabel_Click(object sender, EventArgs e)
        {
            ConfigForm cf = new ConfigForm(Settings);
            cf.Show();
        }

        private void playLabel_Click(object sender, EventArgs e)
{
    // 1. 检查模块选择
    ModuleEntry selected = (ModuleEntry)moduleSelectBox.SelectedItem;
    if (selected == null)
    {
        MessageBox.Show("请选择一个模块！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    // 2. 保存默认模块
    if (Settings.bDefaultModule.Get() != selected.ToString())
    {
        Settings.bDefaultModule.Set(selected.ToString());
        Settings.WriteSettings();
    }

    // 3. 生成客户端参数（仅用于客户端启动，服务器不需要）
    CLI_Options options = new CLI_Options();
    options.Module = selected.Name;
    options.IntroDisabled = Settings.bDisableIntro.Get();
    options.AdditionalArgs.Add("+load_plugin WSE2Auth.dll");
    string cli_options = options.ToString(); // 客户端参数，服务器不需要

    // 4. 修正路径：PK1.3.3 是文件夹，启动器在文件夹内
    string serverExePath = Path.Combine(WarbandPath, "PK1.3.3", "PK1.3.3.exe");

    // 5. 修正参数：服务器启动器不需要客户端参数
    // 服务器参数示例：-r PW.txt -m 模块名
    string server_arguments = $@"-r PW.txt -m ""{selected.Name}""";

    // 6. 检查文件存在性
    if (!File.Exists(serverExePath))
    {
        MessageBox.Show($"未找到启动器文件: {serverExePath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

    // 7. 输出调试信息
    Console.WriteLine($"Executing server command: {serverExePath} {server_arguments}");

    // 8. 启动进程
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = serverExePath,
            Arguments = server_arguments,
            WorkingDirectory = WarbandPath, // 工作目录设为游戏目录
            UseShellExecute = false,
            CreateNoWindow = true
        });

        // 启动成功后关闭窗口（可选）
        this.Close();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"启动服务器失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

        private void moduleSelectBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // changes the image
            ModuleEntry selected = (ModuleEntry)moduleSelectBox.SelectedItem;
            modulePictureBox.Image = selected.GetBitmap();
        }

        private void LauncherForm_Load(object sender, EventArgs e)
        {

        }
    }
}
