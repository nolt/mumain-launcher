namespace Launcher.Core;

/// <summary>Stage the updater is currently in.</summary>
public enum UpdatePhase
{
    FetchingManifest,
    CheckingFiles,
    Downloading,
    Completed,
}

/// <summary>
/// A snapshot of update progress, reported to the UI. Byte counts are only
/// meaningful during <see cref="UpdatePhase.Downloading"/>; file counts during
/// <see cref="UpdatePhase.CheckingFiles"/> and <see cref="UpdatePhase.Downloading"/>.
/// </summary>
public sealed record UpdateProgress(
    UpdatePhase Phase,
    string? CurrentFile,
    int FilesCompleted,
    int FilesTotal,
    long BytesCompleted,
    long BytesTotal);
