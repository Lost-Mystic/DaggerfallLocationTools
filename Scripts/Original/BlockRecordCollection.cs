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

using System.Collections.Generic;
using System.Xml.Serialization;
using DaggerfallConnect;
using System.Linq;


namespace DaggerfallWorkshop.DungeonGenerator
{
    [System.Serializable]
    public class BlockRecordCollection
    {
        public List<BlockRecord> blockRecords;

        //default constructor
        public BlockRecordCollection()
        {
            blockRecords = new List<BlockRecord>();
        }


        /// <summary>
        /// Filter blocks by block type
        /// </summary>
        /// <param name="blockType"></param>
        /// <returns></returns>
        public List<BlockRecord> FilterBlocks(DFBlock.BlockTypes blockType, bool reverseFilter = false)
        {
            return this.blockRecords.Where(block => (block.blockType == blockType) != reverseFilter).ToList<BlockRecord>();
        }

        public List<BlockRecord> FilterBlocks(List<BlockRecord> blockRecords, DFBlock.BlockTypes blockType, bool reverseFilter = false)
        {
            return blockRecords.Where(block => (block.blockType == blockType) != reverseFilter).ToList<BlockRecord>();
        }


        /// <summary>
        /// Filter blocks by RdBType - only returns RDB block type records.
        /// </summary>
        /// <param name="rdbType"></param>
        /// <returns></returns>
        public List<BlockRecord> FilterRDBBlocks(DFBlock.RdbTypes rdbType, bool reverseFilter = false)
        {
            return this.blockRecords.Where(block => block.blockType == DFBlock.BlockTypes.Rdb && ((block.RdBType == rdbType) != reverseFilter)).ToList<BlockRecord>();
        }

        public List<BlockRecord> FilterRDBBlocks(List<BlockRecord> blockRecords, DFBlock.RdbTypes rdbType, bool reverseFilter = false)
        {
            return this.blockRecords.Where(block => block.blockType == DFBlock.BlockTypes.Rdb && ((block.RdBType == rdbType) != reverseFilter)).ToList<BlockRecord>();
        }

        /// <summary>
        /// Return all blocks with start flags
        /// </summary>
        /// <returns></returns>
        public List<BlockRecord> GetStartBlocks(bool hasStartFlags = true)
        {
            return this.blockRecords.Where(block => block.hasStartFlags == hasStartFlags).ToList<BlockRecord>();
        }

        public List<BlockRecord> GetStartBlocks(List<BlockRecord> blockRecords, bool hasStartFlags = true)
        {
            return blockRecords.Where(block => block.hasStartFlags == hasStartFlags).ToList<BlockRecord>();
        }


        public BlockRecord GetRandomBlock()
        {
            int select = UnityEngine.Random.Range(0, blockRecords.Count);
            return blockRecords[select];
        }

        public BlockRecord GetRandomBlock(List<BlockRecord> blockRecords)
        {
            int select = UnityEngine.Random.Range(0, blockRecords.Count);
            return blockRecords[select];
        }



    }
}




