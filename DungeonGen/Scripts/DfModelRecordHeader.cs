using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.DungeonGenerator;

public class DfModelRecordHeader : MonoBehaviour
{
    public int ModelID;
    public DfModelRecord modelRecord;
    public DungeonGenerator dg;

    // List of labels from the larger records.
    public List<string> Labels;

    private void OnValidate()
    {
        // If you type something in and press the button or press ENTER it will then be updated.
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
