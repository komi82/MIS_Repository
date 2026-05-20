using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ArtifactDatabase", menuName = "Game/ArtifactDatabase")]
public class ArtifactDatabase : ScriptableObject
{
    public List<ArtifactData> allArtifacts;
}



