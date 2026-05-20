using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "BaffItemDatabase", menuName = "Game/BaffItemDatabase")]
public class BaffItemDatabase : ScriptableObject
{
    public List<BaffItemData> allBaffItems;
}



