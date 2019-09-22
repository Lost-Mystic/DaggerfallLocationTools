using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using DaggerfallWorkshop.DungeonGenerator;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;

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

        // Location contains a dungeon
        // Has a block index in the static doors list - Block index in BLOCKS.BSA
        // Record index is for a specific interior (like shop or home)
        // each location(site) is marked on the world map.  Each site can have a number of exterior blocks, and
        // a dungeon attached to it.
        // Everything for transport is in player enter exit
        // DFLocation.RegionMapTable
        //locationdungeon
        // Has array of dungeonblock
        // !! Must overwrite a normal dungeon block with a more base form.
        // That must simulate any features a normal dungeon would have for compatibility.
        // LocationIndex?

        // ?? Where is the index of locations you can intercept for mods? Based on GPS?
        // Can you intercept a transition request event?
        // Requests to Maps.bsa
        // DFValidator:152  // Supports alternate MAPS.BSA from Resources if available


        // MODIFY THE LOCATIONS INDEX
        // Have our own data file that has overwrites for regions already.
        // Each data point is driven by a mod.  Use the mod's loading order and existing locations to ensure no overwrites
        // Have the "final" modified version of the additional map data parsed.
        // 1.) Wait until the maps file is loaded.
            // Use the start mod events unless better are found.
        // 2.) Then edit the maps file index in memory
        // 3.) Then reload the existing area if needed 
        // ?? Content Reader Get Location is used, how to overwrite that?  Does it mandate use of the data file?


        // Modify mapsfile.cs  region record add the new region index in or change it

        if (GUILayout.Button("Teleport Player to test dungeon area", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            // Test if game is running
            if (IsGameRunning() == false) return;

            Transform doorOwner = null;
            StaticDoor door = new StaticDoor();
            DFLocation location = new DFLocation();

            // Assign an override location

            // Grab player and use their PlayerEnterExit
            // TransitionDungeonInterior
            GameManager.Instance.PlayerEnterExit.TransitionDungeonInterior(doorOwner, door, location);

        }



        if (GUILayout.Button("Do other thing", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            EditorUtility.SetDirty(soData);

        }

        PrintLocationInformation();

        EditorGUILayout.EndHorizontal();

    }

    #endregion

    bool IsPrefab(GameObject gameObject)
    {
        if (gameObject == null) return false;

        if (PrefabUtility.GetPrefabType(gameObject) == PrefabType.Prefab) return true;

        return false;
    }

    bool IsGameRunning()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");

        if (go == null) return false;

        DaggerfallWorkshop.PlayerGPS gPS = go.GetComponent<DaggerfallWorkshop.PlayerGPS>();


        if (gPS == null) return false;

        return true;
    }

    GameObject DungeonPrefab;
    string LocationName;
    int RegionSelected = 0;

    void PrintLocationInformation()
    {
        int[] RegionValues = {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
            15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
            30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44
        };

        GUILayout.BeginVertical();

        DungeonPrefab = EditorGUILayout.ObjectField("", DungeonPrefab, typeof(GameObject)) as GameObject;

        if (IsPrefab(DungeonPrefab) == false) DungeonPrefab = null;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Location Name:");
        LocationName = GUILayout.TextField(LocationName, GUILayout.MinWidth(80));
        GUILayout.EndHorizontal();

        

        RegionSelected = EditorGUILayout.IntPopup("Region",RegionSelected, MapsFile.RegionNames, RegionValues,GUILayout.MinWidth(80));

        // Coordinates on map (with popup map)
        GUILayout.Label("Map Coords:");

        // Check if dungeon is registered.   This will either register the dungeon, or change something in it.

        if (GUILayout.Button("Register Dungeon"))
        {

        }

    }

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


