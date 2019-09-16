using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.DungeonGenerator;
using DaggerfallWorkshop.Utility.AssetInjection;

public class SpawnDfModel : MonoBehaviour
{

    public int ModelId;
    
    public bool SpawnModelNow = false;

    private static List<int> defaultTextures = new List<int>();

    public static void SetDefaultTextures(int[] textureKeys)
    {
        if (textureKeys == null)
        {
            defaultTextures.Clear();
            return;
        }

        defaultTextures.AddRange(textureKeys);
    }


    public static void SpawnModel(int Model_Id)
    {
        Transform parent = null;
        
        int[] textureTable = new int[] { 119, 120, 122, 123, 124, 168 };
        
        uint _ModelId = (uint)Model_Id;

        DaggerfallUnity dfUnity = DaggerfallUnity.Instance;

        DFBlock.RdbObject obj = new DFBlock.RdbObject();

        Matrix4x4 modelMatrix = GetModelMatrix(obj);

        // Get model data
        DaggerfallWorkshop.ModelData modelData = new ModelData();
        dfUnity.MeshReader.GetModelData(_ModelId, out modelData);

        GameObject standaloneObject;

        standaloneObject = AddStandaloneModel(dfUnity, ref modelData, modelMatrix, parent, false);
        standaloneObject.GetComponent<DaggerfallMesh>().SetDungeonTextures(textureTable);

    }
    /*
    private void PrepMesh()
    {
        int[] textureTable = new int[] { 119, 120, 122, 123, 124, 168 };
        uint _ModelId = (uint)ModelId;

        DaggerfallUnity dfUnity = DaggerfallUnity.Instance;

        DFBlock.RdbObject obj = new DFBlock.RdbObject();

        Matrix4x4 modelMatrix = GetModelMatrix(obj);

        // Get model data
        DaggerfallWorkshop.ModelData modelData = new ModelData();
        dfUnity.MeshReader.GetModelData(_ModelId, out modelData);
    }
    */

    public static BareDaggerfallMeshStats GetBareMeshFromId(int ModelID)
    {
        BareDaggerfallMeshStats MeshDat = new BareDaggerfallMeshStats();

        // ---------------
        int[] textureTable = new int[] { 119, 120, 122, 123, 124, 168 };
        uint _ModelId = (uint)ModelID;

        DaggerfallUnity dfUnity = DaggerfallUnity.Instance;

        DFBlock.RdbObject obj = new DFBlock.RdbObject();

        Matrix4x4 modelMatrix = GetModelMatrix(obj);

        // Get model data
        DaggerfallWorkshop.ModelData modelData = new ModelData();
        dfUnity.MeshReader.GetModelData(_ModelId, out modelData);
        // ---------------


        // DIVERT MESH FUNCTIONALITY HERE
        // Maybe model ID, or makestatic
        CachedMaterial[] cachedMaterials;
        int[] textureKeys;
        //textureKeys = DungeonTextureTables.DefaultTextureTable;
        bool hasAnimations;

        MeshDat.mesh = dfUnity.MeshReader.GetMesh(
        dfUnity,
        (uint)ModelID,
        out cachedMaterials,
        out textureKeys,
        out hasAnimations,
        dfUnity.MeshReader.AddMeshTangents,
        dfUnity.MeshReader.AddMeshLightmapUVs);

        MeshDat.matrix = modelMatrix;
        MeshDat.SubMeshIndex = modelData.SubMeshes.Length;      // May be only drawing one submesh

        MeshDat.mat = new Material[cachedMaterials.Length];
        MeshDat.mProp = new MaterialPropertyBlock();
        for (int i = 0; i < cachedMaterials.Length; i++) {
            MeshDat.mProp.SetTexture(i, cachedMaterials[i].material.mainTexture);
            MeshDat.mat[i] = cachedMaterials[i].material;
        }
        //meshRenderer.sharedMaterials = GetMaterialArray(cachedMaterials);
        //dfMesh.SetDefaultTextures(textureKeys);
        

        //SetDefaultTextures(textureKeys);
        //MeshDat.mat = SetBaseMeshTextures(textureTable);

        return MeshDat;
    }


    public static Material[] SetBaseMeshTextures(int[] textureKeys)
    {

        Material[] materials = new Material[1];

        if (textureKeys.Length == 0)
        {
            Debug.Log("SetBaseMeshTextures - Texturekeys length is zero");
            return materials;
        }

        DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
        if (!dfUnity.IsReady)
        {
            Debug.Log("SetBaseMeshTextures - dfUnity isn't ready");
            return materials;
        }

        // Get new material array
        int archive, record, frame;
        materials = new Material[defaultTextures.Count];

        int climateIndex = 0;

        for (int i = 0; i < defaultTextures.Count; i++)
        {
            MaterialReader.ReverseTextureKey(defaultTextures[i], out archive, out record, out frame);
            switch (archive)
            {
                case 119:
                    archive = textureKeys[0];
                    break;
                case 120:
                    archive = textureKeys[1];
                    break;
                case 122:
                    archive = textureKeys[2];
                    break;
                case 123:
                    archive = textureKeys[3];
                    break;
                case 124:
                    archive = textureKeys[4];
                    break;
                case 168:
                    archive = textureKeys[5];
                    break;
                case 74:
                    archive += climateIndex;
                    break;
            }
            materials[i] = dfUnity.MaterialReader.GetMaterial(archive, record);
        }

        return materials;

    }




    // Update is called once per frame
    void Update()
    {

        if (SpawnModelNow)
        {
            SpawnModel(ModelId);
            SpawnModelNow = false;
        }
        
    }

    /// <summary>
    /// Extracts correct matrix from model data.
    /// </summary>
    private static Matrix4x4 GetModelMatrix(DFBlock.RdbObject obj)
    {
        // Get rotation angle for each axis
        float degreesX = -obj.Resources.ModelResource.XRotation / BlocksFile.RotationDivisor;
        float degreesY = -obj.Resources.ModelResource.YRotation / BlocksFile.RotationDivisor;
        float degreesZ = -obj.Resources.ModelResource.ZRotation / BlocksFile.RotationDivisor;

        // Calcuate transform
        Vector3 position = new Vector3(obj.XPos, -obj.YPos, obj.ZPos) * MeshReader.GlobalScale;

        // Calculate matrix
        Vector3 rx = new Vector3(degreesX, 0, 0);
        Vector3 ry = new Vector3(0, degreesY, 0);
        Vector3 rz = new Vector3(0, 0, degreesZ);
        Matrix4x4 modelMatrix = Matrix4x4.identity;
        modelMatrix *= Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
        modelMatrix *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(rz), Vector3.one);
        modelMatrix *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(rx), Vector3.one);
        modelMatrix *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(ry), Vector3.one);

        return modelMatrix;
    }


    /// <summary>
    /// Add a standalone model when not combining.
    /// </summary>
    private static GameObject AddStandaloneModel(
        DaggerfallUnity dfUnity,
        ref ModelData modelData,
        Matrix4x4 matrix,
        Transform parent,
        bool overrideStatic = false,
        bool ignoreCollider = false)
    {
        // Determine static flag
        bool makeStatic = (dfUnity.Option_SetStaticFlags && !overrideStatic) ? true : false;

        // Add GameObject
        uint modelID = (uint)modelData.DFMesh.ObjectId;
        GameObject go = GameObjectHelper.CreateDaggerfallMeshGameObject(modelID, parent, makeStatic, null, ignoreCollider);
        go.transform.position = matrix.GetColumn(3);
        go.transform.rotation = GameObjectHelper.QuaternionFromMatrix(matrix);

        return go;
    }



}
