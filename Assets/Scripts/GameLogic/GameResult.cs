using UnityEngine;

/// <summary>
/// Persists between scenes to carry the ending result.
/// Set before loading OutroScene, read by OutroSceneManager.
/// </summary>
public class GameResult : MonoBehaviour
{
    public static GameResult Instance { get; private set; }
    public bool IsGoodEnding { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Set(bool goodEnding) => IsGoodEnding = goodEnding;
}