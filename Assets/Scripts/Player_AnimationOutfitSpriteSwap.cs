using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the player's clothing SpriteRenderers so ONE set of body animations (IDLE, WALK,
/// JUMP, CROUCH...) works with ANY outfit - no need to author a clip per outfit combination.
///
/// How it works:
///  - The body animation (Player : Sprite + transforms) provides the timing/motion.
///  - Every LateUpdate (AFTER the Animator writes), this script figures out the current
///    animation status (IDLE/WALK/...) and the current frame index (0..N-1), then sets each
///    clothing part's sprite from the equipped PlayerOutfit ScriptableObject that matches.
///  - Because LateUpdate runs after the Animator, the script's sprite always wins.
///
/// Equipping an outfit just changes which ScriptableObject a slot reads - zero clip rebuilding.
/// </summary>
public class Player_AnimationOutfitSpriteSwap : MonoBehaviour
{
    // The distinct clothing slots on the character. Extend freely.
    public enum BodyPart { Glasses, Bag, Hair, Camera, Overall, Shirt, Pants, Shoes }

    [System.Serializable]
    public class BodyPartSlot
    {
        [Tooltip("Which body part this slot represents.")]
        public BodyPart part;

        [Tooltip("The item_* SpriteRenderer child for this part.")]
        public SpriteRenderer renderer;

        [Tooltip("Every outfit variant for THIS part, across all animation states " +
                 "(e.g. BagA_IDLE, BagA_WALK, BagB_IDLE, BagB_WALK...).")]
        public List<PlayerOutfit_ScriptableObject> variants = new List<PlayerOutfit_ScriptableObject>();

        [Tooltip("Outfit ID currently equipped for this part (e.g. \"BagA\"). Empty = nothing worn.")]
        public string equippedID = "";
    }

    [Header("Animator")]
    [Tooltip("The player's Animator driving the body animations.")]
    [SerializeField] private Animator animator;

    [Header("Frames")]
    [Tooltip("How many frames each animation state has (matches your clip's keyframe count).")]
    [SerializeField] private int frameCount = 4;

    [Header("Animation Statuses to detect (must match the SO's status names, ALL CAPS)")]
    [Tooltip("The script picks a status by checking if the current clip name contains one of these.")]
    [SerializeField] private string[] knownStatuses = { "IDLE", "WALK", "JUMP", "CROUCH" };
    [SerializeField] private string fallbackStatus = "IDLE";

    [Header("Body Part Slots")]
    [SerializeField] private List<BodyPartSlot> slots = new List<BodyPartSlot>();

    // part -> slot, for fast exclusivity lookups (Overall vs Shirt/Pants).
    private Dictionary<BodyPart, BodyPartSlot> slotByPart;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();

