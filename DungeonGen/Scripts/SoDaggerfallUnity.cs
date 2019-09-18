using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility;

namespace DaggerfallWorkshop {

    public class SoDaggerfallUnity : ScriptableObject
    {
        // General
        public string Arena2Path;
        public int ModelImporter_ModelID = 456;
        public string BlockImporter_BlockName = "MAGEAA01.RMB";
        public string CityImporter_CityName = "Daggerfall/Daggerfall";
        public string Experimental_CityLayoutName = "Daggerfall/Aldcart";
        public string DungeonImporter_DungeonName = "Daggerfall/Privateer's Hold";

        // Performance options
        public bool Option_SetStaticFlags = true;
        public bool Option_CombineRMB = true;
        public bool Option_CombineRDB = true;
        public bool Option_BatchBillboards = true;

        // Import options
        public bool Option_AddMeshColliders = true;
        public bool Option_AddNavmeshAgents = true;
        public bool Option_RMBGroundPlane = true;

        // Prefab options
        public bool Option_ImportLightPrefabs = true;
        public Light Option_CityLightPrefab = null;
        public Light Option_DungeonLightPrefab = null;
        public Light Option_InteriorLightPrefab = null;
        public bool Option_ImportDoorPrefabs = true;
        public DaggerfallActionDoor Option_DungeonDoorPrefab = null;
        public DaggerfallActionDoor Option_InteriorDoorPrefab = null;
        public DaggerfallRMBBlock Option_CityBlockPrefab = null;
        public DaggerfallRDBBlock Option_DungeonBlockPrefab = null;
        public MobilePersonMotor Option_MobileNPCPrefab = null;
        public bool Option_ImportEnemyPrefabs = true;
        public DaggerfallEnemy Option_EnemyPrefab = null;
        public bool Option_ImportRandomTreasure = true;
        public DaggerfallLoot Option_LootContainerPrefab = null;
        public GameObject Option_DungeonWaterPrefab = null;
        public Vector3 Option_DungeonWaterPlaneSize = Vector3.one;
        public Vector3 Option_DungeonWaterPlaneOffset = Vector3.zero;

        // Time and space options
        public bool Option_AutomateTextureSwaps = true;
        public bool Option_AutomateSky = true;
        public bool Option_AutomateCityWindows = true;
        public bool Option_AutomateCityLights = true;
        public bool Option_AutomateCityGates = false;


    }
}