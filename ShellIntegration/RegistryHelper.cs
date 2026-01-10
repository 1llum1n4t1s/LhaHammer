using Microsoft.Win32;
using System.Security;

namespace LhaHammer.ShellIntegration;

/// <summary>
/// Helper class for Windows Registry operations
/// </summary>
public static class RegistryHelper
{
    /// <summary>
    /// Checks if the current process has administrator privileges
    /// </summary>
    public static bool IsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a registry key with error handling
    /// </summary>
    public static RegistryKey? CreateKey(RegistryKey baseKey, string subKeyName)
    {
        try
        {
            return baseKey.CreateSubKey(subKeyName, true);
        }
        catch (SecurityException)
        {
            throw new UnauthorizedAccessException($"Administrator privileges required to create registry key: {subKeyName}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create registry key: {subKeyName}", ex);
        }
    }

    /// <summary>
    /// Deletes a registry key with error handling
    /// </summary>
    public static void DeleteKey(RegistryKey baseKey, string subKeyName, bool throwOnMissingSubKey = false)
    {
        try
        {
            baseKey.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey);
        }
        catch (SecurityException)
        {
            throw new UnauthorizedAccessException($"Administrator privileges required to delete registry key: {subKeyName}");
        }
        catch (ArgumentException)
        {
            if (throwOnMissingSubKey)
                throw;
        }
    }

    /// <summary>
    /// Sets a registry value with error handling
    /// </summary>
    public static void SetValue(RegistryKey key, string valueName, object value, RegistryValueKind valueKind = RegistryValueKind.String)
    {
        try
        {
            key.SetValue(valueName, value, valueKind);
        }
        catch (SecurityException)
        {
            throw new UnauthorizedAccessException($"Administrator privileges required to set registry value: {valueName}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set registry value: {valueName}", ex);
        }
    }

    /// <summary>
    /// Gets a registry value with error handling
    /// </summary>
    public static object? GetValue(RegistryKey key, string valueName, object? defaultValue = null)
    {
        try
        {
            return key.GetValue(valueName, defaultValue);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Checks if a registry key exists
    /// </summary>
    public static bool KeyExists(RegistryKey baseKey, string subKeyName)
    {
        try
        {
            using var key = baseKey.OpenSubKey(subKeyName);
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Restarts the current application with administrator privileges
    /// </summary>
    public static void RestartAsAdministrator()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = Environment.ProcessPath!,
            UseShellExecute = true,
            Verb = "runas" // Request elevation
        };

        try
        {
            System.Diagnostics.Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to restart as administrator", ex);
        }
    }
}
