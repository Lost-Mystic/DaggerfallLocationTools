using System.Collections;
using UnityEditor;
using UnityEngine;
using DaggerfallWorkshop.DungeonGenerator;

[CustomEditor(typeof(DungeonGenerator))]
// ^ This is the script we are making a custom editor for.
public class YourScriptEditor : Editor
{

    DungeonGenerator dg;

    private void Awake()
    {
         dg = FindObjectOfType<DungeonGenerator>();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Generate Random Dungeon"))
        {
            dg.CreateNewDungeon();
        }
            
    }


 }
