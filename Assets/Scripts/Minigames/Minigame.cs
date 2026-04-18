using UnityEngine;

/// <summary>
/// Base class for all minigames. Co-devs inherit from this.
/// 
/// USAGE:
///   1. Create a new script, e.g. SimonSaysMinigame.cs
///   2. Inherit from Minigame instead of MonoBehaviour
///   3. Implement OnBegin() and optionally OnCleanup()
///   4. Call Complete(true) or Complete(false) when done
///   5. Attach to a UI panel GameObject, assign in MinigameManager
/// 
/// EXAMPLE:
///   public class SimonSaysMinigame : Minigame
///   {
///       protected override void OnBegin() { /* start your logic */ }
///       protected override void OnCleanup() { /* reset state */ }
///   }
/// </summary>
public abstract class Minigame : MonoBehaviour
{
    // ── Called by MinigameManager ─────────────────────────────────────

    /// <summary>Activates the panel and starts the minigame.</summary>
    public void Begin()
    {
        gameObject.SetActive(true);
        OnBegin();
    }

    /// <summary>Deactivates the panel and cleans up.</summary>
    public void End()
    {
        OnCleanup();
        gameObject.SetActive(false);
    }

    // ── Co-dev interface ─────────────────────────────────────────────

    /// <summary>Put your minigame startup logic here.</summary>
    protected abstract void OnBegin();

    /// <summary>
    /// Optional. Reset any internal state here so the minigame
    /// is clean if played again in a later round.
    /// </summary>
    protected virtual void OnCleanup() { }

    /// <summary>
    /// Call this from your minigame when the player wins or loses.
    /// true  = success, false = failure.
    /// </summary>
    protected void Complete(bool success)
    {
        MinigameManager.Instance.ResolveCurrentMinigame(success);
    }
}