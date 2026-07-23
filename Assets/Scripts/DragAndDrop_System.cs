using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DragAndDrop_System : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Item Data")]
    [SerializeField] private SelectableItem_ScriptableObject itemData;

    [Header("Slots")]
    [Tooltip("GameObjects with this tag act as snap targets.")]
    [SerializeField] private string slotTag = "ItemSlot";

    [Header("Snap Animation")]
    [SerializeField] private float snapDuration = 0.2f;
    [SerializeField] private Ease snapEase = Ease.OutQuad;

    [Header("Return Position (optional)")]
    [Tooltip("Where the item goes when dropped outside a slot or right-clicked out of one. If empty, returns to its scene start position.")]
    [SerializeField] private Transform returnSpawnPoint;

    [Header("Events")]
    [SerializeField] private UnityEvent onHover;
    [SerializeField] private UnityEvent onLeftClick;
    [SerializeField] private UnityEvent onRightClick;
    [SerializeField] private UnityEvent onSnapped;

    private RectTransform rt;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform[] slots;
    private Vector3 originalWorldPos;
    private RectTransform currentSlot;

    // Reused so GetWorldRect doesn't allocate a Vector3[4] every drop.
    private readonly Vector3[] cornersBuffer = new Vector3[4];

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        originalWorldPos = rt.position;

        // Cache slots by tag once - cheaper than searching every drag.
        GameObject[] slotGOs = GameObject.FindGameObjectsWithTag(slotTag);
        slots = new RectTransform[slotGOs.Length];
        for (int i = 0; i < slotGOs.Length; i++)
        {
            slots[i] = slotGOs[i].GetComponent<RectTransform>();
        }
    }

    //Mouse onHover and onClick

    public void OnPointerEnter(PointerEventData e)
    {
        onHover?.Invoke();
    }

    public void OnPointerExit(PointerEventData e) { }

    public void OnPointerDown(PointerEventData e)
    {
        // Fires the instant the button is pressed, before any drag begins. Runs exactly once per press, regardless of whether the user then holds or drags.
        if (e.button == PointerEventData.InputButton.Left)
        {
            onLeftClick?.Invoke();
        }
        else if (e.button == PointerEventData.InputButton.Right)
        {
            onRightClick?.Invoke();
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        // Left-click is handled in OnPointerDown so it fires immediately (and even if a drag starts).
        // Right-click stays here because ejecting on release feels right and it never conflicts with drag.
        if (e.button == PointerEventData.InputButton.Right && currentSlot != null)
        {
            currentSlot = null;
            Vector3 dest = returnSpawnPoint != null ? returnSpawnPoint.position : originalWorldPos;
            rt.DOMove(dest, snapDuration).SetEase(snapEase);
        }
    }

    //Drag

    public void OnBeginDrag(PointerEventData e)
    {
        canvasGroup.blocksRaycasts = false; // let raycasts hit slots under the item pulled out of any current slot
        currentSlot = null;
    }

    public void OnDrag(PointerEventData e)
    {
        if (canvas == null) return;

        // Works for all Canvas render modes (Overlay / Camera / World).
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform, e.position, canvas.worldCamera, out Vector3 world))
        {
            rt.position = world;
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        canvasGroup.blocksRaycasts = true;

        RectTransform snapTarget = FindClosestOverlappingSlot();
        currentSlot = snapTarget;

        Vector3 dest = snapTarget != null
            ? snapTarget.position
            : (returnSpawnPoint != null ? returnSpawnPoint.position : originalWorldPos);

        rt.DOMove(dest, snapDuration).SetEase(snapEase).OnComplete(() =>
        {
            if (currentSlot != null) onSnapped?.Invoke();
        });
    }

    //Items automatically snap to the nearest slot

    private RectTransform FindClosestOverlappingSlot()
    {
        if (slots == null || slots.Length == 0) return null;

        Rect itemRect = GetWorldRect(rt);
        RectTransform best = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            RectTransform s = slots[i];
            if (s == null) continue;

            Rect slotRect = GetWorldRect(s);
            if (!itemRect.Overlaps(slotRect)) continue;

            // Break ties by closest pivot-to-pivot distance.
            float dSq = ((Vector2)s.position - (Vector2)rt.position).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                best = s;
            }
        }
        return best;
    }

    private Rect GetWorldRect(RectTransform target)
    {
        target.GetWorldCorners(cornersBuffer);
        Vector3 bl = cornersBuffer[0];
        Vector3 tr = cornersBuffer[2];
        return new Rect(bl.x, bl.y, tr.x - bl.x, tr.y - bl.y);
    }
}
