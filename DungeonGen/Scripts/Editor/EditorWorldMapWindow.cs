using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using DaggerfallWorkshop.DungeonGenerator;
using DaggerfallConnect.Arena2;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using DaggerfallConnect;
using System.Linq;
using System;


public class EditorWorldMapWindow : EditorWindow {

    [MenuItem("Daggerfall Tools/Daggerfall Map")]
    static void ShowWindow()
    {
        GetWindow<EditorWorldMapWindow>("Daggerfall Map");
    }

    bool bNeedsRedraw = false;
    PreviewRenderUtility previewRenderUtility;
    float ZoomFactor = 1.0f;
    float WindowZoomFactor = 1.0f;      // At 1.0 the window is large enough for a full 1000x500 pixel map
    Vector2 PanOffset = Vector2.zero;

    int OldWindowWidth;
    int OldWindowHeight;

    int WindowHeight;
    int WindowWidth;

    int MapWindowWidth;
    int MapWindowHeight;

    Vector2 DimMapBorder;


    Rect rectMap;
    Rect rectFooter;
    int FooterMinimumHeight = 40;

    /// <summary>
    /// The original Map Texture
    /// </summary>
    Texture2D MapTexture;

    Texture2D txtDaggerfall;

    /// <summary>
    /// The current display (zoomed or panned) of the map texture
    /// </summary>
    Texture2D MapDisplay;

    /// <summary>
    /// The temporary array of pixels
    /// </summary>
    Color32[] MapPixels;

    /// <summary>
    /// Prints the main map window
    /// </summary>
    void PrintMapWindow()
    {

        GUILayout.Box(MapDisplay);
        rectMap = GUILayoutUtility.GetLastRect();
    }

    /// <summary>
    /// Just used for refreshing the window
    /// </summary>
    void OnInspectorUpdate()
    {
        if (OldWindowHeight != MapWindowHeight || OldWindowWidth != MapWindowWidth)
        {

            ResizeMap();

            OldWindowHeight = MapWindowHeight;
            OldWindowWidth = MapWindowWidth;

        }
    }

    void PrintFooterBar()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Resize"))
        {
            ResizeMap();
        }

        GUILayout.Space(10);
        GUILayout.Label("Mouse Map Coords:");
        GUILayout.Label(GetMouseMapCoords().ToString());

        GUILayout.Space(10);
        GUILayout.Label("Offset:");
        GUILayout.Label(PanOffset.ToString());

