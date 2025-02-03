using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SceneGroupData
{
    public string groupName;
    public List<string> scenePaths = new List<string>();
}

public class SceneGroupDatabase : ScriptableObject
{
    public List<SceneGroupData> groups = new List<SceneGroupData>();
}