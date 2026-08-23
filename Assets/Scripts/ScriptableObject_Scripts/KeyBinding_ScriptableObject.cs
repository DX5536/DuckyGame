using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "KeyBinding", menuName = "ScriptableObject/KeyBinding", order = 1)]
public class KeyBinding_ScriptableObject : ScriptableObject
{
    [Header("Is Moving (master toggle - if false, input is ignored)")]
    [SerializeField] private bool canMove = true;
    public bool CanMove
    {
        get { return canMove; }
        set { canMove = value; }
    }

    [Header("Move (Value / Vector2)")]
    [Tooltip("Read as Vector2; only the X component is used for side-scroller movement. " +
             "Bind to a 2D Vector composite (WASD / Arrows), a gamepad stick, or an on-screen joystick.")]
    [SerializeField] private InputActionReference move;
    public InputActionReference Move
    {
        get { return move; }
    }

    [Header("Jump (Button)")]
    [SerializeField] private InputActionReference jump;
    public InputActionReference Jump
    {
        get { return jump; }
    }

    [Header("Crouch (Button - read as held)")]
    [SerializeField] private InputActionReference crouch;
    public InputActionReference Crouch
    {
        get { return crouch; }
    }

    [Header("Interact Object (Button)")]
    [SerializeField] private InputActionReference interactObject;
    public InputActionReference InteractObject
    {
        get { return interactObject; }
    }

    [Header("Inventory (Button)")]
    [SerializeField] private InputActionReference inventory;
    public InputActionReference Inventory
    {
        get { return inventory; }
    }

    [Header("Notepad (Button)")]
    [SerializeField] private InputActionReference notepad;
    public InputActionReference Notepad
    {
        get { return notepad; }
    }

    [Header("Pause (Button)")]
    [SerializeField] private InputActionReference pause;
    public InputActionReference Pause
    {
        get { return pause; }
    }
}
