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
using System.Collections.Generic;
using System.Collections;
using DaggerfallConnect;
using System.Linq;
using UnityEditor;

namespace DaggerfallWorkshop.DungeonGenerator
{
    //[RequireComponent(typeof(BlockRecordSerializer))]
    public class DungeonGenerator : MonoBehaviour
    {
        //Dungeon setup stuff
        float dimension = 51.2f;
        public int seed = 0;                                                //if 0 dungeon will be randomized, else will used provided seed
        public DFRegion.DungeonTypes dungeonType = DFRegion.DungeonTypes.HumanStronghold;

        [Tooltip ("Total Number of internal blocks including the root.  Minimum of 1")]
        public int numOfInternalBlocks = 2;

        [Tooltip("Not Implemented Yet. Leave at 1")]
        public int numOfExits = 1;                                          //not used yet

        [Tooltip("Will regenerate dungeon up to 10 times to maintain minimum distance from root.  This becomes easier of the internal blocks number is higher.")]
        public float minExitDistanceFromRoot = 53;


        public string dungeonName = "Generated_Dungeon";
        public bool filterSpecialBlocks = true;                             //if true, won't use special blocks in dungeon
        public bool filterWetBlocks = false;                                //if true, won't use wet blocks in dungeon
        public Vector3 customRootPosition = Vector3.zero;                   //set where the dungeon will be placed in unity world space
        public Quaternion customRootRotation = Quaternion.identity;         //set the rotation of the dungeon
        public int[] textureTable = new int[] { 119, 120, 122, 123, 124, 168 };

        public string fileName = "BlockRecordExport.xml"; //file name to read / write from

        public bool GenerateEnemies = true;
        public bool GenerateTreasure = true;

        public bool CombineIndividualModels = true;
        public DaggerfallActionDoor DungeonDoor;
        public DaggerfallLoot LootContainer;


        public SoModelRecords modelArchive;    //Link to the model records

        BlockRecordCollection blockCollection;   //list of block records created by Serializer
        List<BlockRecord> blocks;                //stores the blocks selected for dungeon
        Vector2[] shifts;
        bool isReady = false;


        public Dictionary<Vector2, float> distanceLookup = null;


        static DungeonGenerator instance = null;
        public static DungeonGenerator Instance
        {
            get
            {
                if (instance == null)
                {
                    if (GameObject.FindObjectOfType<DungeonGenerator>() == null)
                    {
                        GameObject go = new GameObject();
                        go.name = "Dungeon Generator";
                        instance = go.AddComponent<DungeonGenerator>();
                    }
                }
                return instance;
            }
        }


        DFBlock.RdbTypes[] validTypes = new DFBlock.RdbTypes[]  //used to filter out unwanted RDB blocks - not used
        {
            DFBlock.RdbTypes.Border,
            DFBlock.RdbTypes.Normal,
            DFBlock.RdbTypes.Quest,
            DFBlock.RdbTypes.Start,
            DFBlock.RdbTypes.Wet
        };

        //set to true to create a dungeon using current params - exists mainly for ease of use w/ inspector
        public bool FlagCreateNewDungeon = false;

        void OnDisable()
        {
            StopAllCoroutines();
        }

        void Update()
        {
            if (FlagCreateNewDungeon)
            {
                CreateNewDungeon();
                FlagCreateNewDungeon = false;
            }

        }

        public void CreateNewDungeon()
        {
            Destroy(GameObject.Find(dungeonName));
            LayOutDungeon();
        }

        private void Start()
        {
            // Add daggerfall unity
            // Add game manager
            // Add player                                       
            // Add input manager             
            // Add default doors from daggerfall

            //DFBlock df = new DFBlock();
            //DaggerfallUnity.Instance.ContentReader.GetBlock("Option_DungeonDoorPrefab", out df);
            DaggerfallUnity.Instance.Option_DungeonDoorPrefab = DungeonDoor;
            DaggerfallUnity.Instance.Option_LootContainerPrefab = LootContainer;
            DaggerfallUnity.Instance.Option_CombineRDB = CombineIndividualModels;
            //DaggerfallUnity.Instance.Option_DungeonDoorPrefab = DaggerfallUnity.Instance.serializedObject.FindProperty("Option_ImportDoorPrefabs");
            
        }

       


