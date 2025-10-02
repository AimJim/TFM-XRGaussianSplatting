using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InteractableManager : MonoBehaviour
{
    [SerializeField] Transform[] spawnPos;

    [SerializeField] GameObject gaussianPrefab;

    [SerializeField] bool debug = false;
    [SerializeField] string[] debugGaussians;

    int count = 0;

    void Awake()
    {
        List<string> paths = new List<string>();
        if (debug)
        {
            paths = debugGaussians.ToList();
        }
        else
        {
            MultipleSceneInfo msi = GameObject.FindAnyObjectByType<MultipleSceneInfo>();

            paths = msi.GetFilePaths();
        }
        

        foreach (string file in paths)
        {
            GameObject go = Instantiate(gaussianPrefab);
            go.transform.position = spawnPos[count].position;
            go.transform.rotation = spawnPos[count].rotation;
            

            go.GetComponentInChildren<GaussianMove>().SetPath(file);
            go.GetComponentInChildren<GaussianMove>().Init();

            count = (count + 1) % spawnPos.Length;
            
        }
    }
}
