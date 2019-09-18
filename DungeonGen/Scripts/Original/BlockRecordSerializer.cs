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
using System.Xml.Serialization;
using DaggerfallConnect;
using System.IO;
using System;
using System.Linq;

namespace DaggerfallWorkshop.DungeonGenerator
{
    public class BlockRecordSerializer
    {
        DaggerfallUnity dfUnity;
        XmlSerializer serializer;
        FileStream stream;

        public string defaultFileName = "BlockRecordExport.xml";
        public BlockRecordCollection recordCollection;                  //only for testing
        //public bool runTest = true;                                     //set to true to run test on start




        /// <summary>
        /// These RDB blocks (plus any blocks that start w/ B) don't have start markers (Thanks InterKarma!)
        /// </summary>
        internal string[] startMarkerFilter = new string[]
        {
            "S0000021.RDB",
            "S0000022.RDB",
            "S0000061.RDB",
            "S0000069.RDB",
            "S0000140.RDB"
        };




        // Use this for initialization
        void Start()
        {
            recordCollection = new BlockRecordCollection();
            //DaggerfallUnity.Instance = DaggerfallUnity.Instance;
            //if (runTest)
                //StartCoroutine(Test());

        }

        void OnDestroy()
        {
            recordCollection = null;
            serializer = null;
            stream = null;
        }

        /// <summary>
        /// Builds BlockRecordCollection using ContentReader.BlockFileReader
        /// Gets all RDB & RMB blocks
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public bool BuildRecordList(out List<BlockRecord> collection)
        {
            Debug.Log("BuildBLockList()");
            collection = new List<BlockRecord>();

            for (int i = 0; i < DaggerfallUnity.Instance.ContentReader.BlockFileReader.Count; i++)
            {
                BlockRecord record;
                try
                {
                    CreateBlockRecordFromDFBlock(out record, i);
                    if (record.blockType == DFBlock.BlockTypes.Rdi || record.blockType == DFBlock.BlockTypes.Unknown)
                        continue;
                  

                    collection.Add(record);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                    return false;
                }


            }
            return true;

        }

        public bool BuildRecordList(out BlockRecordCollection collection)
        {
            collection = new BlockRecordCollection();
            return BuildRecordList(out collection.blockRecords);

        }

        /// <summary>
        /// Grabs the relevant data from a DFBlock struct and creates a BlockRecord
        /// </summary>
        /// <param name="recordOut"></param>
        /// <param name="blockData"></param>
        public void CreateBlockRecordFromDFBlock(out BlockRecord recordOut, DFBlock blockData)
        {
            DFBlock.RdbTypes RdBType = DaggerfallUnity.Instance.ContentReader.BlockFileReader.GetRdbType(blockData.Name);
            
            bool hasStartMarkers = false;
            if (blockData.Type == DFBlock.BlockTypes.Rdb && !startMarkerFilter.Contains(blockData.Name) && !blockData.Name.StartsWith("B"))
                hasStartMarkers = true;
            recordOut = new BlockRecord(blockData.Name, blockData.Index, hasStartMarkers, blockData.Type, RdBType);
        }

        public void CreateBlockRecordFromDFBlock(out BlockRecord recordOut, int blockIndex)
        {
            DFBlock block = DaggerfallUnity.Instance.ContentReader.BlockFileReader.GetBlock(blockIndex);
            
            CreateBlockRecordFromDFBlock(out recordOut, block);
        }


        /// <summary>
        /// Writes Block record list to xml file
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public bool WriteBlockData(List<BlockRecord> collection, string fileName = "BlockRecordExport.xml", FileMode writeMode = FileMode.CreateNew)
        {
            Debug.Log("WriteBlockData()");
            if (collection == null)
            {
                Debug.LogError("BlockRecordCollection null in WriteBlockData; stopping");
                return false;
            }

            try
            {
                serializer = new XmlSerializer(typeof(List<BlockRecord>));
                if (string.IsNullOrEmpty(fileName))
                    fileName = defaultFileName;

                stream = new FileStream(Path.Combine(Application.dataPath, fileName), writeMode, FileAccess.Write);
                serializer.Serialize(stream, collection);
                stream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;

            }
            return true;


        }

        public bool WriteBlockData(BlockRecordCollection collection, string fileName = "BlockRecordExport.xml", FileMode writeMode = FileMode.CreateNew)
        {
            try
            {
                return WriteBlockData(collection.blockRecords, fileName, writeMode);
            }
            catch (Exception ex)
            {

                Debug.LogError(ex.Message);
                return false;
            }


        }


        /// <summary>
        /// Loads file w/ name of FileName at Application.dataPath and Deserialize it
        /// to List<BlockRecord>
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public bool ReadBlockData(out List<BlockRecord> collection, string fileName = "BlockRecordExport.xml", FileMode readMode = FileMode.Open)
        {
            try
            {
                collection = new List<BlockRecord>();
                serializer = new XmlSerializer(typeof(List<BlockRecord>));
                if (string.IsNullOrEmpty(fileName))
                {
                    Debug.LogWarning("Null or Empty filname; reverting to default");
                    fileName = defaultFileName;
                }

                if (!File.Exists(Path.Combine(Application.dataPath, fileName)))
                {
                    Debug.LogError("File not found!");
                    return false;
                }
                stream = new FileStream(Path.Combine(Application.dataPath, fileName), readMode, FileAccess.Read);
                collection = (List<BlockRecord>)serializer.Deserialize(stream);
                stream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                collection = null;
                stream.Close();
                return false;
            }

            return true;

        }

        public bool ReadBlockData(out BlockRecordCollection collection, string fileName, FileMode readMode = FileMode.Open)
        {
            collection = new BlockRecordCollection();

            try
            {
                ReadBlockData(out collection.blockRecords, fileName, readMode);

            }
            catch (Exception ex)
            {

                Debug.LogError(ex.Message);
                return false;
            }

            return true;

        }



        /// <summary>
        /// Test function.  Builds list of block records, writes to XML & reads it back in.
        /// </summary>
        /// <returns></returns>
        IEnumerator Test()
        {
            Debug.Log("Waiting on dfUnity...");
            while (!dfUnity.IsReady)
                yield return new WaitForEndOfFrame();

            Debug.Log("Finished waiting");
            bool check = false;
            recordCollection = new BlockRecordCollection();
            check = BuildRecordList(out recordCollection.blockRecords);
            Debug.Log("BuildRecordList returned: " + check);
            if (check)
            {
                check = WriteBlockData(
                    recordCollection.FilterBlocks(DFBlock.BlockTypes.Rdb),
                    defaultFileName, FileMode.Create);

                Debug.Log("WriteBlockData returned: " + check);

            }
            if (check)
            {

                check = ReadBlockData(out recordCollection.blockRecords, defaultFileName);
                Debug.Log("ReadBlockData returned: " + check);
            }

            Debug.Log("Finished Test");
            yield break;

        }


    }
}
