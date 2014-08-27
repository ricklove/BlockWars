using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

using System.Linq;

public class FileController : MonoBehaviour
{
    public BaseState GetGameBaseState(GameObject baseObject, float maxDistanceFromBase)
    {
        var blockStates = new List<BlockState>();

        // Get all blocks of that base
        foreach (Transform childTransform in baseObject.transform)
        {
            if (childTransform.gameObject.tag == "BaseMarker")
            {
                continue;
            }

            var bObj = childTransform.gameObject;

            // Skip blocks that are too far from the base
            if (bObj.transform.localPosition.magnitude > maxDistanceFromBase)
            {
                continue;
            }

            var bState = new BlockState();
            bState.blockType = BlockState.GetBlockType(bObj.name);
            bState.x = bObj.transform.localPosition.x;
            bState.y = bObj.transform.localPosition.y;
            bState.z = bObj.transform.localPosition.z;
            bState.qx = bObj.transform.localRotation.x;
            bState.qy = bObj.transform.localRotation.y;
            bState.qz = bObj.transform.localRotation.z;
            bState.qw = bObj.transform.localRotation.w;

            blockStates.Add(bState);
        }

        var baseState = new BaseState();
        baseState.blockStates = blockStates.ToArray();

        return baseState;
    }

    public void SetGameBaseState(GameObject[] blockPrefabs, GameObject baseObject, BaseState state, int layerNumber, float maxDistanceFromBase)
    {
        // Remove children blocks of base
        foreach (Transform cTransform in baseObject.transform)
        {
            if (cTransform.gameObject.tag != "BaseMarker")
            {
                Destroy(cTransform.gameObject);
            }
        }

        // Create base
        foreach (var bState in state.blockStates)
        {
            var position = new Vector3(bState.x, bState.y, bState.z);
            var rotation = new Quaternion(bState.qx, bState.qy, bState.qz, bState.qw);

            // Skip blocks that are too far from the base
            if (position.magnitude > maxDistanceFromBase)
            {
                continue;
            }

            // TODO: No need to set position / rotation until it has a parent transform
            var block = CreateBlock(blockPrefabs, bState.blockType, position, rotation, layerNumber);
            block.transform.parent = baseObject.transform;
            block.transform.localPosition = position;
            block.transform.localRotation = rotation;

            block.AddComponent<MirrorObject>();
        }
    }

    private GameObject CreateBlock(GameObject[] blockPrefabs, BlockType type, Vector3 position, Quaternion rotation, int layerNumber)
    {
        GameObject model = blockPrefabs[0];

        // Create block of correct type
        foreach (var bPrefab in blockPrefabs)
        {
            if (bPrefab.name.EndsWith(System.Enum.GetName(typeof(BlockType), type)))
            {
                model = bPrefab;
                break;
            }
        }

        var block = (GameObject)Instantiate(model, position, rotation) as GameObject;

        //Consts.SetLayer(block, layerNumber);
        block.tag = "Block";

        var blockRigidbody = block.GetComponent<Rigidbody>();
        blockRigidbody.constraints = RigidbodyConstraints.None;
        blockRigidbody.isKinematic = false;
        blockRigidbody.drag = 1;

        return block;
    }

    public void SaveBaseState(string name, BaseState state)
    {
        // TODO: Make user base state into a single entry (the serializing/deserializing is a bigger overhead than the size)

        // Serialize to a string
        var dataText = Serialize(state);

        // Save string
        SaveData(name, dataText);

        // Add Data Name
        AddDataName(name);
    }

    public void DeleteBaseState(string name)
    {
        DeleteData(name);
        RemoveDataName(name);
    }

    public BaseState LoadBaseState(string name)
    {
        var dataText = LoadData(name);
        var state = DeSerializeBaseState(dataText);
        return state;
    }

    public string ExportBases()
    {
        var groupData = Serialize(LoadUserBaseGroup());
        return groupData;
    }

    public void ImportBases(string importBaseStateGroupData)
    {
        // TODO: Save a backup before import

        var bGroup = DeSerializeBaseStateGroup(importBaseStateGroupData);

        // Delete the old base states
        foreach (var oldName in GetBaseStateNames())
        {
            DeleteBaseState(oldName);
        }

        for (int i = 0; i < bGroup.baseNames.Length; i++)
        {
            SaveBaseState(bGroup.baseNames[i], bGroup.baseStates[i]);
        }
    }

