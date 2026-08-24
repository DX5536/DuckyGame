using UnityEngine;

[CreateAssetMenu(fileName = "PlayerOutfit", menuName = "ScriptableObject/PlayerOutfit", order = 1)]
public class PlayerOutfit_ScriptableObject : ScriptableObject
{
    [Header("Outfit Name - For shop display etc")]
    [SerializeField] private string playerOutfitName;
    public string PlayerOutfitName
    {
        get { return playerOutfitName; }
    }

    [Header("Outfit Description - For shop display etc")]
    [SerializeField] private string playerOutfitDescription;
    public string PlayerOutfitDescription
    {
        get { return playerOutfitDescription; }
    }

    [Header("Outfit ID - For internal coding purposes, eg. BagA, HairA")]
    [SerializeField] private string playerOutfitID;
    public string PlayerOutfitID
    {
        get { return playerOutfitID; }
    }

    [Header("Animation Status (All Caps) - WALK, IDLE, JUMP, CROUCH")]
    [SerializeField] private string playerAnimationStatusName;
    public string PlayerAnimationStatusName
    {
        get { return playerAnimationStatusName; }
    }

    [Header("Player Outfit Sprites - one per animation frame (e.g. 4)")]
    [Tooltip("The actual Sprite assets for this body part in this animation state, in frame order.")]
    [SerializeField]
	private Sprite[] playerOutfitSprite;

	public Sprite[] PlayerOutfitSprite
	{
		get { return playerOutfitSprite; }
	}
}