        public bool StartUp()
        {



            if (!DaggerfallUnity.Instance.IsReady)
                StartCoroutine(ReadyCheck());
            
            if (seed != 0)
                UnityEngine.Random.seed = seed;

            dimension = DaggerfallWorkshop.Utility.RDBLayout.RDBSide;
            blocks = new List<BlockRecord>();
            blockCollection = new BlockRecordCollection();

            shifts = new Vector2[]
            {
                new Vector2(dimension, 0),
                new Vector2(-dimension, 0),
                new Vector2(0,dimension),
                new Vector2(0,-dimension)
             };


            numOfExits = Mathf.Min(numOfInternalBlocks, numOfExits);
            distanceLookup = new Dictionary<Vector2, float>();

            if (!GetBlockList())                //failed to get list of RDB blocks
            {
                Debug.LogError("Failed to get block list, stopping");
                this.enabled = false;
                isReady = false;
            }
            isReady = true;
            return isReady;
        }



        

        /// <summary>
        /// Creates dungeon using paramaters
        /// </summary>
        /// <returns></returns>
        public GameObject LayOutDungeon(int count = 10)
        {

            Debug.Log("****** DUNGEON COUNT: " + count.ToString());

            StartUp();
            if(!isReady)
            {
                Debug.LogError("Failed to setup DungeonGenerator, stopping");
                return null;
            }

            GameObject DungeonObject = new GameObject("DungeonObject");
            DungeonRecord dungeonRecord = DungeonObject.AddComponent<DungeonRecord>();
            dungeonRecord.MyGenerator = this;
            dungeonRecord.SetupDungeon(customRootPosition, customRootRotation, dungeonName, textureTable, dungeonType);


            //add the root position to dictionary to avoid collisions
            distanceLookup.Add(Vector2.zero, 0);

            bool PositionsAllValid = GetPositions();
            if(!PositionsAllValid) //should never happen
            {
                Debug.LogError("GetPositions returned false");
                if(count > 0)
                {
                    Destroy(DungeonObject);
                    seed = 0;
                    count--;
                    return LayOutDungeon(count);
                }
                return null;
            }

            var x = distanceLookup.OrderBy(d => d.Value).ToList();

            //if farthest distance exit block below min distance, try a new
            //layout if possible
            bool distanceCheck = (numOfExits > 0 && numOfInternalBlocks > numOfExits);
            if (distanceCheck)
            {
                if (x[(x.Count - 1) - numOfExits].Value < minExitDistanceFromRoot && count > 0)
                {
                    Debug.LogError("exit < min distance from root.: ");
                    Destroy(DungeonObject);
                    count--;
                    seed = 0;
                    return LayOutDungeon(count);
                }
            }

            //Build up the blocks, starting w from the root, and the blocks closet to the root, and finishing
            //with the exit blocks, which will always be as far away as possible from root
            for (int i = 0; i < x.Count; i++)
            {
                Vector2 pos = Vector2.zero;
                BlockRecord record = null;
                if(i == 0) //get root block
                {
                    record = GetRandomBlock(DFBlock.RdbTypes.Start);
                    record.isStartBlock = true;

                }
                else if((x.Count - i) <= numOfExits)   //get additional starting blocks
                {
                   
                    record = GetRandomBlock(DFBlock.RdbTypes.Start);
                    record.isStartBlock = true;
      
                }
                else  //get internal blocks without exits
                {

                    record = GetRandomBlock(DFBlock.RdbTypes.Unknown);
                    record.isStartBlock = false;
                }

                if (record == null)
                {
                    Debug.LogError("Failed to get block record");
                    Destroy(DungeonObject);
                    if(count > 0)
                    {
                        count--;
                        return LayOutDungeon(count);
                    }
                    return null;


                }
                pos = x[i].Key;
                record.position = pos;
                blocks.Add(record);
            }

            SetupBorderBlocks();
            dungeonRecord.blocks = blocks;
            FlagCreateNewDungeon = false;   // Debug, not normally supposed to be here, but it hangs otherwise
            dungeonRecord.CreateDungeon();
            return DungeonObject;
        }

