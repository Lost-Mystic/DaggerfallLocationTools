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

using System.Collections;
using DaggerfallConnect;



namespace DaggerfallWorkshop.DungeonGenerator
{
    [System.Serializable]
    public class BlockRecord
    {
        public string name                      = "INVALID";
        public int index                        =  -1;
        public bool hasStartFlags               = false;
        public DFBlock.BlockTypes blockType     = DFBlock.BlockTypes.Unknown;
        public DFBlock.RdbTypes RdBType         = DFBlock.RdbTypes.Unknown;

        [System.Xml.Serialization.XmlIgnore]
        public UnityEngine.Vector2 position     = UnityEngine.Vector2.zero;
        [System.Xml.Serialization.XmlIgnore]
        public bool isStartBlock                = false;


        //constructors
        public BlockRecord()
        {

        }

        public BlockRecord(string name, int index, bool hasStartFlags, DFBlock.BlockTypes blockType, DFBlock.RdbTypes rdbtype)
        {
            this.name = name;
            this.index = index;
            this.hasStartFlags = hasStartFlags;
            this.blockType = blockType;
            this.RdBType = rdbtype;
        }


        public override bool Equals(object obj)
        {
            try
            {
                BlockRecord otherRecord = (BlockRecord)obj;
                if (otherRecord == null)
                    return false;
                if (otherRecord.name != this.name)
                    return false;
                if (otherRecord.index != this.index)
                    return false;
                if (otherRecord.hasStartFlags != this.hasStartFlags)
                    return false;
                if (otherRecord.blockType != this.blockType)
                    return false;
                if (otherRecord.RdBType != this.RdBType)
                    return false;

                return true;

            }
            catch
            {
                return false;
            
            }

        }



        public override string ToString()
        {
            return this.name;
        }


    }
}