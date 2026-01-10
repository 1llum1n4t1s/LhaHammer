using Microsoft.Win32;
using LhaHammer.Models;

namespace LhaHammer.ShellIntegration;

/// <summary>
/// Manages file associations for archive formats
/// </summary>
public class FileAssociationManager
{
    private const string ProgId = "LhaHammer.Archive";
    private const string AppName = "LhaHammer";
    private const string AppDescription = "LhaHammer Archive Manager";

    private static readonly Dictionary<ArchiveFormat, string[]> FormatExtensions = new()
    {
        [ArchiveFormat.Zip] = new[] { ".zip", ".zipx" },
        [ArchiveFormat.SevenZip] = new[] { ".7z" },
        [ArchiveFormat.Tar] = new[] { ".tar" },
        [ArchiveFormat.GZip] = new[] { ".gz", ".tgz", ".tar.gz" },
        [ArchiveFormat.BZip2] = new[] { ".bz2", ".tbz2", ".tar.bz2" },
        [ArchiveFormat.Xz] = new[] { ".xz", ".txz" },
        [ArchiveFormat.Lzma] = new[] { ".lzma" },
        [ArchiveFormat.Rar] = new[] { ".rar" },
        [ArchiveFormat.Lzh] = new[] { ".lzh", ".lha" },
        [ArchiveFormat.Cab] = new[] { ".cab" },
        [ArchiveFormat.Iso] = new[] { ".iso" }
    };

    /// <summary>
    /// Registers file associations for specified formats
    /// </summary>
    public void RegisterFileAssociations(IEnumerable<ArchiveFormat> formats)
    {
        if (!RegistryHelper.IsAdministrator())
        {
            throw new UnauthorizedAccessException("Administrator privileges required to register file associations");
        }

        var executablePath = Environment.ProcessPath!;
        var iconPath = executablePath;

        // Register ProgId
        RegisterProgId(executablePath, iconPath);

        // Register each extension
        foreach (var format in formats)
        {
            if (!FormatExtensions.ContainsKey(format))
                continue;

            foreach (var extension in FormatExtensions[format])
            {
                RegisterExtension(extension);
            }
        }

        // Notify shell of changes
        NotifyShellOfChanges();
    }

    /// <summary>
    /// Unregisters file associations for specified formats
    /// </summary>
    public void UnregisterFileAssociations(IEnumerable<ArchiveFormat> formats)
    {
        if (!RegistryHelper.IsAdministrator())
        {
            throw new UnauthorizedAccessException("Administrator privileges required to unregister file associations");
        }

        foreach (var format in formats)
        {
            if (!FormatExtensions.ContainsKey(format))
                continue;

            foreach (var extension in FormatExtensions[format])
            {
                UnregisterExtension(extension);
            }
        }

        // Notify shell of changes
        NotifyShellOfChanges();
    }

    /// <summary>
    /// Checks if file associations are registered for specified formats
    /// </summary>
    public bool AreFileAssociationsRegistered(IEnumerable<ArchiveFormat> formats)
    {
        foreach (var format in formats)
        {
            if (!FormatExtensions.ContainsKey(format))
                continue;

            foreach (var extension in FormatExtensions[format])
            {
                if (!IsExtensionRegistered(extension))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets all supported extensions
    /// </summary>
    public static IEnumerable<string> GetAllExtensions()
    {
        return FormatExtensions.Values.SelectMany(e => e);
    }

    /// <summary>
    /// Gets extensions for a specific format
    /// </summary>
    public static IEnumerable<string> GetExtensions(ArchiveFormat format)
    {
        return FormatExtensions.TryGetValue(format, out var extensions) ? extensions : Array.Empty<string>();
    }

    private void RegisterProgId(string executablePath, string iconPath)
    {
        using var progIdKey = RegistryHelper.CreateKey(Registry.ClassesRoot, ProgId);
        if (progIdKey == null) return;

        RegistryHelper.SetValue(progIdKey, "", AppDescription);
        RegistryHelper.SetValue(progIdKey, "FriendlyTypeName", AppName);

        // Set default icon
        using var iconKey = RegistryHelper.CreateKey(progIdKey, "DefaultIcon");
        if (iconKey != null)
        {
            RegistryHelper.SetValue(iconKey, "", $"{iconPath},0");
        }

        // Set open command
        using var shellKey = RegistryHelper.CreateKey(progIdKey, "shell");
        using var openKey = RegistryHelper.CreateKey(shellKey!, "open");
        using var commandKey = RegistryHelper.CreateKey(openKey!, "command");
        if (commandKey != null)
        {
            RegistryHelper.SetValue(commandKey, "", $"\"{executablePath}\" \"%1\"");
        }
    }

    private void RegisterExtension(string extension)
    {
        using var extensionKey = RegistryHelper.CreateKey(Registry.ClassesRoot, extension);
        if (extensionKey == null) return;

        // Backup existing association
        var existingProgId = RegistryHelper.GetValue(extensionKey, "") as string;
        if (!string.IsNullOrEmpty(existingProgId) && existingProgId != ProgId)
        {
            RegistryHelper.SetValue(extensionKey, $"{ProgId}_backup", existingProgId);
        }

        // Set our ProgId
        RegistryHelper.SetValue(extensionKey, "", ProgId);

        // Add to OpenWithProgids
        using var openWithKey = RegistryHelper.CreateKey(extensionKey, "OpenWithProgids");
        if (openWithKey != null)
        {
            RegistryHelper.SetValue(openWithKey, ProgId, new byte[0], RegistryValueKind.None);
        }
    }

    private void UnregisterExtension(string extension)
    {
        try
        {
            using var extensionKey = Registry.ClassesRoot.OpenSubKey(extension, true);
            if (extensionKey == null) return;

            var currentProgId = RegistryHelper.GetValue(extensionKey, "") as string;
            if (currentProgId == ProgId)
            {
                // Restore backup if exists
                var backup = RegistryHelper.GetValue(extensionKey, $"{ProgId}_backup") as string;
                if (!string.IsNullOrEmpty(backup))
                {
                    RegistryHelper.SetValue(extensionKey, "", backup);
                    extensionKey.DeleteValue($"{ProgId}_backup", false);
                }
                else
                {
                    extensionKey.DeleteValue("", false);
                }
            }

            // Remove from OpenWithProgids
            using var openWithKey = extensionKey.OpenSubKey("OpenWithProgids", true);
            if (openWithKey != null)
            {
                openWithKey.DeleteValue(ProgId, false);
            }
        }
        catch
        {
            // Ignore errors during unregistration
        }
    }

    private bool IsExtensionRegistered(string extension)
    {
        try
        {
            using var extensionKey = Registry.ClassesRoot.OpenSubKey(extension);
            if (extensionKey == null) return false;

            var progId = RegistryHelper.GetValue(extensionKey, "") as string;
            return progId == ProgId;
        }
        catch
        {
            return false;
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
