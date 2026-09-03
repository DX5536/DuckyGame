using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the player's clothing SpriteRenderers so ONE set of body animations (IDLE, WALK,
/// JUMP, CROUCH...) works with ANY outfit - no need to author a clip per outfit combination.
///
/// Data model: ONE slot per (body part + animation status). So a part like Hair has a
/// "Hair / IDLE" slot AND a "Hair / WALK" slot, each holding only that status's variants.
/// Every LateUpdate the script applies only the slot whose status matches the current clip,
/// so slots for other statuses never touch (and never hide) the renderer - no fighting.
///
/// Equipping just changes which variant id a part reads - zero clip rebuilding.
/// </summary>
public class Player_AnimationOutfitSpriteSwap : MonoBehaviour
{
    public enum BodyPart { Glasses, Bag, Hair, Camera, Overall, Shirt, Pants, Shoes }

    [System.Serializable]
    public class BodyPartSlot
    {
        [Tooltip("Which body part this slot represents.")]
        public BodyPart part;

        [Tooltip("Which animation status this slot serves - IDLE, WALK, JUMP, CROUCH. " +
                 "Must match the clip name (the clip name must CONTAIN this word) and the SO's status.")]
        public string status = "IDLE";

        [Tooltip("The item_* SpriteRenderer child for this part.")]
        public SpriteRenderer renderer;

        [Tooltip("ONLY this part's variants for THIS status (e.g. IDLE_HairA, IDLE_HairAA).")]
        public List<PlayerOutfit_ScriptableObject> variants = new List<PlayerOutfit_ScriptableObject>();

        [Tooltip("Starting equipped variant id (e.g. \"HairA\"). Empty = this part starts hidden. " +
                 "You only need to set it on ONE slot per part - it is copied to the part's other " +
                 "status slots at startup.")]
        public string equippedID = "";
    }

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Frames")]
    [Tooltip("How many frames each animation state has (matches your clip's keyframe count).")]
    [SerializeField] private int frameCount = 4;

    [Header("Animation Statuses to detect (must match slot/SO status names, ALL CAPS)")]
    [SerializeField] private string[] knownStatuses = { "IDLE", "WALK", "JUMP", "CROUCH" };
    [SerializeField] private string fallbackStatus = "IDLE";

    [Header("Body Part Slots (one per part + status)")]
    [SerializeField] private List<BodyPartSlot> slots = new List<BodyPartSlot>();

