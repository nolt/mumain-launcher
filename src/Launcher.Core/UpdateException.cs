namespace Launcher.Core;

/// <summary>
/// Raised when the client cannot be brought up to date: the manifest could not
/// be fetched, a download failed, or a downloaded file failed verification.
/// </summary>
public sealed class UpdateException : Exception
{
    public UpdateException(string message)
        : base(message)
    {
    }

    public UpdateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
