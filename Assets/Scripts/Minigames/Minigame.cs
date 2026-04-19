using UnityEngine;

public abstract class Minigame : MonoBehaviour
{
    public void Begin()
    {
        gameObject.SetActive(true);
        OnBegin();
    }

    public void End()
    {
        OnCleanup();
        gameObject.SetActive(false);
    }

    protected abstract void OnBegin();
    protected virtual void OnCleanup() { }

    public virtual void OnPipelineEnable() { }
    public virtual void OnPipelineDisable() { }

    protected void Complete(bool success)
    {
        MinigameManager.Instance.ResolveCurrentMinigame(success);
    }
}