    [Header("Debug")]
    [Tooltip("Logs why a body part is hidden. Turn on to diagnose 'character is naked' problems.")]
    [SerializeField] private bool verboseLogging = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Start()
    {
        SyncStartingEquippedIDsPerPart(); // set on one slot -> copied to that part's other slots
        EnforceOverallExclusivity();

        if (verboseLogging)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                BodyPartSlot s = slots[i];
                if (s == null) continue;
                string info = string.IsNullOrEmpty(s.equippedID)
                    ? "EMPTY equippedID -> starts HIDDEN"
                    : $"equippedID='{s.equippedID}'";
                Debug.Log($"[{name}] Slot {s.part}/{s.status}: {info}", this);
            }
        }
    }

    private void LateUpdate()
    {
        // Runs after the Animator so our sprite writes win.
        string status = GetCurrentStatus();
        int frame = GetCurrentFrameIndex();

        for (int i = 0; i < slots.Count; i++)
        {
            BodyPartSlot slot = slots[i];
            if (slot == null) continue;

            // Only the slot matching the CURRENT status touches its renderer. Slots for other
            // statuses are skipped entirely (they never disable it) - this is what removes the
            // fighting that happened when two components both drove the same renderer.
            if (!StatusMatches(slot.status, status)) continue;

            ApplySlot(slot, frame);
        }
    }

    // ---------- Public API (wire to buttons / sliders / Yarn) ----------

    /// <summary>
    /// Equip an outfit variant by its ID (e.g. "HairA"). Applies to every slot (all statuses) that
    /// owns a variant with that ID, so IDLE and WALK stay in sync automatically. Handles the
    /// Overall vs Shirt/Pants mutual exclusivity.
    /// </summary>
    public void EquipOutfit(string outfitID)
    {
        if (string.IsNullOrEmpty(outfitID)) return;

        bool found = false;
        BodyPart part = default;

        for (int i = 0; i < slots.Count; i++)
        {
            BodyPartSlot slot = slots[i];
            if (slot == null) continue;
            if (SlotOwnsVariant(slot, outfitID))
            {
                slot.equippedID = outfitID;
                part = slot.part;
                found = true;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"[{name}] EquipOutfit: no slot has a variant with ID '{outfitID}'.", this);
            return;
        }

        ApplyExclusivityFor(part);
    }

    /// <summary>Removes whatever is worn on the given body part (all its status slots).</summary>
    public void UnequipPart(BodyPart part)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].part == part) slots[i].equippedID = "";
        }
    }

    /// <summary>String overload so a UnityEvent/button can unequip by part name (e.g. "Overall").</summary>
    public void UnequipPart(string partName)
    {
        if (System.Enum.TryParse(partName, true, out BodyPart part)) UnequipPart(part);
    }

    // ---------- Internals ----------

    private void ApplySlot(BodyPartSlot slot, int frame)
    {
        if (slot.renderer == null) return;

        if (string.IsNullOrEmpty(slot.equippedID))
        {
            if (slot.renderer.enabled) slot.renderer.enabled = false;
            return;
        }

        PlayerOutfit_ScriptableObject outfit = FindVariant(slot, slot.equippedID);
        if (outfit == null)
        {
            if (verboseLogging)
                Debug.LogWarning($"[{name}] {slot.part}/{slot.status}: equipped '{slot.equippedID}' but no SO with that PlayerOutfitID is in this slot's Variants. Hidden.", this);
            if (slot.renderer.enabled) slot.renderer.enabled = false;
            return;
        }

        Sprite[] frames = outfit.PlayerOutfitSprite;
        if (frames == null || frames.Length == 0)
        {
            if (verboseLogging)
                Debug.LogWarning($"[{name}] {slot.part}/{slot.status}: SO '{outfit.name}' has an EMPTY sprite array. Hidden.", this);
            if (slot.renderer.enabled) slot.renderer.enabled = false;
            return;
        }

        int idx = Mathf.Clamp(frame, 0, frames.Length - 1);
        if (!slot.renderer.enabled) slot.renderer.enabled = true;
        slot.renderer.sprite = frames[idx];
    }

    private static PlayerOutfit_ScriptableObject FindVariant(BodyPartSlot slot, string outfitID)
    {
        for (int i = 0; i < slot.variants.Count; i++)
        {
            PlayerOutfit_ScriptableObject o = slot.variants[i];
            if (o != null && o.PlayerOutfitID == outfitID) return o;
        }
        return null;
    }

    private static bool SlotOwnsVariant(BodyPartSlot slot, string outfitID)
    {
        return FindVariant(slot, outfitID) != null;
    }

    // If you set the starting equippedID on just one of a part's status slots, copy it to the rest.
    private void SyncStartingEquippedIDsPerPart()
    {
        // For each part, find the first non-empty equippedID and apply it to every slot of that part.
        HashSet<BodyPart> done = new HashSet<BodyPart>();
        for (int i = 0; i < slots.Count; i++)
        {
            BodyPartSlot slot = slots[i];
            if (slot == null || done.Contains(slot.part)) continue;

            string chosen = "";
            for (int j = 0; j < slots.Count; j++)
            {
                if (slots[j] != null && slots[j].part == slot.part && !string.IsNullOrEmpty(slots[j].equippedID))
                {
                    chosen = slots[j].equippedID;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(chosen))
            {
                for (int j = 0; j < slots.Count; j++)
                {
                    if (slots[j] != null && slots[j].part == slot.part) slots[j].equippedID = chosen;
                }
            }
            done.Add(slot.part);
        }
    }

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

    private void EnforceOverallExclusivity()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].part == BodyPart.Overall && !string.IsNullOrEmpty(slots[i].equippedID))
            {
                UnequipPart(BodyPart.Shirt);
                UnequipPart(BodyPart.Pants);
                return;
            }
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
        if (normalized < 0f) normalized += 1f;
        int idx = Mathf.FloorToInt(normalized * frameCount);
        return Mathf.Clamp(idx, 0, frameCount - 1);
    }

    private static bool StatusMatches(string a, string b)
    {
        return !string.IsNullOrEmpty(a) && a.Equals(b, System.StringComparison.OrdinalIgnoreCase);
    }
}
