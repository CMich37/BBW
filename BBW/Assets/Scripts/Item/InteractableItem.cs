using UnityEngine;

public class InteractableItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName;
    public Sprite icon;
    
    [Header("Item Data")]
    public ItemData itemData;
}
