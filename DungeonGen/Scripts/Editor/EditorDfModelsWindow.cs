using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DaggerfallWorkshop.DungeonGenerator;

public class EditorDfModelsWindow : EditorWindow
{
    [MenuItem("Daggerfall Tools/Daggerfall Models")]
    static void ShowWindow()
    {
        GetWindow<EditorDfModelsWindow>("Daggerfall Models");
    }

    Editor gameObjectEditor;
    PreviewRenderUtility previewRenderUtility;  // Hold our own previewrender util (used for custom preview windows)
    SoModelRecords soData;                  // Holds link to the Scriptable object with the Model data
    int RecordDisplayMaxCount = 9;          // Maximum amount of models that can be displayed at one time in window. (Scroll to see more)
    BareDaggerfallMeshStats myBareMesh;
    List<DfModelRecord> FilteredModels;

    float dimTopLabelsHeight = 20f;
    float dimFilteredListWidth = 80f;
    float dimPreviewFilteredHeight = 200f;
    float dimPreviewFilteredHeightPercent = 0.5f;       // What height percentage of the window does it take up
    float dimLabelEntryHeight = 20f;                    // Height of the label entry section
    float dimLabelListingHeight = 60f;                  // Suggested height of the label listings (they'll wrap and take as much as required

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
    Rect rScrollWindow;

    // Scroll position for the scroll bar
    Vector2 ScrollPosition;
    private Vector3 PrevCameraPos = Vector3.forward * -5;



    /// <summary>
    /// Prints the filtered text list of model numbers to the editor window
    /// </summary>
    void PrintFilteredModelListToWindow()
    {
        
        GUILayout.BeginVertical(GUILayout.Width(dimFilteredListWidth), GUILayout.Height(dimPreviewFilteredHeight));

        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);

        // If there are no records listed, and no labels selected, the list should show default.
        if (FilteredModels.Count == 0 && SelectedLabel.Count <= 0)
        {
            FilteredModels = soData.record;
            UpdateFilteredList();
        }
        
        if (FilteredModels.Count == 0)
            GUILayout.Label("No items.",GUILayout.Height(dimPreviewFilteredHeight));


        //SerializedObject serializedRecords = new SerializedObject(soData);

        Color color_default = GUI.backgroundColor;
        GUIStyle gsSelected = EditorStyles.label;
        gsSelected.fontStyle = FontStyle.Bold;
        GUIStyle gsCurrent = GUIStyle.none;
        float EntryWidth = 60f;
        float EntryHeight = 20f;

        // Determine Window Size.
        if (rScrollWindow == null)
            rScrollWindow = new Rect(0, 0, dimFilteredListWidth, dimPreviewFilteredHeight);

        // Scroll position goes by pixels
        //Debug.Log("Scroll Pos: " + ScrollPosition.ToString());
        //Debug.Log("rScrollBox: " + rScrollWindow.ToString());

        // Find number of entries that can fit in window, round up
        int iMaxEntries = Mathf.CeilToInt(rScrollWindow.height / (float)EntryHeight);
        int EntriesAfter = 0;
        int EntriesBefore = 0;

        EntriesBefore = Mathf.FloorToInt(ScrollPosition.y / EntryHeight);

        //EntriesBefore = Mathf.RoundToInt(FilteredModels.Count * ScrollPosition.y) - (iMaxEntries / 2);

        // If filtered list is less than can fit on the screen at one time
        if (FilteredModels.Count < iMaxEntries)
        {
            EntriesBefore = 0;
            EntriesBefore = 0;
            iMaxEntries = FilteredModels.Count;
        }
        else
        if (EntriesBefore <= 0)  // If at bottom of the section
        {
            EntriesBefore = 0;
            EntriesAfter = FilteredModels.Count - iMaxEntries;
        }
        else
        if ((EntriesBefore + iMaxEntries) >= FilteredModels.Count)   // Top section of chart
        {
            EntriesAfter = 0;
            EntriesBefore = FilteredModels.Count - iMaxEntries;
        }
        else // Somewhere in the middle
        {
            EntriesBefore = Mathf.FloorToInt(ScrollPosition.y / EntryHeight);
            EntriesAfter = FilteredModels.Count - EntriesBefore - iMaxEntries;
        }
        

