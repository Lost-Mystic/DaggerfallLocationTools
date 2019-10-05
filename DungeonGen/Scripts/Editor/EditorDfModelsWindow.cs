using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DaggerfallWorkshop.DungeonGenerator;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using DaggerfallConnect;
using System.Linq;
using System;



public class EditorDfModelsWindow : EditorWindow
{
    [MenuItem("Daggerfall Tools/Daggerfall Models")]
    static void ShowWindow()
    {
        GetWindow<EditorDfModelsWindow>("Daggerfall Models");
    }

    #region Variables

    string VersionNumber = "0.1";
    string BuildDate = "09/20/2019";

    Editor gameObjectEditor;
    PreviewRenderUtility previewRenderUtility;  // Hold our own previewrender util (used for custom preview windows)
    SoModelRecords soData;                  // Holds link to the Scriptable object with the Model data
    int RecordDisplayMaxCount = 9;          // Maximum amount of models that can be displayed at one time in window. (Scroll to see more)
    BareDaggerfallMeshStats myBareMesh;
    List<DfModelRecord> FilteredModels;

    int MaxEntriesOnFilteredListWindow;     // Max entries you can have on the filtered list based on current window height


    float dimFilteredListWidth = 100f;
    float dimPreviewFilteredHeight = 200f;
    float dimPreviewFilteredHeightPercent = 0.5f;       // What height percentage of the window does it take up
    float dimLabelEntryHeight = 20f;                    // Height of the label entry section
    float dimLabelListingHeight = 60f;                  // Suggested height of the label listings (they'll wrap and take as much as required
    float dimDebugHeight;
    float dimFooterHeight;

    float FilteredModelListEntryWidth = 60f;
    float FilteredModelListEntryHeight = 20f;

    Vector2 CollapseableLabelListScrollPos = new Vector2();

    GUIStyle gsFilteredSelected;
    GUIStyle gsFilteredSelectedPrime;       // For the primary selected item that shows up in the window (last clicked)
    GUIStyle gsLabelUnselected;
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

    #region ActionWrappers

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
    /// Adds a label to the selected objects
    /// </summary>
    /// <param name="LabelToAdd"></param>
    void AddLabel(string LabelToAdd) 
    {
        soData.TryToAddLabel(LabelToAdd, GetSelectedModelRecords());
        EditorUtility.SetDirty(soData);
        AssetDatabase.SaveAssets();
        //UpdateFilteredList();
        soData.GetAllCurrentLabels();
    }

    /// <summary>
    /// Removes a label from selected objects
    /// </summary>
    /// <param name="LabelToRemove"></param>
    void RemoveLabel(string LabelToRemove)
    {
        soData.TryToRemoveLabel(LabelToRemove, GetSelectedModelRecords());
        EditorUtility.SetDirty(soData);
        AssetDatabase.SaveAssets();
        UpdateFilteredList();
        soData.GetAllCurrentLabels();
    }

    /// <summary>
    /// Filter based on this label
    /// </summary>
    /// <param name="selLabel"></param>
    void SelectedLabelClickAdd(string selLabel)
    {
        // If has no key pressed only do the one label
        if (!Event.current.control)
        {
            SelectedLabel = new List<string>();
        }

        SelectedLabel.Add(selLabel);
        txtLabelEntry = selLabel;

        UpdateFilteredList();
    }

    /// <summary>
    /// Remove this label from current filters
    /// </summary>
    /// <param name="selLabel"></param>
    void SelectedLabelClickRemove(string selLabel)
    {
        // If more than one selected, merely single select the one you click on
        if (SelectedLabel.Count > 1 && !Event.current.control)
        {
            SelectedLabel = new List<string>();
            SelectedLabel.Add(selLabel);
            return;
        }


        // If has no key pressed only do the one label
        SelectedLabel.Remove(selLabel);

        UpdateFilteredList();
        bNeedsRedraw = true;
    }

    #endregion

