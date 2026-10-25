using UnityEngine;

[CreateAssetMenu(menuName = "Game/Koua Artifact Catalog")]
public sealed class KouaArtifactCatalog : ScriptableObject
{
    public ArtifactData[] artifacts;
    public RecipeDatabase[] recipes;
    public ItemData[] ores;
    public ItemData[] plants;
    public ItemData[] mushrooms;
}
