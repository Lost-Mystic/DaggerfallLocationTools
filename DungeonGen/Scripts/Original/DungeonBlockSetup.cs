/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using UnityEngine;
using System.Collections;
using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallConnect.Arena2;


namespace DaggerfallWorkshop.DungeonGenerator
{

    [RequireComponent(typeof(DaggerfallRDBBlock))]
    public class DungeonBlockSetup : MonoBehaviour
    {
        
        public int blockIndex = 1050;
        public bool isStartBlock = false;
        public int[] textureTable = new int[] { 119, 120, 122, 123, 124, 168 };
        DFRegion.DungeonTypes dungeonType = DFRegion.DungeonTypes.HumanStronghold;
        DaggerfallRDBBlock cloneFrom = null;
        public Vector2 position;
        public System.Collections.Generic.Dictionary<int, RDBLayout.ActionLink> actLink;


        public void SetupDungeonBlock
            (
            int index,
            bool isStartBlock,
            int[] textureTable,
            DFRegion.DungeonTypes dungeonType = DFRegion.DungeonTypes.HumanStronghold,
            int seed = 0,
            DaggerfallRDBBlock cloneFrom = null
            )
        {
            this.blockIndex = index;
            this.isStartBlock = isStartBlock;
            this.textureTable = textureTable;
            this.dungeonType = dungeonType;
            this.cloneFrom = cloneFrom;
            CreateDungeonBlock();

        }


        public void SetupDungeonBlock
            (
            BlockRecord record,
            bool isStartBlock,
            int[] textureTable,
            DFRegion.DungeonTypes dungeonType = DFRegion.DungeonTypes.HumanStronghold,
            int seed = 0,
            DaggerfallRDBBlock cloneFrom = null
            )
        {
            this.blockIndex = record.index;
            actLink = new System.Collections.Generic.Dictionary<int, RDBLayout.ActionLink>();

            this.isStartBlock = isStartBlock;
            this.textureTable = textureTable;
            this.dungeonType = dungeonType;
            this.cloneFrom = cloneFrom;
            this.position = record.position;
            CreateDungeonBlock();
        }


        public void CreateDungeonBlock()
        {
            DungeonGenerator myGen = GetComponent<DungeonRecord>().MyGenerator;
            Debug.LogWarning("Creating dungeon block: " + blockIndex + " isStarting block: " + isStartBlock);
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
            {
                Debug.LogError("CreateDungeonBlock found dfUnity not ready; stopping");
                return;
            }

            // Create base object
            DFBlock blockData = dfUnity.ContentReader.BlockFileReader.GetBlock(blockIndex);
            if (blockData.Type != DFBlock.BlockTypes.Rdb)
            {
                Debug.LogError(string.Format("Invalid block index : {0} | block name: {1} | block type: {2}, returning", blockIndex, blockData.Name, blockData.Type));
                return;

            }
            
            GameObject go = RDBLayout.CreateBaseGameObject(blockData.Name, actLink, textureTable, true, cloneFrom);

            // Add exit doors
            if (isStartBlock)
            {
                StaticDoor[] doorsOut;
                RDBLayout.AddActionDoors(go, actLink, ref blockData, textureTable);
            }

            // Add action doors
            RDBLayout.AddActionDoors(go, actLink, ref blockData, textureTable);

            // Add lights
            RDBLayout.AddLights(go, ref blockData);

            // Add flats
            DFBlock.RdbObject[] editorObjectsOut = new DFBlock.RdbObject[0];
            GameObject[] startMarkersOut = null;
            GameObject[] enterMarkersOut = null;

            if (myGen.GenerateTreasure)
                RDBLayout.AddFlats(go, actLink, ref blockData, out editorObjectsOut, out startMarkersOut, out enterMarkersOut, dungeonType);

            // Set start markers
            DaggerfallRDBBlock dfBlock = (cloneFrom != null) ? cloneFrom : go.GetComponent<DaggerfallRDBBlock>();
            if (dfBlock != null)
                dfBlock.SetMarkers(startMarkersOut, enterMarkersOut);

            // Add enemies
            if (myGen.GenerateEnemies)
                RDBLayout.AddRandomEnemies(go, editorObjectsOut, dungeonType,0.5f,ref blockData,startMarkersOut);

            go.transform.SetParent(this.transform);
            go.transform.localPosition = new Vector3(position.x, 0, position.y);

        }




    }

}