using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MinigameCRTController : MonoBehaviour
{
    public static MinigameCRTController Instance { get; private set; }

    [Header("RT Pipeline")]
    [SerializeField] private Camera minigameUICamera;
    [SerializeField] private RawImage crtOutput;
    [SerializeField] private Material crtMaterial;

    [Header("Aberration Flash")]
    [SerializeField] private float flashDuration = 0.35f;
    [SerializeField] private float maxAberration = 0.022f;
    [SerializeField] private float maxFlashBright = 0.25f;

    private Material _instance;
    private Coroutine _flashRoutine;
    private RenderTexture _dynamicRT; // Track this so we can release it

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 1. Create a RenderTexture that matches the CURRENT screen resolution
        // This ensures mouse (1920, 1080) maps perfectly to RT (1920, 1080)
        _dynamicRT = new RenderTexture(Screen.width, Screen.height, 24);
        _dynamicRT.name = "DynamicMinigameRT_" + Screen.width + "x" + Screen.height;

        // 2. Assign it to the Camera and the RawImage
        minigameUICamera.targetTexture = _dynamicRT;
        crtOutput.texture = _dynamicRT;

        _instance = Instantiate(crtMaterial);
        crtOutput.material = _instance;

        SetPipelineActive(false);
    }

    private void Update()
    {
        if (!minigameUICamera.gameObject.activeSelf) return;
        _instance.SetFloat("_Time2", Time.time);
    }

    private void OnDestroy()
    {
        if (_instance != null) Destroy(_instance);

        // Cleanup the dynamic texture to prevent memory leaks
        if (_dynamicRT != null)
        {
            minigameUICamera.targetTexture = null;
            _dynamicRT.Release();
            Destroy(_dynamicRT);
        }
    }

    public void SetPipelineActive(bool on)
    {
        minigameUICamera.gameObject.SetActive(on);
        crtOutput.gameObject.SetActive(on);
    }

    public void TriggerWrongPressFlash()
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            float peak = 1f - Mathf.Abs(t - 0.5f) * 2f;
            _instance.SetFloat("_AberrationStr", peak * maxAberration);
            _instance.SetFloat("_FlashStr", peak * maxFlashBright);
            yield return null;
        }
        _instance.SetFloat("_AberrationStr", 0f);
        _instance.SetFloat("_FlashStr", 0f);
        _flashRoutine = null;
    }
}