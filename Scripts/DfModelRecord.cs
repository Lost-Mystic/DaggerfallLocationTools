using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DfModelRecord
{

    public int ModelID;

    public GameObject TestModel;

    [Tooltip ("If true, this is not found in the original daggerfall files, and must be included.")]
    public bool ExtendedModel = true;

    [Tooltip("Not used for Daggerfall Models, but used as unique prefab name ID for extended models.")]
    public string ModelName;

    [Tooltip("What categories this belongs to.  For example, Dungeon, Corridor, DaggerfallStyleB.  All labels are stored in lower case.  ")]
    public List<string> Labels;

    [Tooltip("TEMPORARY - TRUE if this can be placed in a dungeon.")]
    public bool DungeonPiece = false;

    //public BareDaggerfallMeshStats BareMesh;

    // List of Connections
    public DfModelConnectionRecord[] Connections;

    // List of suggested flats
    public DfFlatRecord[] SuggestedFlats;
    // List of suggested enemy/npc types
    public DfEnemyRecord[] SuggestedEnemies;

    // Serialize this data ??

    public void AddLabel(string label)
    {
        label = label.ToLower();

        foreach (string s in Labels)
        {
            if (label.Equals(s))
                return;
        }
        Labels.Add(label);
    }

    public void RemoveLabel(string label)
    {
        label = label.ToLower();

        Labels.Remove(label);
        Labels.TrimExcess();
    }


}


[System.Serializable]
public class DfModelConnectionRecord
{
    // Connection Facing NSWE Up Down
    // (0,0,0) = North  - Should align with default unity object rotation
    // (0,90,0) = East
    // (90,0,0) = North
    
    // Connection Type - What style of hall or door is this.  Match up with others of it's type.
    public Vector3 facing;

    // Connection Local Coords - The snap point for the lower left hand corner 
    public Vector3 loc;

    // Style determines width, height and dimensions of connections.  They will be matched together.
    public string style;
}

[System.Serializable]
public class DfFlatRecord
{
    // By default no specific flat.  Otherwise use daggerfall flat ID
    public int SpecificFlatID = -1;

    // Suggested type of flat
    public string SuggestedType;

    // How strong is the suggestion. 0-5
    // 0 = just a suggestion
    // 3 = Suggestion, but would really work well here.
    // 5 = Absolute must
    public int SuggestedRank = 0;

    // Local Coordinate Location of the flat
    public Vector3 loc;
}


[System.Serializable]
public class DfEnemyRecord
{
    // By default no specific enemy.  Otherwise use daggerfall enemy ID
    public int SpecificEnemyID = -1;

    // Suggested type of enemy
    public string SuggestedType;

    // How strong is the suggestion. 0-5
    // 0 = just a suggestion
    // 3 = Suggestion, but would really work well here.
    // 5 = Absolute must
    public int SuggestedRank = 0;

    // Local Coordinate Location of the enemy
    public Vector3 loc;

}

[System.Serializable]
public class BareDaggerfallMeshStats
{
    public Mesh mesh;
    public Matrix4x4 matrix;
    public Material[] mat;
    public int SubMeshIndex;
    public MaterialPropertyBlock mProp;
}


