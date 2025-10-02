using System.Collections.Generic;
using UnityEngine;

public class MultipleSceneInfo : SceneInfo
{
    List<string> paths = new List<string>();

    // void Awake()
    // {
    //     DontDestroyOnLoad(gameObject);
    // }
    public void AddFilePath(string newFilePath)
    {
        paths.Add(newFilePath);
    }

    public List<string> GetFilePaths()
    {
        return paths;
    }

    public override string GetFilePath()
    {
        if (paths.Count > 0)
        {
            return paths[0];
        }
        return "";
        
    }
}