        /// <summary>
        /// Sets the Vect2 positions the internal blocks will use
        /// </summary>
        /// <returns></returns>
        public bool GetPositions()
        {
            bool check = false;
            Vector2 temp = Vector2.zero;
            for(int i = 0; i < numOfInternalBlocks; i ++)
            {
                check = GetNextStep(ref temp);
                if(check)
                {
                    if(distanceLookup.ContainsKey(temp))
                    {
                        Debug.LogError("distance lookup dictionary already contained: " + temp.ToString());
                        return false;
                    }
                    distanceLookup.Add(temp, Vector2.Distance(Vector2.zero, temp));
                }
                else
                {
                    Debug.LogError("Failed to get next step");
                    return false;
                }


            }

            return true;
        }



        /// <summary>
        /// Gets next random point for internal block
        /// if no free points are found for current pos, will work recursively w/
        /// a previous point; if that fails returns false
        /// </summary>
        /// <param name="currentPos"></param>
        /// <param name="usedPositions"></param>
        /// <returns></returns>
        bool GetNextStep(ref Vector2 currentPos, int count = 10)
        {
            Vector2 temp;
            var usedPositions = GetUsedPositions();
            shifts = shifts.OrderBy(v => UnityEngine.Random.Range(0, shifts.Length)).ToArray();

            foreach(Vector2 shift in shifts)
            {
                temp = currentPos + shift;
                if (CheckCollisions(temp))
                    continue;
                else
                {
                    Debug.LogWarning("Found Free Position: " + temp.ToString());
                    currentPos = temp;
                    return true;
                }
            }


            Debug.LogWarning("Failed to get free position.");
            count--;
            if (count > 0)
            {
                currentPos = usedPositions[UnityEngine.Random.Range(0, usedPositions.Count())];
                return GetNextStep(ref currentPos, count);
            }
            else
                return false;
        }

