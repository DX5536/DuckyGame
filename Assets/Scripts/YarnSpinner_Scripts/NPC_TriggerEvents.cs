using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
public class NPC_TriggerEvents : MonoBehaviour
{
    [Header("Trigger Events")]
    [SerializeField] private UnityEvent onCollide;
    [SerializeField] private UnityEvent onStay;
    [SerializeField] private UnityEvent onExit;

    [Header("Interact (fires when player is inside AND presses the interact key)")]
    [SerializeField] private KeyBinding_ScriptableObject keyBindings;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private UnityEvent onInteract;

    private bool playerInside;

    private void Reset()
    {
        // Convenience: make sure the collider is a trigger the moment this script is added in the editor.
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null) box.isTrigger = true;
    }

    private void Update()
    {
        if (!playerInside) return;
        if (keyBindings == null || Keyboard.current == null) return;

        if (Keyboard.current[keyBindings.InteractObject].wasPressedThisFrame)
        {
            onInteract?.Invoke();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        onCollide?.Invoke();
        if (other.CompareTag(playerTag)) playerInside = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        onStay?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        onExit?.Invoke();
        if (other.CompareTag(playerTag)) playerInside = false;
    }
}
