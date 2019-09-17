using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DaggerfallWorkshop.DungeonGenerator;

public class EditorDfLocationsWindow : EditorWindow
{
    [MenuItem("Daggerfall Tools/Location Overrides")]
    static void ShowWindow()
    {
        GetWindow<EditorDfLocationsWindow>("Location Overrides");
    }

    #region Variables


    Editor gameObjectEditor;
    PreviewRenderUtility previewRenderUtility;  // Hold our own previewrender util (used for custom preview windows)
    SoModelRecords soData;                  // Holds link to the Scriptable object with the Model data




    GUIStyle gsLabelSelected;
    GUIStyleState gssSelected;
    bool bShowLabelListArea = false;

    // Scroll position for the scroll bar
    Vector2 FilteredModelListScrollPosition;
    private Vector3 PrevCameraPos = Vector3.forward * -5;

    /// <summary>
    /// The text field current contents users can type the Label entry into
    /// </summary>
    string txtLabelEntry = "";
    /// <summary>
    /// List of the currently selected LABEL NAMES.  eg. "Dungeon"
    /// </summary>
    List<string> SelectedLabel = new List<string>();

    GameObject gameObjectSelected;              // Selected game object
    Color color_selected = Color.cyan;          // Background Color of selected items
    /// <summary>
    /// List of selected index numbers from the filtered list
    /// </summary>
    List<int> selectedIndex = new List<int>();

    /// <summary>
    /// Holds the rect information for the filtered list scrollable window
    /// </summary>
    Rect rFilteredItemsScrollWindow;

    /// <summary>
    /// If true, only displays things with NO labels.
    /// </summary>
    bool bHideLabeled = false;
    bool bNeedsRedraw = false;

    #endregion


    #region PrintWindowElements


    /// <summary>
    /// Prints debug buttons for editor at bottom.
    /// </summary>
    void PrintDebugEditorButtons()
    {

        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));

        float buttonWidth = 400;
        float buttonHeight = 30;

        // Map Tile Location, or Interior or Dungeon  Tabs

        // Two options: Select then Replace/Insert
            // This zooms map to locations or blocks and allows user to replace or add entry to a location


        // Dungeon Layout
            // Dungeon Block
                // Door
                // Model / Flat / Event

        // Option 2: Import Location - Bring up the area first, then pick where to insert it
        // If Dungeon
        // Replace existing location or add new location
        // Add new location loads a dungeon layout, must have exit(s) the lead to location(s), but with 
        // If add new location needs an above ground entrance with a map tile or keyed door
            // Verifies all features that need to be in area.  Modifies prefab

        // Mod adds interruption in between specific location doors or when streaming a tile

        // Dungeon
        // Data for dungeon
        // Dropdown on how to access it
        // Override existing place (with mod)
        // key a specific door
        // Scripted command (Dialog, falling down a pit, or if you have another area that will reference it later)
            // Key to a generated location
        // backup area to send person to if it doesn't work.

        //DFLocation
        // DaggerfallLocation
        // RUNTIME
        // May be in DaggerFall unity during runtime
        // Playerenterexit has a location override
        // Dungeon --> DaggerfallDungeon  monobehaviour
            // chidren have list of dungeon blocks and enemies
            // has location textures
            // When dungeon is gone it is removed from the dungeon branch
        // Outdoor locations have list of doors, which are automaticlaly mapped onto the terrain
        // DaggerfallStaticDoors

        if (GUILayout.Button("Do thing", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            
        }

        if (GUILayout.Button("Do other thing", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            EditorUtility.SetDirty(soData);

        }

        EditorGUILayout.EndHorizontal();

    }

    #endregion


    void Update()
    {
        if (bNeedsRedraw)
        {
            this.Repaint();
            bNeedsRedraw = false;
        }

    }

    void OnGUI()
    {
        SetGuiStyles();

        EditorGUILayout.BeginVertical();

        PrintDebugEditorButtons();

        EditorGUILayout.EndVertical();


    }

    void SetGuiStyles()
    {


        gsLabelSelected = new GUIStyle();
        gssSelected.textColor = Color.white;
        gsLabelSelected.fontStyle = FontStyle.Bold;
        gsLabelSelected.onNormal = gssSelected;
        gsLabelSelected.focused = gssSelected;
        gsLabelSelected.hover = gssSelected;
        gsLabelSelected.normal = gssSelected;



    }

    #region Events

    // Start is called before the first frame update
    void OnEnable()
    {

        if (soData == null)
            soData = Resources.Load("ModelRecords") as SoModelRecords;

        Texture2D txBlue = new Texture2D(1, 1);
        txBlue.SetPixel(0, 0, Color.blue);
        txBlue.Apply();
        gssSelected = new GUIStyleState();
        gssSelected.background = txBlue;

    }



    private void OnFocus()
    {


    }

    private void OnLostFocus()
    {

        // Have to call this or when dragging a Preview out into the scene it causes a number of errors.
        if (previewRenderUtility == null)
            previewRenderUtility = new PreviewRenderUtility();

    }

    private void OnDisable()
    {
        
        // Have to call this or when dragging a Preview out into the scene it causes a number of errors.
        if (previewRenderUtility == null)
            previewRenderUtility = new PreviewRenderUtility();

        previewRenderUtility.Cleanup();
        
    }

    #endregion
}


