using UnityEngine;

namespace HorrorCoopGame.Building
{
    [CreateAssetMenu(fileName = "NewBuildableData", menuName = "Survival Horror/Buildable Data")]
    public sealed class BuildableData : ScriptableObject
    {
        public string buildableName;
        public GameObject prefab;
        public GameObject ghostPrefab;
        public string requiredItemName = "ScrapMetal";
        public int requiredItemAmount = 4;
        public float gridSize = 1f;
        [Range(0.05f, 5f)] public float yawSnap = 90f;
    }
}
