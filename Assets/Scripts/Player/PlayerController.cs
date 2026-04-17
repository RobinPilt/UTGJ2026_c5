using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 7f;
    private Rigidbody rb;
    private Vector3 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Only get input if we aren't in a dialogue
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive)
        {
            movement = Vector3.zero;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.z = Input.GetAxisRaw("Vertical");
    }

    void FixedUpdate()
    {
        // Apply movement. Normalizing prevents faster diagonal movement.
        rb.MovePosition(rb.position + movement.normalized * speed * Time.fixedDeltaTime);
    }
}