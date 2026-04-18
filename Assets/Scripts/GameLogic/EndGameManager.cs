using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class EndGameManager : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip goodEndingClip;
    [SerializeField] private VideoClip badEndingClip;
    [SerializeField] private RawImage videoDisplay;   // fullscreen RawImage the video renders to

    // Replace with this single field:
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // match your scene name exactly

    [Header("Timing")]
    [SerializeField] private float fadeToVideoDelay = 0.3f; // pause on black before video starts

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Start()
    {
        videoDisplay.gameObject.SetActive(false);

        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameEnd.AddListener(TriggerEnding);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameEnd.RemoveListener(TriggerEnding);

        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    // ── Core ──────────────────────────────────────────────────────────

    public void TriggerEnding(bool goodEnding)
    {
        VideoClip clip = goodEnding ? goodEndingClip : badEndingClip;

        if (clip == null)
        {
            // No clip assigned yet — skip straight to main menu
            Debug.LogWarning($"EndGameManager: no clip assigned for {(goodEnding ? "good" : "bad")} ending. Loading main menu.");
            StartCoroutine(LoadMainMenu());
            return;
        }

        videoPlayer.clip = clip;
        StartCoroutine(EndingSequence());
    }

    private IEnumerator EndingSequence()
    {
        // 1. Fade to black — reuse ScreenFader
        bool fadeComplete = false;
        ScreenFader.Instance.FadeOut(() => fadeComplete = true);
        yield return new WaitUntil(() => fadeComplete);

        // 2. Brief pause on black, prepare the video display
        yield return new WaitForSeconds(fadeToVideoDelay);
        videoDisplay.gameObject.SetActive(true);

        // 3. Prepare the video (buffers without playing)
        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        // 4. Fade back in over the video display
        bool fadeInComplete = false;
        ScreenFader.Instance.FadeIn(() => fadeInComplete = true);
        yield return new WaitUntil(() => fadeInComplete);

        // 5. Play
        videoPlayer.Play();

        // OnVideoFinished() handles the rest when clip ends
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(LoadMainMenu());
    }

    private IEnumerator LoadMainMenu()
    {
        bool fadeComplete = false;
        ScreenFader.Instance.FadeOut(() => fadeComplete = true);
        yield return new WaitUntil(() => fadeComplete);

        videoDisplay.gameObject.SetActive(false);
        videoPlayer.Stop();

        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── Buttons ───────────────────────────────────────────────────────

    private IEnumerator ReloadAfterFade()
    {
        bool fadeComplete = false;
        ScreenFader.Instance.FadeOut(() => fadeComplete = true);
        yield return new WaitUntil(() => fadeComplete);

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}