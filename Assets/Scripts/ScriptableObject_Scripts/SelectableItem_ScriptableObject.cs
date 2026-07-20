using UnityEngine;

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

    [Header("Item Image (drag a Sprite asset)")]
    [SerializeField] private Sprite itemIMG;
    public Sprite ItemIMG
    {
        get { return itemIMG; }
        set { itemIMG = value; }
    }
}
