namespace Launcher.Core;

/// <summary>Raised when the game client could not be started.</summary>
public sealed class ClientLaunchException : Exception
{
    public ClientLaunchException(string message)
        : base(message)
    {
    }

    public ClientLaunchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
