using UnityEngine;

public class NPC : MonoBehaviour
{
    [TextArea(3, 5)]
    public string[] dialogueLines;
    public Sprite characterPortrait;

    void OnCollisionEnter(Collision collision)
    {
        // When the player bumps into this blob, trigger the dialogue
        if (collision.gameObject.CompareTag("Player"))
        {
            DialogueManager.Instance.StartDialogue(dialogueLines, characterPortrait);
        }
    }
}