    #region Inputs

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
        }
        else

        if (Event.current.shift)
        {
            // If selecting the same thing, ignore
            if (IsSelected(NowClickedIndex))
            {
                return;
            }
            else

            // If selecting something above last clicked, add any in between to selected index
            if (NowClickedIndex > soData.LastClickedIndex)
            {
                for (int i = soData.LastClickedIndex; i <= NowClickedIndex; i++)
                {
                    selectedIndex.Add(i);
                }
            }
            else
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
            }
            else
            {
                DeSelected = true;
            }
        }

        soData.LastClickedIndex = NowClickedIndex;

        UpdateMeshLoaded(NowClickedIndex);


        PreviewRenderInit();

        //if (FilteredModels.Count <= 0) return;

    }

    /// <summary>
    /// Key commands for this editor window
    /// </summary>
    void InputKeyboardCommands()
    {
        // Will keep cycling through selected items if more than on is selected.  Otherwise will go down list and
        // wrap to the top selecting each item one by one.

        if (Event.current.type != EventType.KeyDown) return;

        int MoveSelection = 0;
        int PageSize = MaxEntriesOnFilteredListWindow;

        if (Event.current.keyCode == KeyCode.UpArrow)
            MoveSelection = -1;
        if (Event.current.keyCode == KeyCode.DownArrow)
            MoveSelection = 1;
        if (Event.current.keyCode == KeyCode.PageUp)
            MoveSelection = -1 * PageSize;
        if (Event.current.keyCode == KeyCode.PageDown)
            MoveSelection = 1 * PageSize;

        if (MoveSelection != 0)
        {
            if (selectedIndex.Count == 0)
            {
                soData.LastClickedIndex = 0;
                selectedIndex.Add(soData.LastClickedIndex);
                UpdateMeshLoaded(soData.LastClickedIndex);
                UpdateFilteredModelListScrollBarToSelectedEntry();
                return;
            }

            if (selectedIndex.Count == 1)
            {
                soData.LastClickedIndex += MoveSelection;
                if (soData.LastClickedIndex >= FilteredModels.Count)
                    soData.LastClickedIndex = 0;
                if (soData.LastClickedIndex < 0)
                    soData.LastClickedIndex = FilteredModels.Count - 1;

                selectedIndex = new List<int>();
                selectedIndex.Add(soData.LastClickedIndex);
                UpdateMeshLoaded(soData.LastClickedIndex);
                UpdateFilteredModelListScrollBarToSelectedEntry();
                return;
            }
        }
        /*
            if (selectedIndex.Count > 1)
            {
                selectedIndex.Sort();

                // Get exactly which index LastClicked is within the SelectedIndex

                int si = selectedIndex.IndexOf(soData.LastClickedIndex);
                si++;
                if (si >= selectedIndex.Count)
                    si = 0;
                // Increment that index position
                soData.LastClickedIndex = selectedIndex[si];
                CheckMeshLoaded(soData.LastClickedIndex);
            }
        */

        // CTRL A = Select All
        if (Event.current.keyCode == KeyCode.A && Event.current.control)
        {
            selectedIndex = new List<int>();
            for (int i = 0; i < FilteredModels.Count; i++)
            {
                selectedIndex.Add(i);
            }
            bNeedsRedraw = true;
        }


        // Plus Adds label to selected
        if (Event.current.keyCode == KeyCode.F4)
        {
            AddLabel(txtLabelEntry);
        }


        // Minus removes label from selected
        if (Event.current.keyCode == KeyCode.F9)
        {
            RemoveLabel(txtLabelEntry);
        }

        // F2 renames a label
        if (Event.current.keyCode == KeyCode.F2)
        {

        }


        // Clicking on a label puts it in text field

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
        }
        else
        {
            ScrollDelta = Vector2.zero;
        }


        PrevCameraPos.z += ScrollDelta.y * ScrollMod;

        //previewRenderUtility.camera.transform.position = PrevCameraPos;



        if (Event.current.type == EventType.MouseDrag && Event.current.button == 2)
        {
            previewRenderUtility.camera.transform.Translate(previewRenderUtility.camera.transform.right * -drag.x * PanMod * 0.3f);
            previewRenderUtility.camera.transform.Translate(previewRenderUtility.camera.transform.up * -drag.y * PanMod * 0.3f);
        }
        else
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

    #endregion

    #region UpdatingRefreshing

    /// <summary>
    /// Load the proper mesh of the last clicked selected item.
    /// </summary>
    /// <param name="NowClickedIndex"></param>
    void UpdateMeshLoaded(int NowClickedIndex)
    {
        if (soData.record.Count <= 0) return;
        if (FilteredModels.Count <= 0) return;
        if (selectedIndex.Count <= 0) return;

        //Debug.Log("Try to Check Mesh" + FilteredModels.Count.ToString() + " " + NowClickedIndex.ToString());

        if (NowClickedIndex == -1)
            NowClickedIndex = soData.LastClickedIndex;

        //Debug.Log("Try to Check Mesh" + FilteredModels[NowClickedIndex].ModelID.ToString());

        bool LikelyLostFocus = (selectedIndex.Count == 1 && soData.LastClickedIndex == 0);
        bool ForceUpdate = (NowClickedIndex == -1);
        bool ClickedOnSameItem = (NowClickedIndex == soData.LastClickedIndex);

        //  if ((!LikelyLostFocus && !ClickedOnSameItem) || ForceUpdate)
        {
            //Debug.Log("Loading Display Mesh ID: " + FilteredModels[NowClickedIndex].ModelID.ToString());
            myBareMesh = SpawnDfModel.GetBareMeshFromId(FilteredModels[NowClickedIndex].ModelID);

        }

        bNeedsRedraw = true;
        soData.LastClickedIndex = NowClickedIndex;
        EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// Moves the filtered list scroll bar to put the currently selected entry on the viewing screen
    /// </summary>
    void UpdateFilteredModelListScrollBarToSelectedEntry()
    {
        // Find number of entries that can fit in window, round up
        int iMaxEntries = Mathf.CeilToInt(rFilteredItemsScrollWindow.height / (float)FilteredModelListEntryHeight);
        int EntriesBefore = 0;
        // Max entires possible onscreen

        //EntriesBefore = Mathf.FloorToInt(FilteredModelListScrollPosition.y / FilteredModelListEntryHeight);

        EntriesBefore = soData.LastClickedIndex - (iMaxEntries / 2);

        // If filtered list is less than can fit on the screen at one time
        if (FilteredModels.Count < iMaxEntries)
        {
            FilteredModelListScrollPosition.y = 0;
        }
        else
        if (EntriesBefore <= 0)  // If at bottom of the section
        {
            FilteredModelListScrollPosition.y = 0;
        }
        else
        if ((EntriesBefore + iMaxEntries) >= FilteredModels.Count)   // Top section of chart
        {
            FilteredModelListScrollPosition.y = (FilteredModels.Count - iMaxEntries) * FilteredModelListEntryHeight;
        }
        else // Somewhere in the middle
        {
            FilteredModelListScrollPosition.y = soData.LastClickedIndex * FilteredModelListEntryHeight;
        }
    }

    /// <summary>
    /// Sets the filtered list after it's been changed somehow.
    /// </summary>
    void UpdateFilteredList()
    {
        // If no records then return
        if (soData.record.Count <= 0) return;

        // Create a new list of the actual ID numbers of selected items
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
        }
        else

        // If no labels selected show the entire list
        if (SelectedLabel.Count <= 0)
        {
            FilteredModels = soData.record;
        }
        else
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
        UpdateMeshLoaded(soData.LastClickedIndex);

    }

    void UpdateSelectedList(List<int> SelectedID)
    {
        if (FilteredModels.Count <= 0) return;

        bool FoundSelectedRecordInNewFilteredList = false;

        // Save the last clicked ID model, and see if it's still selected at the end.
        int LastClickedId;

        if (SelectedID.Count > 0)
            LastClickedId = SelectedID[0];
        else
            LastClickedId = 0;

        Debug.Log("Last clicked ID is: " + LastClickedId.ToString());

        soData.LastClickedIndex = -1;

        selectedIndex = new List<int>();

        // No filtered items to display
        if (FilteredModels.Count <= 0)
        {
            return;
        }
        // Sort list
        SelectedID.Sort();
        int selCount = 0;
        int filCount = 0;

        // Ensures the same records selected before stay selected
        while (selCount < SelectedID.Count && filCount < FilteredModels.Count)
        {
            // Goes through each old selected ID to compare to new list
            if (SelectedID[selCount] == FilteredModels[filCount].ModelID)
            {
                selectedIndex.Add(filCount);
                selCount++;
            }

            // If you get to the same entry as the prime selected entry or beyond.  Reassign to that selected entry.
            if (LastClickedId <= FilteredModels[filCount].ModelID && !FoundSelectedRecordInNewFilteredList)
            {
                FoundSelectedRecordInNewFilteredList = true;
                selectedIndex.Add(filCount);
                soData.LastClickedIndex = filCount;
            }

            filCount++;
        }


        // If LastClickedIndex is still -1, set it to the first entry
        if (!FoundSelectedRecordInNewFilteredList)
        {
            if (selectedIndex.Count > 0)
                soData.LastClickedIndex = selectedIndex[0];
            else
            {
                soData.LastClickedIndex = 0;
                selectedIndex = new List<int>();
                selectedIndex.Add(soData.LastClickedIndex);

            }
        }

        UpdateFilteredModelListScrollBarToSelectedEntry();

    }

    #endregion

    #region PrintWindowElements

    /// <summary>
    /// Prints the filtered text list of model numbers to the editor window
    /// </summary>
    void PrintFilteredModelListToWindow()
    {

        GUILayout.BeginVertical(GUILayout.Width(dimFilteredListWidth)/*, GUILayout.Height(dimPreviewFilteredHeight)*/);

        FilteredModelListScrollPosition = GUILayout.BeginScrollView(FilteredModelListScrollPosition);

        // If there are no records listed, and no labels selected, the list should show default.
        if (FilteredModels.Count == 0 && SelectedLabel.Count <= 0)
        {
            FilteredModels = soData.record;
            UpdateFilteredList();
        }

        if (FilteredModels.Count == 0)
            GUILayout.Label("No items.", GUILayout.Height(dimPreviewFilteredHeight));

        //Color color_default = GUI.backgroundColor;

        GUIStyle gsCurrent = GUIStyle.none;


        // Determine Window Size.
        if (rFilteredItemsScrollWindow == null)
            rFilteredItemsScrollWindow = new Rect(0, 0, dimFilteredListWidth, dimPreviewFilteredHeight);

        // Find number of entries that can fit in window, round up
        MaxEntriesOnFilteredListWindow = Mathf.CeilToInt(rFilteredItemsScrollWindow.height / (float)FilteredModelListEntryHeight);
        int EntriesAfter = 0;
        int EntriesBefore = 0;

        EntriesBefore = Mathf.FloorToInt(FilteredModelListScrollPosition.y / FilteredModelListEntryHeight);

        // If filtered list is less than can fit on the screen at one time
        if (FilteredModels.Count < MaxEntriesOnFilteredListWindow)
        {
            EntriesBefore = 0;
            MaxEntriesOnFilteredListWindow = FilteredModels.Count;
        }
        else
        if (EntriesBefore <= 0)  // If at bottom of the section
        {
            EntriesBefore = 0;
            EntriesAfter = FilteredModels.Count - MaxEntriesOnFilteredListWindow;
        }
        else
        if ((EntriesBefore + MaxEntriesOnFilteredListWindow) >= FilteredModels.Count)   // Top section of chart
        {
            EntriesAfter = 0;
            EntriesBefore = FilteredModels.Count - MaxEntriesOnFilteredListWindow;
        }
        else // Somewhere in the middle
        {
            EntriesBefore = Mathf.FloorToInt(FilteredModelListScrollPosition.y / FilteredModelListEntryHeight);
            EntriesAfter = FilteredModels.Count - EntriesBefore - MaxEntriesOnFilteredListWindow;
        }


        // Put blank space before and after scroll view using unit size times units remaining
        // This is updated every frame so if list changes, it will be updated with the frame


        // This gets the array of records, and displays those already known.

        GUILayout.Space(EntriesBefore * FilteredModelListEntryHeight);

        for (int i = EntriesBefore; i < (MaxEntriesOnFilteredListWindow + EntriesBefore); i++)
        {
            //GUI.backgroundColor = (selectedIndex == i) ? color_selected : color_default;
            if (i == soData.LastClickedIndex)
                gsCurrent = gsFilteredSelectedPrime;
            else
            if (IsSelected(i))
                gsCurrent = gsFilteredSelected;
            else
                gsCurrent = GUIStyle.none;


            GUIContent entry = new GUIContent(FilteredModels[i].ModelID.ToString());


            if (GUILayout.Button(entry, gsCurrent, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false), GUILayout.Height(FilteredModelListEntryHeight), GUILayout.Width(FilteredModelListEntryWidth)))
            {
                InputSelectNewObjects(i, FilteredModels[i].ModelID);
            }

        }
        GUILayout.Space(EntriesAfter * FilteredModelListEntryHeight);

        GUILayout.EndScrollView();


        Rect rTest = GUILayoutUtility.GetLastRect();
        if (rTest.x != 0.0f)
            rFilteredItemsScrollWindow = rTest;

        GUILayout.EndVertical();
    }

    /// <summary>
    /// Prints the one or more currently selected models to the preview window
    /// </summary>
    void PrintSelectedModelsToPreview()
    {
        dimPreviewFilteredHeight = position.height - 260;
        if (dimPreviewFilteredHeight < 200) dimPreviewFilteredHeight = 200;


        GUILayout.BeginVertical(GUILayout.ExpandHeight(true)/*, GUILayout.Height(dimPreviewFilteredHeight)*/);



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

        DrawRenderPreview(myBareMesh, GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - dimFilteredListWidth, dimPreviewFilteredHeight), GUIStyle.none);


        GUILayout.EndVertical();
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
            AddLabel(txtLabelEntry);
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Remove"))
        {
            // Check if label has been added to any items at all
            RemoveLabel(txtLabelEntry);
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Rename"))
        {

        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Prints the currently selected labels bar section below the model preview.
    /// </summary>
    void PrintCurrentSelectedLabels()
    {

        EditorGUILayout.BeginHorizontal();

        GUIStyle fds = new GUIStyle();
        fds = EditorStyles.miniButton;

        // Print selected labels first
        for (int i = 0; i < SelectedLabel.Count; i++)
        {
            // Remove selected label when clicked
            if (GUILayout.Button(SelectedLabel[i]))
            {
                SelectedLabelClickRemove(SelectedLabel[i]);
            }

            /*
            if (GUILayout.Button("X", fds))
            {
                //soData.TryToRemoveLabel(SelectedLabel[i], GetSelectedModelRecords());
                SelectedLabel.Remove(SelectedLabel[i]);
                SelectedLabel.TrimExcess();
                UpdateFilteredList();
            }
            */
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
                        SelectedLabelClickAdd(FilteredModels[soData.LastClickedIndex].Labels[i]);
                    }
                }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Prints the option bar, with options to open or filter lists etc.
    /// </summary>
    void PrintLabelOptionsBar()
    {


        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));

        GUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(bHideLabeled ? "Show All" : "Hide Labeled", bHideLabeled ? EditorStyles.toolbarButton : EditorStyles.whiteLabel))
        {
            bHideLabeled = !bHideLabeled;

            if (bHideLabeled)
                SelectedLabel = new List<string>();

            soData.GetAllCurrentLabels();
            UpdateFilteredList();

        }

        //bHideLabeled = GUILayout.Toggle(bHideLabeled, "Hide Labeled Models");

        bShowLabelListArea = GUILayout.Toggle(bShowLabelListArea, "Show Label List");

        GUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Prints the list of ALL labels in the editor window. (if the option to show it exists)
    /// </summary>
    void PrintCollapseableLabelList()
    {
        float viewWidth = EditorGUIUtility.currentViewWidth;

        Rect prevRect = GUILayoutUtility.GetLastRect();
        float dimWidth = viewWidth;
        float dimLineCount = 0;
        int dimRowCount = 0;

        GUIStyle gsCurrent = new GUIStyle();

        EditorGUILayout.BeginVertical(GUILayout.MinHeight(30));

        CollapseableLabelListScrollPos = GUILayout.BeginScrollView(CollapseableLabelListScrollPos,false,false,GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal(GUILayout.MaxWidth(viewWidth));
        //GUILayout.BeginArea(new Rect(0, 450, viewWidth, 80));


        foreach (string s in soData.Labels)
        {
            if (SelectedLabel.Contains(s))
            {
                if (GUILayout.Button(s, gsLabelSelected, GUILayout.ExpandWidth(true)))
                {
                    SelectedLabelClickRemove(s);
                }
            }
            else
            {
                if (GUILayout.Button(s, gsLabelUnselected, GUILayout.ExpandWidth(true)))
                {
                    SelectedLabelClickAdd(s);
                }
            }
            /*
            //dimLineCount += GUILayoutUtility.GetLastRect().width;
            GUIContent gc = new GUIContent(s);
            dimLineCount += GUILayoutUtility.GetRect(gc, gsLabelUnselected).width;


            if ((dimLineCount) > dimWidth)
            {
                //GUILayout.EndHorizontal();
                //GUILayout.BeginHorizontal();
                dimLineCount = 0;
                dimRowCount++;
            }
            */

        }

        //GUILayout.EndArea();
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();

        EditorGUILayout.EndVertical();



    }

    /// <summary>
    /// Prints debug buttons for editor at bottom.
    /// </summary>
    void PrintDebugEditorButtons()
    {

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
        

        if (GUILayout.Button("Ready Database for Saving", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            EditorUtility.SetDirty(soData);
        }

        if (GUILayout.Button("Refresh entire label listings", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            soData.GetAllCurrentLabels();
        }
        */
        if (GUILayout.Button("Print selected items", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            EditorUtility.SetDirty(soData);
            Debug.Log("---===Selected Index===---");
            for (int i = 0; i < selectedIndex.Count; i++)
            {
                Debug.Log("Index: " + selectedIndex[i].ToString());

            }
            Debug.Log("---===END Selected Index===---");
        }

        if (GUILayout.Button("Save Assets", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            SaveData();
        }

        if (GUILayout.Button("Load Assets", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
        {
            //EditorUtility.OpenFilePanel("MyTitle", Application.dataPath, "*.xml");
            LoadData();
        }

        EditorGUILayout.EndHorizontal();

    }

    string MyEscapeURL(string url)
    {
        return WWW.EscapeURL(url).Replace("+", "%20");
    }

    void PrintFooterBar()
    {
        string email = "mark@lostmystic.com";
        string subject = MyEscapeURL("Unity Daggerfall Model Editor - ");
        string body = MyEscapeURL("");
        

        GUIStyle gsHyperlink = new GUIStyle();
        gsHyperlink.border = new RectOffset(2, 2, 2, 2);
        GUIStyleState fsHyper = new GUIStyleState();
        fsHyper.textColor = Color.blue;

        gsHyperlink.normal = fsHyper;

        GUILayout.BeginHorizontal();

        GUIContent gcAuthor = new GUIContent("Written by Mark Barazzuol", "Previous BioWare Dragon Age Designer and a huge Daggerfall fan!");
        GUIContent gcContact = new GUIContent("mark@lostmystic.com", "Click here or send me an email with any comments or bugs.");

        GUILayout.Label(gcAuthor);
        GUILayout.Space(10);
        if (GUILayout.Button(gcContact,gsHyperlink))
        {

            Application.OpenURL("mailto:" + email + "?subject=" + subject + "&body=" + body);
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label("Version: " + VersionNumber);
        //GUILayout.Space(10);
        

        GUILayout.EndHorizontal();
    }

    #endregion

    /// <summary>
    /// Called by PrintSelectedModelsToPreview . Actually draws the model on the window.
    /// </summary>
    /// <param name="bareMesh"></param>
    /// <param name="r"></param>
    /// <param name="gstyle"></param>
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
            return;
        }

        previewRenderUtility.BeginPreview(r, gstyle);

        InputPreviewWindowControls(r);

        if (bareMesh.mesh != null)
            for (int i = 0; i < bareMesh.SubMeshIndex; i++)
            {

                previewRenderUtility.DrawMesh(bareMesh.mesh, Vector3.zero, Quaternion.Euler(-30f, 0f, 0f) * Quaternion.Euler(0f, 60, 0f), bareMesh.mat[i], i, bareMesh.mProp);

            }

        bool fog = false;
        Unsupported.SetRenderSettingsUseFogNoDirty(false);
        previewRenderUtility.camera.Render();
        Unsupported.SetRenderSettingsUseFogNoDirty(fog);
        Texture texture = previewRenderUtility.EndPreview();

        GUI.DrawTexture(r, texture);
    }

    void Update()
    {
        
        //Logic
        //EditorUtility.SetDirty(this);

        if (bNeedsRedraw)
        {
            this.Repaint();
            bNeedsRedraw = false;
        }

    }

    void OnGUI()
    {
        SetGuiStyles();
        InputKeyboardCommands();

        EditorGUILayout.BeginHorizontal();

        PrintSelectedModelsToPreview();

        PrintFilteredModelListToWindow();

        EditorGUILayout.EndHorizontal();

        PrintLabelTypeField();

        PrintCurrentSelectedLabels();

        PrintLabelOptionsBar();

        if (bShowLabelListArea)
            PrintCollapseableLabelList();

        PrintDebugEditorButtons();

        PrintFooterBar();

    }

    void SetGuiStyles()
    {
        gsLabelUnselected = new GUIStyle(EditorStyles.helpBox);
        gsLabelUnselected.stretchWidth = true;
        gsLabelUnselected.fixedWidth = 0;
        gsLabelUnselected.wordWrap = false;

        gsLabelSelected = new GUIStyle(gsLabelUnselected);
        gssSelected.textColor = Color.white;
        gsLabelSelected.fontStyle = FontStyle.Bold;
        gsLabelSelected.onNormal = gssSelected;
        gsLabelSelected.focused = gssSelected;
        gsLabelSelected.hover = gssSelected;
        gsLabelSelected.normal = gssSelected;

        gsFilteredSelected = new GUIStyle(EditorStyles.label);
        gssSelected.textColor = Color.white;
        gsFilteredSelected.fontStyle = FontStyle.Normal;
        gsFilteredSelected.onNormal = gssSelected;
        gsFilteredSelected.focused = gssSelected;
        gsFilteredSelected.hover = gssSelected;
        gsFilteredSelected.normal = gssSelected;

        gsFilteredSelectedPrime = new GUIStyle(EditorStyles.label);
        gssSelected.textColor = Color.yellow;
        gsFilteredSelectedPrime.fontStyle = FontStyle.Bold;
        gsFilteredSelectedPrime.onNormal = gssSelected;
        gsFilteredSelectedPrime.focused = gssSelected;
        gsFilteredSelectedPrime.hover = gssSelected;
        gsFilteredSelectedPrime.normal = gssSelected;

    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
    }

    XmlSerializer serializer;
    FileStream stream;

    bool SaveData()
    {
        string fileName = "ModelAssetsExport.xml";


        //fileName = EditorUtility.OpenFilePanel("Save file as", Application.dataPath, "*.xml");
        fileName = EditorUtility.SaveFilePanel("Save file as", Application.dataPath, "ModelAssetExport.xml", "xml");

        FileMode writeMode = FileMode.CreateNew;

        if (soData.record == null)
        {
            Debug.LogError("soData.record null in WriteBlockData; stopping");
            return false;
        }

        try
        {
            serializer = new XmlSerializer(typeof(List<DfModelRecord>));
            //string utf8;

            stream = new FileStream(Path.Combine(Application.dataPath, fileName), writeMode, FileAccess.Write);

            serializer.Serialize(stream, soData.record);

            /*
            using (StringWriter writer = new Utf8StringWriter())
            {
                
                //serializer.Serialize(writer, entry);
                
            }
            */


            
            
            stream.Close();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            return false;

        }
        return true;
    }

    bool LoadData()
    {
        FileMode readMode = FileMode.Open;
        string fileName;

        fileName = EditorUtility.OpenFilePanel("Select Model Database XML", Application.dataPath, "*.xml");

        try
        {
            soData.record = new List<DfModelRecord>();
            serializer = new XmlSerializer(typeof(List<DfModelRecord>));
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning("Null or Empty filname; exiting");
                return false;
            }

            if (!File.Exists(Path.Combine(Application.dataPath, fileName)))
            {
                Debug.LogError("File not found!");
                return false;
            }
            
            stream = new FileStream(Path.Combine(Application.dataPath, fileName), readMode, FileAccess.Read);
            soData.record = (List<DfModelRecord>)serializer.Deserialize(stream);
            stream.Close();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            soData.record = null;
            stream.Close();
            return false;
        }

        return true;
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


        Texture2D txBlue = new Texture2D(1, 1);
        txBlue.SetPixel(0, 0, Color.blue);
        txBlue.Apply();
        gssSelected = new GUIStyleState();
        gssSelected.background = txBlue;
        

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

        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 20);
        if (myBareMesh.mesh != null)
            bounds.Encapsulate(myBareMesh.mesh.bounds);
        PrevCameraPos = Vector3.forward * -1 * bounds.max.magnitude * 3.5f;
        previewRenderUtility.camera.transform.SetPositionAndRotation(PrevCameraPos, previewRenderUtility.camera.transform.rotation);
        
        
    }

    private void OnFocus()
    {

        UpdateMeshLoaded(soData.LastClickedIndex);
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


