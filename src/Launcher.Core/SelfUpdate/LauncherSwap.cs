namespace Launcher.Core.SelfUpdate;

/// <summary>
/// File operations that replace the launcher binary with a freshly downloaded one.
/// Uses renames only - a running executable can be renamed (Windows allows this
/// even though it forbids overwriting), so the same steps work on every platform.
/// Pure file work, so the full cycle can be tested without starting a process.
/// </summary>
public static class LauncherSwap
{
    public const string OldSuffix = ".old";
    public const string NewSuffix = ".new";

    /// <summary>Path the new binary must be written to before <see cref="Apply"/>.</summary>
    public static string NewPathFor(string currentExePath) => currentExePath + NewSuffix;

    /// <summary>
    /// Renames the current binary aside to <c>.old</c> and moves the downloaded
    /// <c>.new</c> into its place. The old binary is removed on the next start.
    /// </summary>
    public static void Apply(string currentExePath)
    {
        var oldPath = currentExePath + OldSuffix;
        var newPath = NewPathFor(currentExePath);

        DeleteIfExists(oldPath);
        File.Move(currentExePath, oldPath);
        File.Move(newPath, currentExePath);
    }

    /// <summary>Removes the leftover <c>.old</c> binary from a previous self-update.</summary>
    public static void CleanupOld(string currentExePath) => DeleteIfExists(currentExePath + OldSuffix);

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
