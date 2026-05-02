// ============================================================
//  GameSession.cs
//  Easy Express — Session State Tracker
// ============================================================
//  Location: Assets/Script/Core/GameSession.cs
//  (Moved from: Online Logics/SessionState.cs)
//
//  Lightweight static tracker. No changes to the code —
//  just moved to Core/ and renamed file to match class name.
// ============================================================

public static class GameSession
{
    /// <summary>True when the player authenticated through PlayFab.</summary>
    public static bool IsLoggedIn { get; private set; }

    /// <summary>True when the player chose "Play Offline".</summary>
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