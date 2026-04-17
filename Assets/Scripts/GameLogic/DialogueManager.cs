using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialogueUI;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;

    private string[] currentLines;
    private int currentLineIndex;

    // We keep this public so other scripts can check it
    [HideInInspector] public bool isDialogueActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        dialogueUI.SetActive(false);
    }

    public void StartDialogue(string[] lines, Sprite portrait)
    {
        if (isDialogueActive) return;

        currentLines = lines;
        currentLineIndex = 0;
        portraitImage.sprite = portrait;

        isDialogueActive = true;
        dialogueUI.SetActive(true);

        // Time.timeScale = 0f; // DELETED: No more pausing!

        AdvanceDialogue();
    }

    void Update()
    {
        if (isDialogueActive && Input.GetMouseButtonDown(0))
        {
            AdvanceDialogue();
        }
    }

    private void AdvanceDialogue()
    {
        if (currentLineIndex < currentLines.Length)
        {
            dialogueText.text = currentLines[currentLineIndex];
            currentLineIndex++;
        }
        else
        {
            isDialogueActive = false;
            dialogueUI.SetActive(false);
            Time.timeScale = 1f;
        }
    }
}