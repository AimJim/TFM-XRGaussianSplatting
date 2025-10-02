using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Android;

public class MultipleFileSelector : MonoBehaviour
{

    List<string> path;
    List<string> possiblePath = new List<string>();

    [SerializeField]
    TextMeshProUGUI currentPathShow;

    [SerializeField]
    TextMeshProUGUI selectedPath;

    [SerializeField]
    MultipleSceneInfo msi;
    string nextDirectory = "";
    int count = 0;
    string currentPath;

    void Start()
    {
        string basePath = "";
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            
        }
        
        
        //Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        #if UNITY_EDITOR
            basePath = Application.dataPath;
            
        #else
            
            basePath = Application.persistentDataPath;
            
        #endif
        currentPathShow.text = basePath;

        path = basePath.Split("/").ToList<string>();
        basePath += "/";
        ReadPath(basePath);
        currentPath = basePath;
    }
    void ReadPath(string basePath)
    {
        possiblePath.Clear();

        var info = new DirectoryInfo(basePath);
        if (info.Extension != "")
        {
            return;
        }
        var dirInfo = info.GetDirectories();
        
        var fileInfo = info.GetFiles();
        foreach (var dir in dirInfo)
        {
            possiblePath.Add(dir.Name);

        }
        foreach (var file in fileInfo)
        {

            if (file.Extension == ".ply")
            {
                possiblePath.Add(file.Name);
            }

        }
        if (possiblePath.Count == 0)
        {
            possiblePath.Add("");
        }
        
    }


    public void NextFile()
    {
        nextDirectory = possiblePath[count];
        count = (count + 1) % possiblePath.Count;
        currentPathShow.text = currentPath + nextDirectory;
    }

    public void PrevFile()
    {
        nextDirectory = possiblePath[count];
        count = count - 1;
        if (count < 0)
        {
            count = possiblePath.Count - 1;
        }
        currentPathShow.text = currentPath + nextDirectory;
    }

    void ResetLocalDir()
    {
        nextDirectory = "";
        currentPath = "";
        
        foreach (string i in path)
        {
            currentPath = currentPath + i + "/";
        }
        currentPathShow.text = currentPath;
        count = 0;
        ReadPath(currentPath);
    }
    public void EnterFolder()
    {
        if (nextDirectory == "")
        {
            return;
        }
        path.Add(nextDirectory);
        ResetLocalDir();
    }

    public void PrevFolder()
    {
        path.RemoveAt(path.Count - 1);
        ResetLocalDir();
    }

    public void SelectPath()
    {
        selectedPath.text += " \n" + nextDirectory;
        
        msi.AddFilePath(currentPath + nextDirectory);
    }
}