        GUILayout.Space(10);
        GUILayout.Label("Map Rect:");
        GUILayout.Label(rectMap.ToString());

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
    }

    Vector2Int GetMouseMapCoords()
    {
        Vector2Int iMapLoc;
        Vector2 vMouseMapPosition;
        Vector2 MapPixelSize = new Vector2(MapTexture.width, MapTexture.height);

        vMouseMapPosition = Event.current.mousePosition;

        // Add border offset
        vMouseMapPosition.x -= DimMapBorder.x;
        vMouseMapPosition.y -= DimMapBorder.y;

        float TempZoom = 1 / ZoomFactor;

        // How many map pixels one screen pixel counts for
        Vector2 NormalizedPixelUnit = new Vector2(TempZoom, TempZoom);

        // Get the normalized map pixels (assuming no offset)
        vMouseMapPosition *= NormalizedPixelUnit;

        // Get current offset to add
        Vector2 AdjustedPanOffset = PanOffset;      // Copy over the pan offset to reverse it
        Vector2 EditorWindowSize = (MapPixelSize * TempZoom) / MapPixelSize;

        // TODO Corner offset is wrong somehow
        AdjustedPanOffset.y = 1 - AdjustedPanOffset.y - EditorWindowSize.y; // Adjust the Y axis as it's reversed


        // How many map pixels are we over
        AdjustedPanOffset *= MapPixelSize;

        iMapLoc = Vector2Int.RoundToInt(vMouseMapPosition + AdjustedPanOffset);
        bNeedsRedraw = true;

        return iMapLoc;
    }

    void ResizeMap()
    {
        CheckOffsetBoundaries();

        int OffsetX = Mathf.RoundToInt(PanOffset.x);
        int OffsetY = Mathf.RoundToInt(PanOffset.y);
        MapWindowWidth = Mathf.RoundToInt(this.position.width);
        MapWindowHeight = Mathf.RoundToInt(this.position.height);
        float MapSizeRatio = (float)MapTexture.height / (float)MapTexture.width;

        // Map Window Tall = Scale width of map to width of window
        if (MapWindowWidth < MapWindowHeight)
        {
            MapWindowHeight = Mathf.RoundToInt(MapWindowWidth * MapSizeRatio); // Generally Height is 2x the width based on the map ratio.
        } else
        // Map window Wide = Widest you can be while still having room for the footer bar
        {
            MapWindowHeight -= FooterMinimumHeight;
            MapWindowWidth = Mathf.RoundToInt(MapWindowHeight / MapSizeRatio);
        }

        MapWindowWidth = Mathf.RoundToInt(MapWindowWidth);
        MapWindowHeight = Mathf.RoundToInt(MapWindowHeight);
        MapWindowWidth -= 14;    // Offset
        MapWindowHeight -= 14;

        if (MapWindowHeight < 1) MapWindowHeight = 1;
        if (MapWindowWidth < 1) MapWindowWidth = 1;

        //Vector2 vPanOffset = PanOffset / EditorWindowWidth;
        //vPanOffset /= ZoomFactor;

        // Need to copy to new array the perfect size of the new items,  Current Array size does not match width and height of setpixels

        MapPixels = MapTexture.GetPixels32();
        //MapDisplay = new Texture2D(Width, Height);
        //MapDisplay.SetPixels32(OffsetX, OffsetY, Width, Height, MapPixels);

        MapDisplay = TextureScaler.scaled(MapTexture, MapWindowWidth, MapWindowHeight, ZoomFactor, PanOffset.x, PanOffset.y);
        // Isolate the portion of the map being used based on scale 2.0 scale = 2 pixels per 1 actual pixel

        // How big is the map compared to the 

        // Compare it's dimensions to the window dimensions.

        //MapTexture.Resize(Mathf.RoundToInt(MapTexture.width * 0.9f), Mathf.RoundToInt(MapTexture.height * 0.9f));
        MapDisplay.Apply();

        //Debug.Log("Width: " + MapWindowWidth.ToString() + " Height: " + MapWindowHeight.ToString() +
        //    " Zoom: " + ZoomFactor.ToString() + " offset: " + PanOffset.ToString());


        bNeedsRedraw = true;
    }

    void InputMapControls()
    {

        // Only use controls if inside this area
        //if (rHotArea.Contains(Event.current.mousePosition) == false)
        //    return;

        float ScrollMod = 0.8f;
        float PanMod = 0.5f;

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

        // Initiate the pan if holding middle mouse and dragging
        if (Event.current.type == EventType.MouseDrag && Event.current.button == 1)
        {
            Vector2 PanTemp = drag * ZoomFactor * PanMod;
            PanTemp /= MapWindowWidth;
            PanTemp /= ZoomFactor;
            PanTemp.x *= -1;

            PanOffset += PanTemp;

            ResizeMap();
        }

        if (Event.current.type == EventType.ScrollWheel)
        {
            ZoomFactor -= ScrollDelta.y * 0.1f;
            ZoomFactor = Mathf.Clamp(ZoomFactor, 1.0f, 50.0f);

            ResizeMap();
        }

    }

    void CheckOffsetBoundaries()
    {
        //Debug.Log("checking offset boundaries");

        // If zoom at 1 offset can only go to max of 0
        // zoom at 2 offset max 0.5f
        // zoom at 4 offset max 
        float tempzoom = 1 / ZoomFactor;

        PanOffset.x = Mathf.Clamp(PanOffset.x, 0, 1 - tempzoom);
        PanOffset.y = Mathf.Clamp(PanOffset.y, 0, 1 - tempzoom);

    }

    /// <summary>
    /// Takes a position on the screen and converts it to a map pixel.  Clamped map edges
    /// </summary>
    /// <param name="MapScrenLoc">The pixel location on the window.  Accounts for borders</param>
    /// <returns></returns>
    Vector2 ScreenToMapCoords(Vector2 MapScrenLoc)
    {
        float TempZoom = 1 / ZoomFactor;

        // How many map pixels one screen pixel counts for
        Vector2 NormalizedPixelUnit = new Vector2(ZoomFactor, ZoomFactor);

        MapScrenLoc /= NormalizedPixelUnit;
        // Subtract the offset

        Vector2 MapPixelSize = new Vector2(MapTexture.width, MapTexture.height);
        // Get current offset to add
        Vector2 AdjustedPanOffset = PanOffset;      // Copy over the pan offset to reverse it
        Vector2 EditorWindowSize = (MapPixelSize * ZoomFactor) / MapPixelSize;

        AdjustedPanOffset.y *= ZoomFactor * -1;
        AdjustedPanOffset.y = 1 - AdjustedPanOffset.y - EditorWindowSize.y; // Adjust the Y axis as it's reversed

        // How many map pixels are we over
        AdjustedPanOffset.x *= ZoomFactor * -1;
        AdjustedPanOffset *= MapPixelSize;

        MapScrenLoc += AdjustedPanOffset;
        MapScrenLoc += DimMapBorder;

        return MapScrenLoc;

    }

    /// <summary>
    /// Converts a pixel location on actual map, and determines where that point is on the screen.
    /// Takes texture border into account
    /// </summary>
    /// <param name="MapLoc">The map location</param>
    /// <returns></returns>
    Vector2 MapToMapScreenCoords(Vector2 MapLoc)
    {
        float TempZoom = 1 / ZoomFactor;

        // How many map pixels one screen pixel counts for
        Vector2 NormalizedPixelUnit = new Vector2(ZoomFactor, ZoomFactor);

        MapLoc *= NormalizedPixelUnit;
        // Subtract the offset

        Vector2 MapPixelSize = new Vector2(MapTexture.width, MapTexture.height);
        // Get current offset to add
        Vector2 AdjustedPanOffset = PanOffset;      // Copy over the pan offset to reverse it
        Vector2 EditorWindowSize = (MapPixelSize * ZoomFactor) / MapPixelSize;
        
        AdjustedPanOffset.y *= ZoomFactor * -1;
        AdjustedPanOffset.y = 1 - AdjustedPanOffset.y - EditorWindowSize.y; // Adjust the Y axis as it's reversed
        
        // How many map pixels are we over
        AdjustedPanOffset.x *= ZoomFactor * -1;
        AdjustedPanOffset *= MapPixelSize;

        MapLoc += AdjustedPanOffset;
        MapLoc += DimMapBorder;

        return MapLoc;
    }

    /// <summary>
    /// Give the icon and the pixel location on the map and have it draw.
    /// </summary>
    /// <param name="img">Texture icon to draw</param>
    /// <param name="loc">Pixel location on the map starting from top left.</param>
    void DisplayTextureAtCoords(Texture2D img, Vector2 loc)
    {
        // Convert Loc to on screen location
        loc = MapToMapScreenCoords(loc);

        if (IsOutsideMap(loc))
        {
            //Debug.LogError("Texture to display is outside map area. " + loc.ToString());
            return;
        }



        Rect rAspectScale = new Rect(0, 0, 0.05f, 0.05f);
        Rect rDrawHere = new Rect(DimMapBorder.x + loc.x, DimMapBorder.y + loc.y, 50, 50);  // Pixel width

        GUI.DrawTexture(rDrawHere, img);
        //GUI.DrawTextureWithTexCoords(rDrawHere, img, rAspectScale,true);


    }

    /// <summary>
    /// Returns true if the coordinates are outside the texture map.  
    /// </summary>
    /// <param name="loc">In terms of map pixels.  Top left is 0,0</param>
    /// <returns></returns>
    bool IsOutsideMap(Vector2 loc)
    {
        if (loc.x < 0 || loc.x > MapTexture.width || loc.y < 0 || loc.y > MapTexture.height)
            return true;

        return false;
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
        //SetGuiStyles();

        InputMapControls();
        //InputKeyboardCommands();

        EditorGUILayout.BeginVertical();

        PrintMapWindow();

        

        PrintFooterBar();

        EditorGUILayout.EndVertical();
        DisplayTextureAtCoords(txtDaggerfall, new Vector2(200,200));

    }

    void OnEnable()
    {

        DimMapBorder = new Vector2(7, 7);
        MapTexture = Resources.Load("DF-map-Iliac_Bay_Political_View") as Texture2D;
        txtDaggerfall = Resources.Load("Location-Daggerfall") as Texture2D;

        MapPixels = MapTexture.GetPixels32();

        Texture2D txBlue = new Texture2D(1, 1);
        txBlue.SetPixel(0, 0, Color.blue);
        txBlue.Apply();

        

    }

}
