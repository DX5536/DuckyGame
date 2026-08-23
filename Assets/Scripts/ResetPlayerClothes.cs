using UnityEngine;
using DG.Tweening;

public class ResetPlayerClothes : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] playerItems;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Super quick and dirty way to remove all items
    public void DisableClothes()
    {
        foreach (SpriteRenderer item in playerItems)
        {
            if (item != null)
            {
                item.DOKill();
                item.DOFade(0f, 0f); // fade out over 0.5 seconds
            }
        }
    }

    public void EnableClothes()
    {
        foreach (SpriteRenderer item in playerItems)
        {
            if (item != null)
            {
                item.DOKill();
                item.DOFade(1f, 0f); // fade out over 0.5 seconds
            }
        }
    }
}
