using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the outro sequence:
///   fade in → typewriter text (good or bad) → glitch → fade out → video → main menu
/// </summary>
public class OutroSceneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TypewriterText typewriter;
    [SerializeField] private GlitchEffect glitchImage;
    [SerializeField] private ScreenFader fader;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private RenderTexture videoRT;

    [Header("Good Ending")]
    [SerializeField] private VideoClip goodEndingClip;
    [SerializeField]
    private string[] goodEndingLines =
    {
        "Remarkable.",
        "You have exceeded expectations.",
        "Perhaps there is hope for you yet."
    };

    [Header("Bad Ending")]
    [SerializeField] private VideoClip badEndingClip;
    [SerializeField]
    private string[] badEndingLines =
    {
        "Disappointing.",
        "You have failed the signal.",
        "We will not be trying again."
    };

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Timing")]
    [SerializeField] private float pauseAfterLine = 0.8f;
    [SerializeField] private float glitchDuration = 0.5f;
    [SerializeField] private float finalPause = 1.2f;
    [SerializeField] private float fadeToVideoDelay = 0.3f;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Start()
    {
        videoDisplay.gameObject.SetActive(false);
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRT;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        StartCoroutine(OutroSequence());
    }

    private void OnDestroy()
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    // ── Sequence ─────────────────────────────────────────────────────
    private IEnumerator OutroSequence()
    {
        bool goodEnding = GameResult.Instance != null && GameResult.Instance.IsGoodEnding;
        string[] lines = goodEnding ? goodEndingLines : badEndingLines;
        VideoClip clip = goodEnding ? goodEndingClip : badEndingClip;

        // Fade in from black
        yield return FadeIn();

        // Play lines with glitch between them
        for (int i = 0; i < lines.Length; i++)
        {
            yield return TypeLine(lines[i]);
            yield return new WaitForSeconds(pauseAfterLine);

            if (i < lines.Length - 1)
                yield return RunGlitch();
        }

        yield return new WaitForSeconds(finalPause);
        yield return FadeOut();

        // Hand off to video
        yield return PlayVideoSequence(clip);
    }

    // ── Video ─────────────────────────────────────────────────────────
    private IEnumerator PlayVideoSequence(VideoClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("OutroSceneManager: no video clip assigned, loading menu directly.");
            LoadMainMenu();
            yield break;
        }

        videoPlayer.clip = clip;
        videoDisplay.gameObject.SetActive(true);

        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        yield return new WaitForSeconds(fadeToVideoDelay);

        // Fade in over video
        bool fadeInDone = false;
        fader.FadeIn(() => fadeInDone = true);
        yield return new WaitUntil(() => fadeInDone);

        videoPlayer.Play();
        // OnVideoFinished handles the rest
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(FinishAndLoadMenu());
    }

    private IEnumerator FinishAndLoadMenu()
    {
        bool fadeComplete = false;
        fader.FadeOut(() => fadeComplete = true);
        yield return new WaitUntil(() => fadeComplete);

        videoDisplay.gameObject.SetActive(false);
        videoPlayer.Stop();
        LoadMainMenu();
    }

    private void LoadMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager
            .LoadScene(mainMenuSceneName);
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private IEnumerator TypeLine(string line)
    {
        bool done = false;
        typewriter.Write(line, () => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator RunGlitch()
    {
        bool done = false;
        glitchImage.Glitch(glitchDuration, () => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator FadeIn()
    {
        bool done = false;
        fader.FadeIn(() => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator FadeOut()
    {
        bool done = false;
        fader.FadeOut(() => done = true);
        yield return new WaitUntil(() => done);
    }
}