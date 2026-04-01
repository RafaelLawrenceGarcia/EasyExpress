/// <summary>
/// GameSession — Lightweight static tracker for the current play session.
///
/// Set once at login / guest-start and read anywhere (GameManager, CloudDataHandler, UI, etc.).
/// Resets automatically when the app quits or when Logout() is called.
///
/// USAGE:
///   GameSession.StartCloudSession();   // after PlayFab login succeeds
///   GameSession.StartGuestSession();   // when "Play Offline" is pressed
///   GameSession.Logout();              // on logout
///
///   if (GameSession.IsLoggedIn) { ... }   // true only for cloud sessions
/// </summary>
public static class GameSession
{
    /// <summary>True when the player authenticated through PlayFab.</summary>
    public static bool IsLoggedIn { get; private set; }

    /// <summary>True when the player chose "Play Offline" (guest / local save).</summary>
    public static bool IsGuest { get; private set; }

    /// <summary>Call after a successful PlayFab login.</summary>
    public static void StartCloudSession()
    {
        IsLoggedIn = true;
        IsGuest = false;
    }

    /// <summary>Call when the player skips login to play offline.</summary>
    public static void StartGuestSession()
    {
        IsLoggedIn = false;
        IsGuest = true;
    }

    /// <summary>Call on logout — clears everything.</summary>
    public static void Logout()
    {
        IsLoggedIn = false;
        IsGuest = false;
    }
}