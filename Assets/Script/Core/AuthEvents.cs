// ============================================================
//  AuthEvents.cs
//  Easy Express — Authentication Event Definitions
// ============================================================
//  Location: Assets/Script/Core/AuthEvents.cs
//
//  These structs are the ONLY contract between Auth/ and MainMenu/.
//  Neither system needs to know the other's class name.
// ============================================================

/// <summary>Published after successful PlayFab login.</summary>
public struct LoginSuccessEvent { }

/// <summary>Published when player chooses "Play Offline".</summary>
public struct GuestLoginEvent { }

/// <summary>Published when any system requests logout.</summary>
public struct LogoutRequestedEvent { }