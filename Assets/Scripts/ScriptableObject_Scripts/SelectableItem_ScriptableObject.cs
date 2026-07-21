using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SelectableItem", menuName = "ScriptableObject/SelectableItem", order = 2)]
public class SelectableItem_ScriptableObject : ScriptableObject
{
    [Header("Item Name")]
    [SerializeField] private string itemName;
    public string ItemName
    {
        get { return itemName; }
        set { itemName = value; }
    }

    [Header("Item Description")]
    [SerializeField, TextArea] private string itemDescription;
    public string ItemDescription
    {
        get { return itemDescription; }
        set { itemDescription = value; }
    }

    [Header("Item Sprite (drag a Sprite asset)")]
    [FormerlySerializedAs("itemIMG")]
    [SerializeField] private Sprite itemSprite;
    public Sprite ItemSprite
    {
        get { return itemSprite; }
        set { itemSprite = value; }
    }

    [Header("Can the player interact with this item")]
    [SerializeField] private bool isInteractable;
    public bool IsInteractable
    {
        get { return isInteractable; }
        set { isInteractable = value; }
    }

    [Header("Materials (get only)")]
    [SerializeField] private Material defaultMAT;
    public Material DefaultMAT
    {
        get { return defaultMAT; }
    }

    [SerializeField] private Material highlightMAT;
    public Material HighlightMAT
    {
        get { return highlightMAT; }
    }

    [SerializeField] private Material uninteractableMAT;
    public Material UninteractableMAT
    {
        get { return uninteractableMAT; }
    }
}
