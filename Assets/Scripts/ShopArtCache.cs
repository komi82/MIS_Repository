using UnityEngine;
using UnityEngine.U2D;

// Explicit references allow Unity's asynchronous loader to prepare shop art during arcade play.
public sealed class ShopArtCache : ScriptableObject
{
    public SpriteAtlas[] atlases;
    public RuntimeAnimatorController[] animations;
}
