using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadButton : MonoBehaviour
{
    [SerializeField] string nextScene;
    [SerializeField] SceneInfo si;

    public void OnClick()
    {
        if (si.GetFilePath().Contains(".ply"))
        {
           SceneManager.LoadScene(nextScene); 
        }
        
    }
}