        slotByPart = new Dictionary<BodyPart, BodyPartSlot>(slots.Count);
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null) slotByPart[slots[i].part] = slots[i];
        }
    }

    private void Start()
    {
        // Enforce Overall vs Shirt/Pants at startup based on the inspector-set equippedIDs.
        EnforceOverallExclusivity();
    }

    private void LateUpdate()
    {
        // Runs after the Animator, so our sprite writes override any clip keyframes.
        string status = GetCurrentStatus();
        int frame = GetCurrentFrameIndex();

        for (int i = 0; i < slots.Count; i++)
        {
            ApplySlot(slots[i], status, frame);
        }
    }

    // ---------- Public API (wire to buttons / sliders / Yarn) ----------

    /// <summary>
    /// Equip an outfit variant by its ID (e.g. "BagA"). The part is inferred from whichever slot owns that variant. Handles Overall vs Shirt/Pants mutual exclusivity automatically.
    /// Pass an empty string (or an ID no slot owns) to do nothing.
    /// </summary>
    public void EquipOutfit(string outfitID)
    {
        if (string.IsNullOrEmpty(outfitID)) return;

        BodyPartSlot target = FindSlotOwningVariant(outfitID);
        if (target == null)
        {
            Debug.LogWarning($"[{name}] EquipOutfit: no slot has a variant with ID '{outfitID}'.", this);
            return;
        }

        target.equippedID = outfitID;
        ApplyExclusivityFor(target.part);
    }

    ///Removes whatever is worn on the given body part
    public void UnequipPart(BodyPart part)
    {
        if (slotByPart != null && slotByPart.TryGetValue(part, out BodyPartSlot slot))
        {
            slot.equippedID = "";
        }
    }

    ///String overload so a UnityEvent/button can unequip by part name (e.g. "Overall")
    public void UnequipPart(string partName)
    {
        if (System.Enum.TryParse(partName, true, out BodyPart part)) UnequipPart(part);
    }

    // ---------- Internals ----------

    private void ApplySlot(BodyPartSlot slot, string status, int frame)
    {
        if (slot == null || slot.renderer == null) return;

        // Nothing equipped -> hide the part.
        if (string.IsNullOrEmpty(slot.equippedID))
        {
            if (slot.renderer.enabled) slot.renderer.enabled = false;
            return;
        }

        // Find the SO for this part matching (equipped variant + current animation status).
        PlayerOutfit_ScriptableObject outfit = FindOutfit(slot, slot.equippedID, status);
        if (outfit == null || outfit.PlayerOutfitSprite == null || outfit.PlayerOutfitSprite.Length == 0)
        {
            if (slot.renderer.enabled) slot.renderer.enabled = false;
            return;
        }

        Sprite[] frames = outfit.PlayerOutfitSprite;
        int idx = Mathf.Clamp(frame, 0, frames.Length - 1);

        if (!slot.renderer.enabled) slot.renderer.enabled = true;
        slot.renderer.sprite = frames[idx];
    }

    private PlayerOutfit_ScriptableObject FindOutfit(BodyPartSlot slot, string outfitID, string status)
    {
        for (int i = 0; i < slot.variants.Count; i++)
        {
            PlayerOutfit_ScriptableObject o = slot.variants[i];
            if (o == null) continue;
            if (o.PlayerOutfitID == outfitID && StatusMatches(o.PlayerAnimationStatusName, status))
            {
                return o;
            }
        }
        return null;
    }

    private BodyPartSlot FindSlotOwningVariant(string outfitID)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            BodyPartSlot slot = slots[i];
            if (slot == null) continue;
            for (int v = 0; v < slot.variants.Count; v++)
            {
                if (slot.variants[v] != null && slot.variants[v].PlayerOutfitID == outfitID)
                {
                    return slot;
                }
            }
        }
        return null;
    }

    // Overall on -> Shirt & Pants off, and vice versa.
    private void ApplyExclusivityFor(BodyPart justEquipped)
    {
        if (justEquipped == BodyPart.Overall)
        {
            UnequipPart(BodyPart.Shirt);
            UnequipPart(BodyPart.Pants);
        }
        else if (justEquipped == BodyPart.Shirt || justEquipped == BodyPart.Pants)
        {
            UnequipPart(BodyPart.Overall);
        }
    }

    // Startup pass: if an Overall is already equipped in the inspector, it wins over Shirt/Pants.
    private void EnforceOverallExclusivity()
    {
        if (slotByPart != null
            && slotByPart.TryGetValue(BodyPart.Overall, out BodyPartSlot overall)
            && !string.IsNullOrEmpty(overall.equippedID))
        {
            UnequipPart(BodyPart.Shirt);
            UnequipPart(BodyPart.Pants);
        }
    }

    private string GetCurrentStatus()
    {
        if (animator == null) return fallbackStatus;

        AnimatorClipInfo[] info = animator.GetCurrentAnimatorClipInfo(0);
        if (info.Length == 0 || info[0].clip == null) return fallbackStatus;

        string clipName = info[0].clip.name.ToUpperInvariant();
        for (int i = 0; i < knownStatuses.Length; i++)
        {
            if (!string.IsNullOrEmpty(knownStatuses[i]) && clipName.Contains(knownStatuses[i]))
            {
                return knownStatuses[i];
            }
        }
        return fallbackStatus;
    }

    private int GetCurrentFrameIndex()
    {
        if (animator == null || frameCount <= 0) return 0;

        float normalized = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
        if (normalized < 0f) normalized += 1f; // guard negative
        int idx = Mathf.FloorToInt(normalized * frameCount);
        return Mathf.Clamp(idx, 0, frameCount - 1);
    }

    private static bool StatusMatches(string soStatus, string current)
    {
        return !string.IsNullOrEmpty(soStatus)
            && soStatus.Equals(current, System.StringComparison.OrdinalIgnoreCase);
    }
}
