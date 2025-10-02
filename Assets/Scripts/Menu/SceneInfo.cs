using UnityEngine;

public class SceneInfo : MonoBehaviour
{
    string filePath = "";
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void SetFilePath(string newFilePath)
    {
        this.filePath = newFilePath;
    }

    virtual public string GetFilePath()
    {
        return filePath;
    }
}
