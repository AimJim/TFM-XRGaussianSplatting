using UnityEngine;

public class PlaceFloor : MonoBehaviour
{
    [SerializeField] Transform cam;

    public void SetHeight(float height)
    {
        transform.position = Vector3.up * height;
        cam.position = Vector3.up * height;
    }
}
