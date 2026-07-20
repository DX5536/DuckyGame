using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "KeyBinding", menuName = "ScriptableObject/KeyBinding", order = 1)]
public class KeyBinding_ScriptableObject : ScriptableObject
{
    [Header("Is Moving (master toggle - if false, input is ignored)")]
    [SerializeField] private bool isMoving = true;
    public bool IsMoving
    {
        get { return isMoving; }
        set { isMoving = value; }
    }

    [Header("Move Left")]
    [SerializeField] private Key moveLeft = Key.A;
    public Key MoveLeft
    {
        get { return moveLeft; }
        set { moveLeft = value; }
    }

    [Header("Move Right")]
    [SerializeField] private Key moveRight = Key.D;
    public Key MoveRight
    {
        get { return moveRight; }
        set { moveRight = value; }
    }

    [Header("Jump")]
    [SerializeField] private Key jump = Key.Space;
    public Key Jump
    {
        get { return jump; }
        set { jump = value; }
    }

    [Header("Crouch")]
    [SerializeField] private Key crouch = Key.LeftCtrl;
    public Key Crouch
    {
        get { return crouch; }
        set { crouch = value; }
    }

    [Header("Interact Object")]
    [SerializeField] private Key interactObject = Key.E;
    public Key InteractObject
    {
        get { return interactObject; }
        set { interactObject = value; }
    }

    [Header("Inventory")]
    [SerializeField] private Key inventory = Key.I;
    public Key Inventory
    {
        get { return inventory; }
        set { inventory = value; }
    }

    [Header("Notepad")]
    [SerializeField] private Key notepad = Key.N;
    public Key Notepad
    {
        get { return notepad; }
        set { notepad = value; }
    }

    [Header("Pause")]
    [SerializeField] private Key pause = Key.Escape;
    public Key Pause
    {
        get { return pause; }
        set { pause = value; }
    }
}
