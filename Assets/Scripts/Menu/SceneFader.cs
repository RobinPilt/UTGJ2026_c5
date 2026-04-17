using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    public Image fadeImage;
    public float fadeSpeed = 1f;

    void Start()
    {
        StartCoroutine(Fade(1, 0));
    }

    public void FadeTo(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * fadeSpeed;
            float a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = new Color(0, 0, 0, a);
            yield return null;
        }
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        yield return StartCoroutine(Fade(0, 1));
        SceneManager.LoadScene(sceneName);
    }
}