        /// <summary>
        /// returns true if pos is < 1 distance away from any existing vect2
        /// used by existing blocks
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        bool CheckCollisions(Vector2 pos)
        {

            var usedPositions = distanceLookup.Keys.ToList();
            foreach(Vector2 v in usedPositions)
            {
                bool check = (Vector2.Distance(v, pos) < 1);    
                if(check)
                {
                    //Debug.Log(string.Format("Check true: {0} {1} {2} {3} ", pos, v, check, usedPositions.Contains(pos)));
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks each possible position around internal blocks
        /// if not used, adds a border block record to list
        /// </summary>
        void SetupBorderBlocks()
        {
            Vector2[] usedPositions = GetUsedPositions();
            if (usedPositions == null)
            {
                Debug.LogError("Error: GetUsedIndicies returned null");
            }



            foreach (Vector2 shift in shifts)
            {

                foreach (Vector2 pos in usedPositions)
                {

                    Vector2 tmp = pos + shift;
                    if (!CheckCollisions(tmp))
                    {
                        BlockRecord record = GetRandomBlock(DFBlock.RdbTypes.Border);
                        record.position = tmp;
                        record.isStartBlock = false;
                        blocks.Add(record);

                        //Need to add border blocks to avoid them colliding - all other blocks
                        //have been added so this won't cause problems
                        distanceLookup.Add(tmp, -1);
                    }


                }

            }

        }

        /// <summary>
        /// Returns an Vec2[] of used positions
        /// </summary>
        /// <returns></returns>
        public Vector2[] GetUsedPositions()
        {
            return distanceLookup.Keys.ToArray(); //blocks.Select(p => p.position).ToArray();  
        }

        /// <summary>
        /// returns an int[] of used block indices to prevent
        /// using the same blocks when possible
        /// </summary>
        /// <returns></returns>
        public int[] GetUsedIndices()
        {
            return blocks.Select(p => p.index).ToArray();  
        }



        /// <summary>
        /// Gets a random block based on rdb type.  Will recursively try and find unique block
        /// Returns null on error
        /// </summary>
        /// <param name="rdbType"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        BlockRecord GetRandomBlock(DFBlock.RdbTypes rdbType = DFBlock.RdbTypes.Unknown, int count = 0)
        {
            try
            {
                count++;
                var usedIndices = GetUsedIndices();
                if (rdbType == DFBlock.RdbTypes.Start) //get random start block
                {
                    BlockRecord record = blockCollection.GetRandomBlock(blockCollection.GetStartBlocks(blockCollection.blockRecords));
                    if (usedIndices.Contains(record.index))
                    {
                        if (count < 100)
                        {
                            return GetRandomBlock(rdbType, count);
                        }
                        else
                        {
                            Debug.LogWarning("Failed to find unique block");
                            record = new BlockRecord(record.name, record.index, record.hasStartFlags, record.blockType, record.RdBType);
                            return record;
                        }

                    }
                    else
                    {
                        return record;

                    }

                }
                else if (rdbType == DFBlock.RdbTypes.Border) //Get a random border block
                {

                    BlockRecord record = blockCollection.GetRandomBlock(blockCollection.FilterRDBBlocks(blockCollection.blockRecords, rdbType));
                    if (usedIndices.Contains(record.index))
                    {
                        if (count < 100)
                        {
                            return GetRandomBlock(rdbType, count);
                        }
                        else
                        {
                            Debug.LogWarning("Failed to find unique block ");
                            record = new BlockRecord(record.name, record.index, record.hasStartFlags, record.blockType, record.RdBType);
                            return record;
                        }

                    }
                    else
                    {
                        return record;

                    }

                }
                else// Get non-border blocks
                {
                    BlockRecord record = blockCollection.GetRandomBlock(blockCollection.FilterRDBBlocks(blockCollection.blockRecords, DFBlock.RdbTypes.Border, true));
                    if (usedIndices.Contains(record.index))
                    {
                        if (count < 100)
                        {
                            return GetRandomBlock(rdbType, count);
                        }
                        else
                        {
                            Debug.LogWarning("Failed to find unique block");
                            record = new BlockRecord(record.name, record.index, record.hasStartFlags, record.blockType, record.RdBType);
                            return record;
                        }

                    }
                    else
                    {
                        return record;

                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                return null;


            }
        }

        /// <summary>
        /// Creates list of BlockRecords from file or from arena2.  Will create a new file if one isn't
        /// found
        /// </summary>
        /// <returns></returns>
        bool GetBlockList()
        {
            bool getBlockRecordsFromArena2 = true;
            BlockRecordSerializer blockSerializer = new BlockRecordSerializer();

            if (blockSerializer.ReadBlockData(out blockCollection.blockRecords, fileName))
            {
                Debug.Log(string.Format("Read file successfully, record count: {0}", blockCollection.blockRecords.Count));
                
                if(blockCollection.blockRecords.Count != 0)
                {
                    getBlockRecordsFromArena2 = false;
                }
            }
            if (getBlockRecordsFromArena2)
            {
                Debug.Log("Getting blocks from arena2");
                if (!blockSerializer.BuildRecordList(out blockCollection.blockRecords))
                {
                    Debug.LogError("Failed to read in Block Record collection from Arena2.");
                    blockSerializer = null;
                    return false;
                }
                else
                {
                    Debug.Log("Writing new file");
                    blockSerializer.WriteBlockData(blockCollection.FilterBlocks(blockCollection.blockRecords, DFBlock.BlockTypes.Rdb), fileName);
                }
            }
            //filter out RDB blocks
            blockCollection.blockRecords = blockCollection.FilterRDBBlocks(DFBlock.RdbTypes.Mausoleum, true);
            if (filterSpecialBlocks)
            {
                blockCollection.blockRecords = blockCollection.FilterRDBBlocks(DFBlock.RdbTypes.Quest, true);
            }

            if(filterWetBlocks)
            {
                blockCollection.blockRecords = blockCollection.FilterRDBBlocks(DFBlock.RdbTypes.Wet, true);
            }
            blockSerializer = null;
            return true;
        }

        IEnumerator ReadyCheck()
        {
            while (!DaggerfallUnity.Instance.IsReady)
                yield return new WaitForEndOfFrame();


            Debug.Log("Ready");

            StartUp();
            yield break;
        }



    }
}
