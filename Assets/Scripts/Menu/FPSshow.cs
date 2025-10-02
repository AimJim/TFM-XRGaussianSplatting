using System.Collections;
using TMPro;
using UnityEngine;

public class FPSshow : MonoBehaviour
{
    TextMeshProUGUI textGui;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textGui = GetComponent<TextMeshProUGUI>();
        StartCoroutine(UpdateText());
    }

    IEnumerator UpdateText()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        textGui.text = ((int)(1 / Time.deltaTime)).ToString();
        StartCoroutine(UpdateText());
    }
    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
