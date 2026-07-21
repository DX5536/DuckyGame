using UnityEngine;

[CreateAssetMenu(fileName = "InteractableNPC", menuName = "ScriptableObject/InteractableNPC", order = 3)]
public class InteractableNPC : ScriptableObject
{
    [Header("NPC Name")]
    [SerializeField] private string npcName;
    public string NPCName
    {
        get { return npcName; }
        set { npcName = value; }
    }

    [Header("NPC Descriptions (one entry per line variant)")]
    [SerializeField, TextArea] private string[] npcDescription;
    public string[] NPCDescription
    {
        get { return npcDescription; }
        set { npcDescription = value; }
    }

    [Header("NPC Sprites (different expressions)")]
    [SerializeField] private Sprite[] npcSprite;
    public Sprite[] NPCSprite
    {
        get { return npcSprite; }
        set { npcSprite = value; }
    }

    [Header("Is Interactable")]
    [SerializeField] private bool isInteractable = true;
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

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip npcIdleAnim;
    public AnimationClip NPCIdleAnim
    {
        get { return npcIdleAnim; }
    }

    [SerializeField] private AnimationClip npcWalkAnim;
    public AnimationClip NPCWalkAnim
    {
        get { return npcWalkAnim; }
    }

    [Header("Affection Value (clamped -20 to +20)")]
    [Range(-20, 20)]
    [SerializeField] private int affectionValue;
    public int AffectionValue
    {
        get { return affectionValue; }
        set { affectionValue = Mathf.Clamp(value, -20, 20); }
    }
}