        // Put blank space before and after scroll view using unit size times units remaining
        // This is updated every frame so if list changes, it will be updated with the frame

        

        // This gets the array of records, and displays those already known.



        GUILayout.Space(EntriesBefore * EntryHeight);

        for (int i = EntriesBefore; i < (iMaxEntries + EntriesBefore); i++)
        {
            //GUI.backgroundColor = (selectedIndex == i) ? color_selected : color_default;
            if (IsSelected(i))
                gsCurrent = gsSelected;
            else
                gsCurrent = GUIStyle.none;


            GUIContent entry = new GUIContent(FilteredModels[i].ModelID.ToString());


            if (GUILayout.Button(entry, gsCurrent, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false), GUILayout.Height(EntryHeight), GUILayout.Width(EntryWidth)))
            {
                InputSelectNewObjects(i, FilteredModels[i].ModelID);
            }

        }
        GUILayout.Space(EntriesAfter * EntryHeight);

        

        GUILayout.EndScrollView();

        
        Rect rTest = GUILayoutUtility.GetLastRect();
        if (rTest.x != 0.0f)
            rScrollWindow = rTest;

        GUILayout.EndVertical();
    }



    /// <summary>
    /// Returns true if the index number from the main database is currently selected.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    bool IsSelected(int index)
    {
        if (selectedIndex.Count <= 0) return false;

        if (selectedIndex.Contains(index)) return true;

        return false;
    }


    /// <summary>
    /// Watches keyboard and mouse events to multi select or de select options
    /// </summary>
    /// <param name="NowClickedIndex"></param>
    void InputSelectNewObjects(int NowClickedIndex, int Model_ID)
    {
        bool DeSelected = false;

        

        if (Event.current.control)
        {
            if (IsSelected(NowClickedIndex))
            {
                selectedIndex.Remove(NowClickedIndex);
                DeSelected = true;
            }
            else
            {
                selectedIndex.Add(NowClickedIndex);
            }
        } else

        if (Event.current.shift)
        {
            // If selecting the same thing, ignore
            if (IsSelected(NowClickedIndex))
            {
                return;
            } else

            // If selecting something above last clicked, add any in between to selected index
            if (NowClickedIndex > soData.LastClickedIndex)
            {
                for (int i = soData.LastClickedIndex; i <= NowClickedIndex; i++)
                {
                    selectedIndex.Add(i);
                }
            } else
            // Selected one before last clicked, 
            if (NowClickedIndex < soData.LastClickedIndex)
            {
                for (int i = soData.LastClickedIndex; i >= NowClickedIndex; i--)
                {
                    selectedIndex.Add(i);
                }
            }


        }
        // Otherwise selecting one item
        else
        {
            selectedIndex = new List<int>();
            if (!IsSelected(NowClickedIndex))
            {
                
                selectedIndex.Add(NowClickedIndex);
            } else
            {
                DeSelected = true;
            }
        }

        soData.LastClickedIndex = NowClickedIndex;

        CheckMeshLoaded(NowClickedIndex);
        // Cached Filtered Objects Update

        PreviewRenderInit();

        //if (DeSelected == true) return;

        

        if (FilteredModels.Count <= 0) return;

    }

    /// <summary>
    /// Load the proper mesh of the last clicked selected item.
    /// </summary>
    /// <param name="NowClickedIndex"></param>
    void CheckMeshLoaded(int NowClickedIndex)
    {
        if (soData.record.Count <= 0) return;
        if (FilteredModels.Count <= 0) return;
        if (selectedIndex.Count <= 0) return;

        //Debug.Log("Try to Check Mesh" + FilteredModels.Count.ToString() + " " + NowClickedIndex.ToString());

        if (NowClickedIndex == -1)
            NowClickedIndex = soData.LastClickedIndex;

        Debug.Log("Try to Check Mesh" + FilteredModels[NowClickedIndex].ModelID.ToString());

        bool LikelyLostFocus = (selectedIndex.Count == 1 && soData.LastClickedIndex == 0);
        bool ForceUpdate = (NowClickedIndex == -1);
        bool ClickedOnSameItem = (NowClickedIndex == soData.LastClickedIndex);

      //  if ((!LikelyLostFocus && !ClickedOnSameItem) || ForceUpdate)
        {


            Debug.Log("Loading Display Mesh ID: " + FilteredModels[NowClickedIndex].ModelID.ToString());
            myBareMesh = SpawnDfModel.GetBareMeshFromId(FilteredModels[NowClickedIndex].ModelID);
            
        }

        soData.LastClickedIndex = NowClickedIndex;
    }


    /// <summary>
    /// Prints the one or more currently selected models to the preview window
    /// </summary>
    void PrintSelectedModelsToPreview()
    {


        GUILayout.BeginVertical(GUILayout.ExpandHeight(true), GUILayout.Height(dimPreviewFilteredHeight));



        // Print dummy window if nothing
        if ((FilteredModels == null) || (FilteredModels.Count <= 0) || (FilteredModels[soData.LastClickedIndex] == null))
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("No models selected or camera crash.", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(dimPreviewFilteredHeight));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            return;
        }

        

        if (myBareMesh == null)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("No mesh to display", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(dimPreviewFilteredHeight));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            return;
        }

        DrawRenderPreview(myBareMesh, GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth-dimFilteredListWidth, dimPreviewFilteredHeight), GUIStyle.none);
        

        GUILayout.EndVertical();
    }

    
    public void DrawRenderPreview(BareDaggerfallMeshStats bareMesh, Rect r, GUIStyle gstyle)
    {
        if (previewRenderUtility == null)
            previewRenderUtility = new PreviewRenderUtility();

        if (bareMesh == null)
        {
            Debug.Log("No Mesh Found.");
            return;
        }

        if (previewRenderUtility.camera == null)
        {
            //Debug.Log("No preview render camera found.");
            return;
        }

        previewRenderUtility.BeginPreview(r, gstyle);

        InputPreviewWindowControls(r);

        // FIXME Doesn't render the right material to the mesh.  Need some way of knowing which submesh to render the material to.

        

        if (bareMesh.mesh != null)
            for (int i = 0; i < bareMesh.SubMeshIndex; i++)
            {

                
                previewRenderUtility.DrawMesh(bareMesh.mesh, Vector3.zero, Quaternion.Euler(-30f, 0f, 0f) * Quaternion.Euler(0f, 60, 0f), bareMesh.mat[i], i, bareMesh.mProp);

            }

        bool fog = false;
        //Unsupported.SetRenderSettingsUseFogNoDirty(false);
        previewRenderUtility.camera.Render();
        //Unsupported.SetRenderSettingsUseFogNoDirty(fog);
        Texture texture = previewRenderUtility.EndPreview();

        GUI.DrawTexture(r, texture);
    }

    

    /// <summary>
    /// Keyboard or mouse controls for viewing and panning in the preview window
    /// </summary>
    private void InputPreviewWindowControls(Rect rHotArea)
    {
        // Only use controls if inside this area
        if (rHotArea.Contains(Event.current.mousePosition) == false)
            return;

        float ScrollMod = 0.8f;
        float PanMod = 0.8f;

        var drag = Vector2.zero;
        Vector2 ScrollDelta = Vector2.zero;
        
        if (Event.current.type == EventType.MouseDrag)
        {
            drag = Event.current.delta;
        }

        if (Event.current.type == EventType.ScrollWheel)
        {
            ScrollDelta = Event.current.delta;
        } else
        {
            ScrollDelta = Vector2.zero;
        }


        PrevCameraPos.z += ScrollDelta.y * ScrollMod;

        //previewRenderUtility.camera.transform.position = PrevCameraPos;



        if (Event.current.type == EventType.MouseDrag && Event.current.button == 2)
        {
            previewRenderUtility.camera.transform.Translate(previewRenderUtility.camera.transform.right * -drag.x * PanMod * 0.3f);
            previewRenderUtility.camera.transform.Translate(previewRenderUtility.camera.transform.up * - drag.y * PanMod * 0.3f);
        } else
        {
            previewRenderUtility.camera.transform.RotateAround(Vector3.forward * 0, Vector3.up, -drag.x * PanMod);
            previewRenderUtility.camera.transform.RotateAround(Vector3.forward * 0, Vector3.right, -drag.y * PanMod);
            //previewRenderUtility.camera.transform.Rotate(0, -drag.x * PanMod,0);
            //previewRenderUtility.camera.transform.Rotate(-drag.y * PanMod,0,0);
        }

       // if(Event.current.type == EventType.MouseDrag && Event.current.button == 1)
        {
            previewRenderUtility.camera.transform.LookAt(Vector3.zero, Vector3.up);
        }


        previewRenderUtility.camera.transform.Translate(previewRenderUtility.camera.transform.forward * -1 * ScrollDelta.y * ScrollMod);


        


        if (drag != Vector2.zero || ScrollDelta != Vector2.zero)
            Repaint();
    

    }


    /// <summary>
    /// Returns a list of selected records
    /// </summary>
    /// <returns></returns>
    private List<DfModelRecord> GetSelectedModelRecords()
    {
        List<DfModelRecord> df = new List<DfModelRecord>();

        for (int i = 0; i < selectedIndex.Count; i++)
        {
            df.Add(FilteredModels[selectedIndex[i]]);
        }
        return df;
    }

    /// <summary>
    /// Prints the label filter or add text box and button
    /// </summary>
    void PrintLabelTypeField()
    {
        EditorGUILayout.BeginHorizontal();

        txtLabelEntry = EditorGUILayout.TextField(txtLabelEntry);

        List<DfModelRecord> SortedRecordsByLabel = soData.GetOnlyRecordsByLabels(SelectedLabel.ToArray());

        // Searches for that particular string
        if (GUILayout.Button("Add"))
        {
            // Check if label has been added to any items at all
            soData.TryToAddLabel(txtLabelEntry, GetSelectedModelRecords());
            UpdateFilteredList();
        }

        if (GUILayout.Button("Remove"))
        {
            // Check if label has been added to any items at all
            soData.TryToRemoveLabel(txtLabelEntry, GetSelectedModelRecords());
            UpdateFilteredList();
        }


        EditorGUILayout.EndHorizontal();
    }


    /// <summary>
    /// If true, only displays things with NO labels.
    /// </summary>
    bool bHideLabeled = false;

    /// <summary>
    /// Prints the currently selected labels bar section below the model preview.
    /// </summary>
    void PrintCurrentSelectedLabels()
    {
        
        EditorGUILayout.BeginHorizontal();

        /*
        if (SelectedLabel.Count <= 0)
        {
            GUILayout.Label("---No Labels Selected---");
            
        }
        */

        GUIStyle fds = new GUIStyle();
        fds = EditorStyles.miniButton;
        //fds.margin = new RectOffset(0, 0, 0, 0);
        //fds.padding = new RectOffset(0, 0, 0, 0);
        //GUIStyleState fg = new GUIStyleState();

        

        //GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);

        // Print selected labels first
        for (int i = 0; i < SelectedLabel.Count; i++)
        {
            // Remove selected label when clicked
            if (GUILayout.Button(SelectedLabel[i])) 
            {
                SelectedLabel.Remove(SelectedLabel[i]);
                SelectedLabel.TrimExcess();
                // Redo Filtered List
                UpdateFilteredList();
            }
            

            if (GUILayout.Button("X",fds))
            {
                soData.TryToRemoveLabel(SelectedLabel[i], GetSelectedModelRecords());
                SelectedLabel.Remove(SelectedLabel[i]);
                SelectedLabel.TrimExcess();
                UpdateFilteredList();
            }
            
            GUILayout.Space(15); // Slight margin
            
            
        }

        // Then print all other label buttons on currently selected object minus selected ones.
        if (FilteredModels != null)
            if (FilteredModels.Count > 0)
                for (int i = 0; i < FilteredModels[soData.LastClickedIndex].Labels.Count; i++)
                {
                    if (SelectedLabel.Contains(FilteredModels[soData.LastClickedIndex].Labels[i]))
                        continue;

                    if (GUILayout.Button(FilteredModels[soData.LastClickedIndex].Labels[i], EditorStyles.whiteLabel))
                    {
                        SelectedLabel.Add(FilteredModels[soData.LastClickedIndex].Labels[i]);
                        // Redo Filtered List
                        UpdateFilteredList();
                    }
                }

        if (GUILayout.Button(bHideLabeled?"Show All":"Hide Labeled", bHideLabeled?EditorStyles.toolbarButton:EditorStyles.whiteLabel))
        {
            bHideLabeled = !bHideLabeled;

            if (bHideLabeled)
                SelectedLabel = new List<string>();

            UpdateFilteredList();
            
        }

        

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }


    /// <summary>
    /// Sets the filtered list after it's been changed somehow.
    /// </summary>
    void UpdateFilteredList()
    {

        // If no records then return
        if (soData.record.Count <= 0) return;

        List<int> SelectedID = new List<int>();

        if (selectedIndex == null)
            selectedIndex = new List<int>();

        // Setup the Selected ID list before the old filtered list is wiped away
        if (soData.LastClickedIndex >= 0)
        {
            // First selected ID is last clicked
            SelectedID.Add(FilteredModels[soData.LastClickedIndex].ModelID);
            // Convert all models to IDs for later
            for (int i = 1; i < selectedIndex.Count; i++)
            {
                SelectedID.Add(FilteredModels[selectedIndex[i]].ModelID);
            }
        }

        FilteredModels = new List<DfModelRecord>();

        // If hide labels only
        if (bHideLabeled)
        {
            // Sort out all the selected labels
            for (int i = 0; i < soData.record.Count; i++)
            {
                    if (soData.record[i].Labels.Count == 0)
                    {
                        FilteredModels.Add(soData.record[i]);
                    }

            }
            //soData.LastClickedIndex = 0;
            //CheckMeshLoaded(0);
            //return;
        } else

        // If no labels selected show the entire list
        if (SelectedLabel.Count <= 0)
        {
            FilteredModels = soData.record;
        } else
            // Show only labels selected
            // Sort out all the selected labels
            for (int i = 0; i < soData.record.Count; i++)
            {

                int CountSelectedLabels = 0;

                // Go through each selected label for each record and compare
                foreach (string str in SelectedLabel)
                {

                    if (soData.record[i].Labels.Contains(str))
                    {
                        CountSelectedLabels++;

                        //break;  // Just need one entry, otherwise this can add an entry multiple times

                    }
                }
                // If record contains all selected labels, then add record to filtered list
                if (CountSelectedLabels >= SelectedLabel.Count)
                {
                    FilteredModels.Add(soData.record[i]);

                }
            }



        UpdateSelectedList(SelectedID);
        CheckMeshLoaded(soData.LastClickedIndex);

    }

    void UpdateSelectedList(List<int> SelectedID)
    {
        if (FilteredModels.Count <= 0) return;

        //bool FoundSelectedRecordInNewFilteredList = false;

        // Save the last clicked ID model, and see if it's still selected at the end.
        int LastClickedId;

        if (SelectedID.Count > 0)
            LastClickedId = SelectedID[0];
        else
            LastClickedId = 0;

        soData.LastClickedIndex = -1;

        //if (selectedIndex.Contains(soData.LastClickedIndex) == false)
        //    soData.LastClickedIndex = 0;

        selectedIndex = new List<int>();

        if (FilteredModels.Count <= 0)
        {

            return;
        }
        // Sort list
        SelectedID.Sort();
        int selCount = 0;
        int filCount = 0;

        while (selCount < SelectedID.Count && filCount < FilteredModels.Count)
        {
            if (SelectedID[selCount] == FilteredModels[filCount].ModelID)
            {
                selectedIndex.Add(filCount);
                selCount++;
            }

            if (LastClickedId == FilteredModels[filCount].ModelID)
                soData.LastClickedIndex = filCount;

                filCount++;
        }

        if (selectedIndex.Count > 0)
            soData.LastClickedIndex = selectedIndex[0];
        else
            soData.LastClickedIndex = 0;

        // If LastClickedIndex is still -1, set it to the first entry



    }

    /// <summary>
    /// Prints debug buttons for editor at bottom.
    /// </summary>
    void PrintDebugEditorButtons()
    {

        if (Event.current.control)
        {
            
            GUILayout.Label("CTRL");
        }

        if (Event.current.shift)
        {
            GUILayout.Label("SHIFT");
        }

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));

        float buttonWidth = 400;
        float buttonHeight = 30;

        if (GUILayout.Button("Instantiate Last Selected Item", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            SpawnDfModel.SpawnModel(FilteredModels[soData.LastClickedIndex].ModelID);
        }

        /*
        if (GUILayout.Button("Clear Entire Model Array", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            soData.record = new List<DfModelRecord>();
            soData.AddRecordById(0);
            soData.LastClickedIndex = 0;
            FilteredModels = soData.record;
            selectedIndex = new List<int>();
            selectedIndex.Add(0);
            UpdateFilteredList();
        }

        if (GUILayout.Button("Populate 1000 Items on Model Array", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            int Count = 0;
            int Index = soData.record.Count - 1;

            //soData.AddRecordById(60604);
            //soData.AddRecordById(62325);
            //soData.AddRecordById(74067);


            while (Count < 1000)
            {
                if (soData.AddRecordById(Index))
                {
                    Count++;
                }
                Index++;
                if (Index % 100 == 0)
                {
                    Debug.Log("Finished index: " + Index.ToString());
                }
            }

        }

        if (GUILayout.Button("Populate ALL Model Items", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            soData.AddAllRecords();
            soData.LastClickedIndex = 0;
            FilteredModels = soData.record;
            selectedIndex = new List<int>();
            selectedIndex.Add(0);
            UpdateFilteredList();
        }
        */

        if (GUILayout.Button("Ready Database for Saving", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            EditorUtility.SetDirty(soData);
        }

        if (GUILayout.Button("Print selected items", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            EditorUtility.SetDirty(soData);
            Debug.Log("---===Selected Index===---");
            for (int i = 0; i < selectedIndex.Count; i++) {
                Debug.Log("Index: " + selectedIndex[i].ToString());
                
            }
            Debug.Log("---===END Selected Index===---");
        }
            EditorGUILayout.EndHorizontal();

    }




    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Select Model Number(s) to View"));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(new GUIContent("Models"));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        PrintSelectedModelsToPreview();

        PrintFilteredModelListToWindow();

        EditorGUILayout.EndHorizontal();

        PrintLabelTypeField();

        PrintCurrentSelectedLabels();

        PrintDebugEditorButtons();

        // MultiSelect Objects

        // Show labels on current objects
        // Show current labels selected - Starts with selected label first no matter what
        // Add or find a label window
        // Add label to selected button, bottom of window
        // Remove label from selected objects - Appears when you click on an already selected label

        // Button to add selected Meshs into scene.  Will only add first one selected.  Greys out if multiple selected.
       // Event.current.Use();
    }

    #region Events

    // Start is called before the first frame update
    void OnEnable()
    {

        if (soData == null)
            soData = Resources.Load("ModelRecords") as SoModelRecords;

        if (myBareMesh == null)
            myBareMesh = new BareDaggerfallMeshStats();

        if (FilteredModels == null)
            FilteredModels = soData.record;

        if (soData.LastClickedIndex >= FilteredModels.Count)
            soData.LastClickedIndex = 0;

        PreviewRenderInit();

        UpdateFilteredList();


    }

    void PreviewRenderInit()
    {
        if (previewRenderUtility == null)
            previewRenderUtility = new PreviewRenderUtility();

        float BaseCamDist = 30f;

        previewRenderUtility.camera.transform.position = (Vector3)(-Vector3.forward * BaseCamDist);
        previewRenderUtility.camera.transform.rotation = Quaternion.identity;
        previewRenderUtility.camera.farClipPlane = 1000;

        previewRenderUtility.camera.backgroundColor = Color.cyan;

        previewRenderUtility.lights[0].intensity = 1.0f;
        previewRenderUtility.lights[0].range = 1000f;
        previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(30f, 30f, 0f);
        previewRenderUtility.lights[1].intensity = 1.0f;

        PrevCameraPos = Vector3.forward * BaseCamDist * -1;


       // previewRenderUtility.camera.
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 20);
        if (myBareMesh.mesh != null)
            bounds.Encapsulate(myBareMesh.mesh.bounds);
        PrevCameraPos = Vector3.forward * -1 * bounds.max.magnitude * 3.5f;
        previewRenderUtility.camera.transform.SetPositionAndRotation(PrevCameraPos, previewRenderUtility.camera.transform.rotation);
        
        
    }

    private void OnFocus()
    {
        //OnEnable();
        CheckMeshLoaded(soData.LastClickedIndex);
    }

    private void OnLostFocus()
    {

        Debug.Log("Lostfocus LastClicked: " + soData.LastClickedIndex.ToString());

        // Have to call this or when dragging a Preview out into the scene it causes a number of errors.
        if (previewRenderUtility == null)
            previewRenderUtility = new PreviewRenderUtility();

//        previewRenderUtility.Cleanup();
  //      OnDisable();
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


