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
using System.Collections.Generic;
using DaggerfallConnect;


namespace DaggerfallWorkshop.DungeonGenerator
{
    [RequireComponent(typeof(DungeonBlockSetup))]
    public class DungeonRecord : MonoBehaviour
    {
        public string dungeonName;
        public DungeonGenerator MyGenerator;
        public int[] textureTable;
        public int creationSeed = 0;
        public DFRegion.DungeonTypes dungeonType = DFRegion.DungeonTypes.HumanStronghold;
        public Vector3 rootPosition;
        public Quaternion rootRotation = Quaternion.identity;
        public List<BlockRecord> blocks;
        DungeonBlockSetup blockSetup;
        int[] defaultTextureTable = new int[] { 119, 120, 122, 123, 124, 168 };

    
        public void SetupDungeon
            (
            Vector3 rootPos, 
            Quaternion rootRot,
            string name = "", 
            int[] textureTable = null, 
            DFRegion.DungeonTypes dungeonType = DFRegion.DungeonTypes.HumanStronghold,
            int creationSeed = 0
            )
        {
            if (textureTable == null)
            {
                this.textureTable = defaultTextureTable;
            }
            else
                this.textureTable = textureTable;

            this.rootPosition = rootPos;
            this.rootRotation = rootRot;
            this.creationSeed = creationSeed;
            this.name = name;
            this.dungeonType = dungeonType;

            this.blocks = new List<BlockRecord>();
        }

   

        /// <summary>
        /// 
        /// </summary>
        public void CreateDungeon()
        {
            if (!DaggerfallWorkshop.DaggerfallUnity.Instance.IsReady)
            {
                Debug.LogError("Error: DFunity not ready");
                return;
            }


            if (!blockSetup)
                blockSetup = this.GetComponent<DungeonBlockSetup>();

            if(blocks == null)
            {
                Debug.LogError("no blocks for Dungeon Creation, stopping");
                return;
            }
            else if(blocks.Count == 0)
            {
                Debug.LogError("no blocks for Dungeon Creation, stopping");
                return;

            }

            this.transform.position = rootPosition;
            this.transform.rotation = rootRotation;
            for(int i = 0; i < blocks.Count; i++)
            {
                BlockRecord record = blocks[i];
                blockSetup.SetupDungeonBlock(record, record.isStartBlock, textureTable, dungeonType);
            }


        }

    }
}
