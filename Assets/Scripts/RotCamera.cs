using UnityEngine;

public class RotCamera : MonoBehaviour
{
    public float rotSpeed;
    // Update is called once per frame
    void Update()
    {
        transform.rotation *= Quaternion.Euler(Vector3.up * rotSpeed * Time.deltaTime);
    }
}
