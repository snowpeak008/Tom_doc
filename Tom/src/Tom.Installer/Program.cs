using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace Tom.Installer;

internal static class Program
{
    private const string Version = "0.1.0";

    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var installRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tom");
            var confirm = MessageBox.Show(
                $"将安装 Tom {Version} 到：\n\n{installRoot}\n\n并创建桌面和开始菜单快捷方式。",
                "Tom 安装",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (confirm != DialogResult.OK) return 1;

            Install(installRoot);

            MessageBox.Show(
                "Tom 0.1.0 安装完成。\n\n桌面和开始菜单中已创建 Tom 快捷方式。",
                "Tom 安装",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            var exePath = Path.Combine(installRoot, "Tom.exe");
            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true, WorkingDirectory = installRoot });
            return 0;
        }
        catch (Exception error)
        {
            MessageBox.Show(
                $"Tom 安装失败：\n\n{error.Message}",
                "Tom 安装",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static void Install(string installRoot)
    {
        Directory.CreateDirectory(installRoot);
        var tempZip = Path.Combine(Path.GetTempPath(), $"Tom-{Version}-{Guid.NewGuid():N}.zip");

        try
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("TomPayloadZip")
                ?? throw new InvalidOperationException("安装包缺少 Tom payload。"))
            using (var file = File.Create(tempZip))
            {
                resource.CopyTo(file);
            }

            ZipFile.ExtractToDirectory(tempZip, installRoot, overwriteFiles: true);
            EnsureWorkspaceDirectories(installRoot);
            CreateShortcuts(installRoot);
            WriteUninstaller(installRoot);
        }
        finally
        {
            try
            {
                if (File.Exists(tempZip)) File.Delete(tempZip);
            }
            catch
            {
                // Temporary cleanup failure should not fail an otherwise successful install.
            }
        }
    }

    private static void EnsureWorkspaceDirectories(string installRoot)
    {
        var root = Path.Combine(installRoot, "tom-docs");
        Directory.CreateDirectory(Path.Combine(root, "documents"));
        Directory.CreateDirectory(Path.Combine(root, "assets"));
        Directory.CreateDirectory(Path.Combine(root, "exports"));
        Directory.CreateDirectory(Path.Combine(root, "snapshots"));
        Directory.CreateDirectory(Path.Combine(root, "ai-runs"));
        Directory.CreateDirectory(Path.Combine(root, "logs"));
    }

    private static void CreateShortcuts(string installRoot)
    {
        var exePath = Path.Combine(installRoot, "Tom.exe");
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        CreateShortcut(Path.Combine(desktop, "Tom.lnk"), exePath, installRoot);

        var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        var startMenuDir = Path.Combine(programs, "Tom");
        Directory.CreateDirectory(startMenuDir);
        CreateShortcut(Path.Combine(startMenuDir, "Tom.lnk"), exePath, installRoot);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("无法创建 Windows 快捷方式。");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("无法启动 Windows 快捷方式组件。");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Description = "Tom 本地AI文档工作台";
        shortcut.Save();
    }

    private static void WriteUninstaller(string installRoot)
    {
        var path = Path.Combine(installRoot, "Uninstall_Tom.cmd");
        File.WriteAllText(path, """
@echo off
echo This will remove Tom program files from %LOCALAPPDATA%\Tom.
echo Your tom-docs data folder is kept by default.
pause
del "%USERPROFILE%\Desktop\Tom.lnk" 2>nul
rmdir /s /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Tom" 2>nul
del "%LOCALAPPDATA%\Tom\Tom.exe" 2>nul
del "%LOCALAPPDATA%\Tom\README.txt" 2>nul
del "%LOCALAPPDATA%\Tom\VERSION.txt" 2>nul
del "%LOCALAPPDATA%\Tom\Uninstall_Tom.cmd" 2>nul
echo Tom program files removed. tom-docs was kept.
pause
""", Encoding.ASCII);
    }
}
