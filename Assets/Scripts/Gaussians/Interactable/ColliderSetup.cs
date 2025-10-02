using UnityEngine;

public class ColliderSetup : MonoBehaviour
{
    [SerializeField] BoxCollider col;

    public void SetupCollider(Vector3 maxValues, Vector3 minValues)
    {

        Vector3 size = new Vector3(Mathf.Abs(maxValues.x - minValues.x), Mathf.Abs(maxValues.y - minValues.y), Mathf.Abs(maxValues.z - minValues.z));
        Vector3 center = new Vector3((maxValues.x + minValues.x) / 2, (maxValues.y + minValues.y) / 2, (maxValues.z + minValues.z) / 2);
        col.size = size;
        col.center = center;
    }
}
