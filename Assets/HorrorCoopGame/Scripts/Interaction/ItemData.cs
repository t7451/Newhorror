using UnityEngine;

namespace HorrorCoopGame.Interaction
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Survival Horror/Item Data")]
    public sealed class ItemData : ScriptableObject
    {
        public string itemName;
        public Sprite icon;
        public int maxStack = 10;
        public bool isKeyEngineComponent;
    }
}