    public BaseStateGroup LoadUserBaseGroup()
    {
        var names = GetBaseStateNames();
        var baseStates = new List<BaseState>();

        foreach (var baseName in names)
        {
            baseStates.Add(LoadBaseState(baseName));
        }

        var group = new BaseStateGroup();
        group.baseNames = names;
        group.baseStates = baseStates.ToArray();


        return group;
    }

    public BaseStateGroup LoadBaseGroup(string baseStateGroupData)
    {
        var bGroup = DeSerializeBaseStateGroup(baseStateGroupData);
        return bGroup;
    }

    public BaseStateGroup LoadBaseGroupFile(string resourceFileName)
    {
        var resource = Resources.Load(resourceFileName);
        var data = ((TextAsset)resource).text;

        // BUGFIX: There is a random unicode character getting appended to the text
        if (data[0] != '{')
        {
            data = data.Substring(1);
        }

        return LoadBaseGroup(data);
    }

    public string[] GetBaseStateNames()
    {
        // For now that data names file is the bases only
        return LoadDataNames();
    }

    private static string dataNamesName = "BASE_NAMES";
    private static char dataNamesDivider = '$';

    private void SaveDataNames(string[] names)
    {
        StringBuilder text = new StringBuilder();

        foreach (var name in names)
        {
            if (!string.IsNullOrEmpty(name))
            {
                text.Append(dataNamesDivider);
                text.Append(name);
            }
        }

        SaveData(dataNamesName, text.ToString());
    }

    private string[] LoadDataNames()
    {
        var dataNames = LoadData(dataNamesName);
        var names = dataNames.Split(new char[] { dataNamesDivider }, System.StringSplitOptions.RemoveEmptyEntries);
        return names;
    }

    private void AddDataName(string name)
    {
        var names = new List<string>(LoadDataNames());
        if (!names.Contains(name))
        {
            names.Add(name);
            SaveDataNames(names.ToArray());
        }
    }

    private void RemoveDataName(string name)
    {
        var names = new List<string>(LoadDataNames());
        if (names.Contains(name))
        {
            names.Remove(name);
            SaveDataNames(names.ToArray());
        }
    }

    private void SaveData(string name, string data)
    {
        PlayerPrefs.SetString(name, data);
    }

    private void DeleteData(string name)
    {
        PlayerPrefs.DeleteKey(name);
    }

    private string LoadData(string name)
    {
        return PlayerPrefs.GetString(name);
    }

    //private string Serialize<T>(T data)
    //{
    //    return MiniJSON.Json.Serialize(data);
    //    //return fastJSON.JSON.Instance.ToJSON(data);
    //}

    public static void Test_SerializeBaseState()
    {
        var orig = new BaseState()
        {
            blockStates = new BlockState[] {
                new BlockState(){ blockType= BlockType.Hexagon, x=1.5f, y=1.5f, z=1.5f, qx=2.5f, qy=2.5f, qz=2.5f, qw=2.5f},
                new BlockState(){ blockType= BlockType.Square, x=1.5f, y=1.5f, z=1.5f, qx=2.5f, qy=2.5f, qz=2.5f, qw=2.5f},
            }
        };

        var text = Serialize(orig);

        var final = DeSerializeBaseState(text);

        // Verify
        for (int i = 0; i < orig.blockStates.Length; i++)
        {
            if (orig.blockStates[i].blockType != final.blockStates[i].blockType
                || orig.blockStates[i].qx != final.blockStates[i].qx)
            {
                throw new Exception("FAIL");
            }
        }
    }

    public static string Serialize(BaseState data)
    {
        var blocks = data.blockStates;
        var bData = blocks.Select(b => new List<float>() { 
          (float)b.blockType,
          b.x,
          b.y,
          b.z,
          b.qx,
          b.qy,
          b.qz,
          b.qw,
        }).ToList();

        return MiniJSON.Json.Serialize(bData);
    }

    public static BaseState DeSerializeBaseState(string dataText)
    {
        var bData = (List<object>)MiniJSON.Json.Deserialize(dataText);
        var blocks = bData
            .Select(b => ((List<object>)b).Select(bi => Convert.ToSingle(bi)).ToList())
            .Select(b => new BlockState()
        {
            blockType = (BlockType)b[0],
            x = b[1],
            y = b[2],
            z = b[3],
            qx = b[4],
            qy = b[5],
            qz = b[6],
            qw = b[7],
        }).ToArray();

        return new BaseState() { blockStates = blocks };
    }

