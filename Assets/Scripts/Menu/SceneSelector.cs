using Unity.VisualScripting;
using UnityEngine;

public class SceneSelector : MonoBehaviour
{
    [SerializeField] GameObject singleScene;
    [SerializeField] GameObject multiScene;


    public void onSingle()
    {
        singleScene.SetActive(true);
        gameObject.SetActive(false);

    }

    public void onMulti()
    {
        multiScene.SetActive(true);
        gameObject.SetActive(false);
    }
}
