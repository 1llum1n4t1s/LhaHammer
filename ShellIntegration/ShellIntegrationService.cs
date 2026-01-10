using Microsoft.Win32;

namespace LhaHammer.ShellIntegration;

/// <summary>
/// Manages Windows Shell integration (context menus)
/// </summary>
public class ShellIntegrationService
{
    private const string AppName = "LhaHammer";
    private const string ContextMenuKey = @"*\shell\LhaHammer";
    private const string DirectoryContextMenuKey = @"Directory\shell\LhaHammer";
    private const string FolderContextMenuKey = @"Folder\shell\LhaHammer";

    private readonly FileAssociationManager _fileAssociationManager;

    public ShellIntegrationService()
    {
        _fileAssociationManager = new FileAssociationManager();
    }

    /// <summary>
    /// Registers shell context menu entries
    /// </summary>
    public void RegisterShellExtension()
    {
        if (!RegistryHelper.IsAdministrator())
        {
            throw new UnauthorizedAccessException("Administrator privileges required to register shell extension");
        }

        var executablePath = Environment.ProcessPath!;

        // Register context menu for files
        RegisterFileContextMenu(executablePath);

        // Register context menu for directories
        RegisterDirectoryContextMenu(executablePath);

        // Notify shell of changes
        NotifyShellOfChanges();
    }

    /// <summary>
    /// Unregisters shell context menu entries
    /// </summary>
    public void UnregisterShellExtension()
    {
        if (!RegistryHelper.IsAdministrator())
        {
            throw new UnauthorizedAccessException("Administrator privileges required to unregister shell extension");
        }

        try
        {
            // Remove file context menu
            RegistryHelper.DeleteKey(Registry.ClassesRoot, ContextMenuKey);

            // Remove directory context menu
            RegistryHelper.DeleteKey(Registry.ClassesRoot, DirectoryContextMenuKey);

            // Remove folder context menu
            RegistryHelper.DeleteKey(Registry.ClassesRoot, FolderContextMenuKey);
        }
        catch
        {
            // Ignore errors during unregistration
        }

        // Notify shell of changes
        NotifyShellOfChanges();
    }

    /// <summary>
    /// Checks if shell extension is registered
    /// </summary>
    public bool IsShellExtensionRegistered()
    {
        return RegistryHelper.KeyExists(Registry.ClassesRoot, ContextMenuKey) ||
               RegistryHelper.KeyExists(Registry.ClassesRoot, DirectoryContextMenuKey);
    }

    /// <summary>
    /// Gets the file association manager
    /// </summary>
    public FileAssociationManager GetFileAssociationManager()
    {
        return _fileAssociationManager;
    }

    private void RegisterFileContextMenu(string executablePath)
    {
        // Main menu item
        using var mainKey = RegistryHelper.CreateKey(Registry.ClassesRoot, ContextMenuKey);
        if (mainKey == null) return;

        RegistryHelper.SetValue(mainKey, "", $"{AppName}");
        RegistryHelper.SetValue(mainKey, "MUIVerb", $"{AppName}");
        RegistryHelper.SetValue(mainKey, "Icon", $"\"{executablePath}\",0");
        RegistryHelper.SetValue(mainKey, "SubCommands", "");

        // Extract Here submenu
        using var extractKey = RegistryHelper.CreateKey(Registry.ClassesRoot, $"{ContextMenuKey}\\shell\\extract");
        if (extractKey != null)
        {
            RegistryHelper.SetValue(extractKey, "", "Extract Here");
            RegistryHelper.SetValue(extractKey, "Icon", $"\"{executablePath}\",0");

            using var extractCommandKey = RegistryHelper.CreateKey(extractKey, "command");
            if (extractCommandKey != null)
            {
                RegistryHelper.SetValue(extractCommandKey, "", $"\"{executablePath}\" extract \"%1\" \"%1\\..\"");
            }
        }

        // Extract to Folder submenu
        using var extractFolderKey = RegistryHelper.CreateKey(Registry.ClassesRoot, $"{ContextMenuKey}\\shell\\extractfolder");
        if (extractFolderKey != null)
        {
            RegistryHelper.SetValue(extractFolderKey, "", "Extract to Folder...");
            RegistryHelper.SetValue(extractFolderKey, "Icon", $"\"{executablePath}\",0");

            using var extractFolderCommandKey = RegistryHelper.CreateKey(extractFolderKey, "command");
            if (extractFolderCommandKey != null)
            {
                RegistryHelper.SetValue(extractFolderCommandKey, "", $"\"{executablePath}\" extract \"%1\"");
            }
        }

        // Test Archive submenu
        using var testKey = RegistryHelper.CreateKey(Registry.ClassesRoot, $"{ContextMenuKey}\\shell\\test");
        if (testKey != null)
        {
            RegistryHelper.SetValue(testKey, "", "Test Archive");
            RegistryHelper.SetValue(testKey, "Icon", $"\"{executablePath}\",0");

            using var testCommandKey = RegistryHelper.CreateKey(testKey, "command");
            if (testCommandKey != null)
            {
                RegistryHelper.SetValue(testCommandKey, "", $"\"{executablePath}\" test \"%1\"");
            }
        }

        // Open with LhaHammer submenu
        using var openKey = RegistryHelper.CreateKey(Registry.ClassesRoot, $"{ContextMenuKey}\\shell\\open");
        if (openKey != null)
        {
            RegistryHelper.SetValue(openKey, "", "Open with LhaHammer");
            RegistryHelper.SetValue(openKey, "Icon", $"\"{executablePath}\",0");

            using var openCommandKey = RegistryHelper.CreateKey(openKey, "command");
            if (openCommandKey != null)
            {
                RegistryHelper.SetValue(openCommandKey, "", $"\"{executablePath}\" \"%1\"");
            }
        }
    }

    private void RegisterDirectoryContextMenu(string executablePath)
    {
        // Compress to ZIP
        RegisterDirectoryCompressionMenu(DirectoryContextMenuKey, executablePath, "zip", "Compress to ZIP");
        RegisterDirectoryCompressionMenu(FolderContextMenuKey, executablePath, "zip", "Compress to ZIP");

        // Compress to 7z
        RegisterDirectoryCompressionMenu(DirectoryContextMenuKey, executablePath, "7z", "Compress to 7z");
        RegisterDirectoryCompressionMenu(FolderContextMenuKey, executablePath, "7z", "Compress to 7z");

        // Compress to TAR.GZ
        RegisterDirectoryCompressionMenu(DirectoryContextMenuKey, executablePath, "tar.gz", "Compress to TAR.GZ");
        RegisterDirectoryCompressionMenu(FolderContextMenuKey, executablePath, "tar.gz", "Compress to TAR.GZ");
    }

    private void RegisterDirectoryCompressionMenu(string baseKey, string executablePath, string format, string menuText)
    {
        var menuKey = $"{baseKey}\\Compress{format}";
        using var key = RegistryHelper.CreateKey(Registry.ClassesRoot, menuKey);
        if (key == null) return;

        RegistryHelper.SetValue(key, "", menuText);
        RegistryHelper.SetValue(key, "Icon", $"\"{executablePath}\",0");

        using var commandKey = RegistryHelper.CreateKey(key, "command");
        if (commandKey != null)
        {
            RegistryHelper.SetValue(commandKey, "", $"\"{executablePath}\" compress \"%1.{format}\" \"%1\"");
        }
    }

    private void NotifyShellOfChanges()
    {
        // SHChangeNotify to refresh shell
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
