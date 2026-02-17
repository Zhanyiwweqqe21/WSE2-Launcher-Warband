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
            // Saves this module as the default module to launch
            ModuleEntry selected = (ModuleEntry)moduleSelectBox.SelectedItem;
           if (selected == null)
           {
                  MessageBox.Show("请选择一个模块！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                      
                 return;
           }

            if (Settings.bDefaultModule.Get() != selected.ToString())
            {
                Settings.bDefaultModule.Set(selected.ToString());
                Settings.WriteSettings();
            }
            CLI_Options options = new CLI_Options();
            options.Module = selected.Name;
            options.IntroDisabled = Settings.bDisableIntro.Get();
            options.AdditionalArgs.Add("+load_plugin WSE2Auth.dll");
            string cli_options = options.ToString();
            try
{
    // 直接启动 WSELoaderServer.exe，完全替代 PK1.3.3.bat
    string serverExPath = Path.Combine(WarbandPath, "WSELoaderServer.exe");
    string server_arguments = $@"-r PW.txt -m ""Persistent Kingdoms 1.3.3"" {cli_options}";

    if (!File.Exists(serverExPath))
    {
        MessageBox.Show($"未找到服务器程序: {serverExPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

    Console.WriteLine("Executing server command: {0} {1}", serverExPath, server_arguments);

    // 构建启动信息
    ProcessStartInfo startInfo = new ProcessStartInfo
    {
        FileName = serverExPath,
        Arguments = server_arguments,
        WorkingDirectory = WarbandPath,
        UseShellExecute = false, // 关键：必须设为 false 才能拿到 PID
        CreateNoWindow = true     // 保持原有无窗口启动
    };

    // 启动服务器进程
    Process serverProcess = Process.Start(startInfo);
    if (serverProcess != null)
    {
        int serverPid = serverProcess.Id;
        Console.WriteLine($"服务器进程 PID: {serverPid}");

        // 注入你的防外挂 DLL
        string dllPath = Path.Combine(WarbandPath, "WSE2Auth.dll"); // 请替换为你的 DLL 文件名
        if (InjectDllToProcess(serverPid, dllPath))
        {
            Console.WriteLine("DLL 注入成功！");
        }
        else
        {
            MessageBox.Show("服务器启动成功，但 DLL 注入失败！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    else
    {
        MessageBox.Show("启动服务器进程失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
catch (Exception ex)
{
    MessageBox.Show($"启动服务器时发生错误: {ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

Close();

              }

        
         // --- DLL 注入函数 (改进版) ---
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int flAllocationType, int flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, int dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, int dwCreationFlags, out int lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, int dwSize);
        private static bool InjectDllToProcess(int processId, string dllPath)
{
    // --- 常量定义 ---
    const int PROCESS_CREATE_THREAD = 0x0002;
    const int PROCESS_QUERY_INFORMATION = 0x0400;
    const int PROCESS_VM_OPERATION = 0x0008;
    const int PROCESS_VM_WRITE = 0x0020;
    const int PROCESS_VM_READ = 0x0010;
    const int MEM_COMMIT = 0x1000;
    const int MEM_RELEASE = 0x8000;
    const int PAGE_READWRITE = 0x04;
    const int EXECUTE_IMMEDIATELY = 0;

    // 1. 打开目标进程
    // 注意：使用 OR 运算符 | 来组合权限
    IntPtr hProcess = OpenProcess(
        PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION |
        PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ,
        false, processId);

    if (hProcess == IntPtr.Zero)
    {
        Console.WriteLine($"OpenProcess 失败: 错误码 {Marshal.GetLastWin32Error()}");
        return false;
    }

    // 2. 分配内存
    int dllPathLen = (dllPath.Length + 1) * 2; // Unicode 字符，乘以 2
    IntPtr pDllPath = VirtualAllocEx(hProcess, IntPtr.Zero, dllPathLen, MEM_COMMIT, PAGE_READWRITE);

    if (pDllPath == IntPtr.Zero)
    {
        Console.WriteLine($"VirtualAllocEx 失败: 错误码 {Marshal.GetLastWin32Error()}");
        CloseHandle(hProcess);
        return false;
    }

    // 3. 写入 DLL 路径
    byte[] dllBytes = Encoding.Unicode.GetBytes(dllPath);
    int bytesWritten = 0;
    bool writeSuccess = WriteProcessMemory(hProcess, pDllPath, dllBytes, dllBytes.Length, out bytesWritten);

    if (!writeSuccess || bytesWritten == 0)
    {
        Console.WriteLine($"WriteProcessMemory 失败: 错误码 {Marshal.GetLastWin32Error()}");
        VirtualFreeEx(hProcess, pDllPath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }

    // 4. 获取 LoadLibraryW 地址 (关键修正点)
    // 原来的 GetModuleHandle 逻辑复杂且容易出错，直接传 null 让系统查找最稳妥
    // 注意：必须使用 LoadLibraryW (W结尾) 因为我们写入的是 Unicode 字符串
    IntPtr pLoadLibrary = GetProcAddress(IntPtr.Zero, "LoadLibraryW");

    if (pLoadLibrary == IntPtr.Zero)
    {
        Console.WriteLine($"GetProcAddress 失败: 错误码 {Marshal.GetLastWin32Error()}");
        VirtualFreeEx(hProcess, pDllPath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }

    // 5. 创建远程线程
    IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, pLoadLibrary, pDllPath, EXECUTE_IMMEDIATELY, out int threadId);

    if (hThread == IntPtr.Zero)
    {
        Console.WriteLine($"CreateRemoteThread 失败: 错误码 {Marshal.GetLastWin32Error()}");
        VirtualFreeEx(hProcess, pDllPath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }

    // 6. 等待线程结束 (可选，通常注入后立即返回即可)
    // WaitForSingleObject(hThread, 5000); 

    // 7. 清理资源
    CloseHandle(hThread);
    VirtualFreeEx(hProcess, pDllPath, 0, MEM_RELEASE);
    CloseHandle(hProcess);

    return true;
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
