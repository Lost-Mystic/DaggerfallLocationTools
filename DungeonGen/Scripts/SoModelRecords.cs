using System.Collections;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect.Arena2;
using DaggerfallConnect;
using DaggerfallWorkshop;

public class SoModelRecords : ScriptableObject
{
    public List<DfModelRecord> record;
    public List<string> Labels; // List of labels being used
    Arch3dFile arch3dFile;

    public int LastClickedIndex = 0;



    void InitArchFile()
    {
        if (arch3dFile == null)
            arch3dFile = new Arch3dFile(Path.Combine(DaggerfallUnity.Instance.Arena2Path, Arch3dFile.Filename), FileUsage.UseMemory, true); 
    }


    /// <summary>
    /// Adds a model info to the record if it's not already there.
    /// </summary>
    /// <param name="ModelId"></param>
    /// <returns></returns>
    public bool AddRecordById(int ModelId)
    {
        //if (ModelId > 65500) return false;

        InitArchFile();
        uint uModelId = (uint)ModelId;

        // Test if model is actually in BSA database and has a mesh.  If not skip
        int index = arch3dFile.GetRecordIndex(uModelId);
        if (index == -1)
        {
            Debug.Log("ModelId " + ModelId.ToString() + " not found in Daggerfall database");
            return false;
        }



        // Go reverse order down, and add successfully if you've found nothing until the point you find it's model-1 number in the index
        // Counts the least records this way

        // eg 1000 records, ask for ModelID 20000 assuming highest is 5000, then add
        // ModelID can never be lower than records

        // Check to ensure ID doesn't already exist.

        //if (record.Count <= ModelId)
            for (int i = record.Count-1; i >= 0; i--)
            {
                if (record[i].ModelID >= ModelId)
                {
                    return false;
                } else
                if (record[i].ModelID < ModelId)
                {
                break;  // Smaller than the highest model number.  Perhaps you missed it?
                }
            }
        /*
        foreach (DfModelRecord df in record)
        {
            if (df.ModelID == ModelId)
            {
                return false;
            }


        }
        */
        DfModelRecord dfMod = new DfModelRecord();
        dfMod.ModelID = ModelId;
        dfMod.Labels = new List<string>();
        record.Add(dfMod);

        return true;
    }

    /// <summary>
    /// Adds a model info to the record if it's not already there.
    /// </summary>
    /// <param name="ModelId"></param>
    /// <returns></returns>
    public void AddAllRecords()
    {
        int MaxModelID = 120000;
        record = new List<DfModelRecord>();
        int ModelId = 0;
        uint uModelId;

        InitArchFile();

        while (ModelId < MaxModelID)
        {
            uModelId = (uint)ModelId;

            // Test if model is actually in BSA database and has a mesh.  If not skip
            int index = arch3dFile.GetRecordIndex(uModelId);
            if (index != -1)
            {
                DfModelRecord dfMod = new DfModelRecord();
                dfMod.ModelID = ModelId;
                dfMod.Labels = new List<string>();
                record.Add(dfMod);
            }
            ModelId++;
            if (ModelId % 1000 == 0)
                Debug.Log("Imported Records: " + ModelId.ToString());
        }

    }


    public bool TryToAddLabel(string labelToAdd, List<DfModelRecord> modelRecords)
    {
        labelToAdd = labelToAdd.ToLower();
        bool SuccessAddingLabel = false;

        for (int i = 0; i < modelRecords.Count; i++ )
        {
            if (modelRecords[i].Labels.Contains(labelToAdd))
                continue;

            modelRecords[i].AddLabel(labelToAdd);

            if (!Labels.Contains(labelToAdd))
                Labels.Add(labelToAdd);

            SuccessAddingLabel = true;
        }

        return SuccessAddingLabel;
    }

    public bool TryToRemoveLabel(string labelToRemove, List<DfModelRecord> modelRecords)
    {
        labelToRemove = labelToRemove.ToLower();
        bool SuccessRemovingLabel = false;

        for (int i = 0; i < modelRecords.Count; i++)
        {
            Debug.Log("Count " + GetOnlyRecordsByLabels(labelToRemove).Count.ToString());
            if (GetOnlyRecordsByLabels(labelToRemove).Count <= 0)
            {

                Labels.Remove(labelToRemove);
            }

            modelRecords[i].RemoveLabel(labelToRemove);



            SuccessRemovingLabel = true;
        }

        return SuccessRemovingLabel;
    }


    public void GetAllCurrentLabels()
    {
        Labels = new List<string>();

        foreach (DfModelRecord d in record)
        {
            foreach (string s in d.Labels)
            {
                if (Labels.Contains(s) == false)
                {
                    Labels.Add(s);
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id">The model number ID in the daggerfall database</param>
    /// <returns>Null if no record, or a link to the record ID </returns>
    public DfModelRecord GetRecordById(int id)
    {
        foreach (DfModelRecord r in record)
        {
            if (r.ModelID == id)
                return r;
        }
        return null;
    }

    /// <summary>
    /// Returns the index of the array/list based on the ID of the model
    /// </summary>
    /// <param name="Model_Id">ModelId of the asset</param>
    /// <returns></returns>
    public int GetIndexById(int Model_Id)
    {
        for (int i = 0; i < record.Count; i++)
        {
            if (record[i].ModelID == Model_Id)
                return i;
        }
        return -1;
    }


    public List<DfModelRecord> GetOnlyRecordsByLabels(string label)
    {
        string[] str = new string[1];

        str[0] = label;

        return GetOnlyRecordsByLabels(str);
    }


        /// <summary>
        /// Returns the sorted list of models with those labels or the entire list if no selected labels.
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public List<DfModelRecord> GetOnlyRecordsByLabels(string[] labels)
    {
        List<DfModelRecord> sortedRecords = new List<DfModelRecord>();
        string TestString;

        // Make all strings lower case
        for (int i = 0; i < labels.Length; i++ )
        {
            labels[i] = labels[i].ToLower();
        }

        // Iterate through each record
        foreach (DfModelRecord r in record)
        {
            int RecordMatchCount = 0;

            // Iterate through each label inside a record
            foreach (string s in r.Labels)
            {
                foreach (string label in labels)
                {
                    TestString = s.ToLower();
                    if (s.Equals(TestString))
                    {
                        RecordMatchCount++;
                    }
                }
            }
            // If all records matched add to sorted records.
            if (RecordMatchCount >= r.Labels.Count)
            {
                sortedRecords.Add(r);
            }
        }

            if (sortedRecords.Count <= 0)
            sortedRecords = null;

        return sortedRecords;
    }

}