    public static void Test_SerializeBaseStateGroup()
    {
        var orig = new BaseStateGroup()
        {
            baseNames = new string[] { "BaseA", "BaseB" },
            baseStates = new BaseState[] { 
                new BaseState()
                {
                    blockStates = new BlockState[] {
                        new BlockState(){ blockType= BlockType.Hexagon, x=1.5f, y=1.5f, z=1.5f, qx=2.5f, qy=2.5f, qz=2.5f, qw=2.5f},
                        new BlockState(){ blockType= BlockType.Square, x=1.5f, y=1.5f, z=1.5f, qx=2.5f, qy=2.5f, qz=2.5f, qw=2.5f},
                    }
                },
                new BaseState()
                {
                    blockStates = new BlockState[] {
                        new BlockState(){ blockType= BlockType.Rhombus, x=1.5f, y=1.5f, z=1.5f, qx=2.5f, qy=2.5f, qz=2.5f, qw=2.5f},
                        new BlockState(){ blockType= BlockType.Trapezoid, x=1.5f, y=1.5f, z=1.5f, qx=2.5f, qy=2.5f, qz=2.5f, qw=2.5f},
                    }
                }
            }
        };

        var text = Serialize(orig);

        var final = DeSerializeBaseStateGroup(text);

        // Verify
        for (int j = 0; j < orig.baseStates.Length; j++)
        {
            if (orig.baseNames[j] != final.baseNames[j])
            {
                throw new Exception("FAIL");
            }

            for (int i = 0; i < orig.baseStates[j].blockStates.Length; i++)
            {
                if (orig.baseStates[j].blockStates[i].blockType != final.baseStates[j].blockStates[i].blockType
                    || orig.baseStates[j].blockStates[i].qx != final.baseStates[j].blockStates[i].qx)
                {
                    throw new Exception("FAIL");
                }
            }
        }
    }

    public static void Test_SerializeBaseStateGroup2()
    {
        var text = @"
[[""Boss A"",""Boss B""],
[


""[

[2, 0,0,0, 0,0,0,0],
[2, 0,1,0, 0,0,0,0],
[2, 0,2,0, 0,0,0,0],
[2, 0,3,0, 0,0,0,0],
[2, 0,4,0, 0,0,0,0],
[2, 0,5,0, 0,0,0,0],
[2, 0,6,0, 0,0,0,0],
[2, 0,7,0, 0,0,0,0],
[2, 0,8,0, 0,0,0,0],
[2, 0,9,0, 0,0,0,0],

]"",


""[

[2, 0,0,0, 0,0,0,0],
[2, 0,1,0, 0,0,0,0],
[2, 0,2,0, 0,0,0,0],
[2, 0,3,0, 0,0,0,0],
[2, 0,4,0, 0,0,0,0],
[2, 0,5,0, 0,0,0,0],
[2, 0,6,0, 0,0,0,0],
[2, 0,7,0, 0,0,0,0],
[2, 0,8,0, 0,0,0,0],
[2, 0,9,0, 0,0,0,0],

]""



]
]
";

        var final = DeSerializeBaseStateGroup(text);
    }

    public static string Serialize(BaseStateGroup data)
    {
        var namesStr = data.baseNames.ToList();
        var statesStr = data.baseStates.Select(bs => Serialize(bs)).ToList();

        var all = new List<List<string>>() { namesStr, statesStr };

        return MiniJSON.Json.Serialize(all);
    }

    public static BaseStateGroup DeSerializeBaseStateGroup(string dataText)
    {
        try
        {
            dataText = dataText.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "");

            var obj = (List<object>)MiniJSON.Json.Deserialize(dataText);
            var all = obj.Select(o => ((List<object>)o).Select(o2 => (string)o2)).ToList();

            var namesStr = all[0];
            var statesStr = all[1];

            var states = statesStr.Select(s => DeSerializeBaseState(s)).ToList();

            return new BaseStateGroup() { baseNames = namesStr.ToArray(), baseStates = states.ToArray() };
        }
        catch
        {
            return new BaseStateGroup() { baseNames = new string[0], baseStates = new BaseState[0] };
        }
    }

}
