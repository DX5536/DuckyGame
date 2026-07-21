using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Swaps a GameObject's material between DefaultMAT / HighlightMAT / UninteractableMAT
/// based on the assigned SelectableItem or InteractableNPC scriptable object.
/// Auto-wires cursor hover (for items) and 2D trigger collisions (for NPCs), but every
/// method is also public so it can be called from other scripts or Yarn Spinner.
/// </summary>
public class Interactable_Highlight_MaterialSwap : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Data (assign ONE of the two)")]
    [Tooltip("Assign this if the GameObject is a selectable item.")]
    [SerializeField] private SelectableItem_ScriptableObject itemData;
    [Tooltip("Assign this if the GameObject is an NPC.")]
    [SerializeField] private InteractableNPC npcData;

    [Header("Collision Tags")]
    [SerializeField] private string playerTag = "Player";

    [Header("Timing")]
    [Tooltip("Small delay between resetting the base material and applying the highlight, so state changes don't step on each other.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float highlightDelay = 0.02f;

    [Header("Auto-wire built-in event handlers")]
    [Tooltip("If true, this script reacts to IPointerEnter/Exit (items) and OnTriggerEnter2D/Exit2D with a Player tag (NPCs) on its own. Turn off if you're wiring everything manually through UnityEvents.")]
    [SerializeField] private bool autoWireEvents = true;

    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private Coroutine highlightRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
    }

    private void Start()
    {
        // Ensure the base material is correct at scene start (in case editor left it wrong).
        UpdateGameObjectMaterial();
    }

    // ---------- Public API ----------

    /// <summary>
    /// Sets the base material - UninteractableMAT when isInteractable is false, else DefaultMAT.
    /// Also cancels any in-flight highlight delay. Safe to call from anywhere.
    /// </summary>
    public void UpdateGameObjectMaterial()
    {
        StopHighlightRoutine();
        Material baseMat = GetBaseMaterial();
        ApplyMaterial(baseMat);
    }

    /// <summary>
    /// Applies HighlightMAT if this GameObject is a selectable item AND is interactable.
    /// NPCs are ignored here - use HighlightNPC() for those.
    /// </summary>
    public void HighlightItem()
    {
        if (IsNPC()) return;
        if (itemData == null || !itemData.IsInteractable) return;
        if (itemData.HighlightMAT == null) return;

        StartHighlight(itemData.HighlightMAT);
    }

    /// <summary>
    /// Applies HighlightMAT if this GameObject is an NPC AND is interactable.
    /// </summary>
    public void HighlightNPC()
    {
        if (!IsNPC()) return;
        if (npcData == null || !npcData.IsInteractable) return;
        if (npcData.HighlightMAT == null) return;

        StartHighlight(npcData.HighlightMAT);
    }

    // ---------- Auto-wired events ----------

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!autoWireEvents) return;
        HighlightItem();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!autoWireEvents) return;
        UpdateGameObjectMaterial();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoWireEvents) return;
        if (other.CompareTag(playerTag)) HighlightNPC();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!autoWireEvents) return;
        if (other.CompareTag(playerTag)) UpdateGameObjectMaterial();
    }

    // ---------- Internals ----------

    private void StartHighlight(Material highlightMat)
    {
        StopHighlightRoutine();
        highlightRoutine = StartCoroutine(HighlightRoutine(highlightMat));
    }

    private void StopHighlightRoutine()
    {
        if (highlightRoutine != null)
        {
            StopCoroutine(highlightRoutine);
            highlightRoutine = null;
        }
    }

    private IEnumerator HighlightRoutine(Material highlightMat)
    {
        // Reset base material first, wait a beat, then apply the highlight.
        ApplyMaterial(GetBaseMaterial());
        if (highlightDelay > 0f) yield return new WaitForSeconds(highlightDelay);
        else yield return null;
        ApplyMaterial(highlightMat);
        highlightRoutine = null;
    }

    private Material GetBaseMaterial()
    {
        if (npcData != null)
        {
            return npcData.IsInteractable ? npcData.DefaultMAT : npcData.UninteractableMAT;
        }
        if (itemData != null)
        {
            return itemData.IsInteractable ? itemData.DefaultMAT : itemData.UninteractableMAT;
        }
        return null;
    }

    private void ApplyMaterial(Material mat)
    {
        if (mat == null) return;
        if (spriteRenderer != null) spriteRenderer.material = mat;
        else if (uiImage != null) uiImage.material = mat;
    }

    private bool IsNPC()
    {
        return npcData != null;
    }
}
