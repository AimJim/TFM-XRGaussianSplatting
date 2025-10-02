using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] InputActionAsset ias;

    InputAction moveI;
    InputAction rotateI;

    Vector3 move;
    Vector2 rotate;

    void Awake()
    {
        ias.Enable();
        moveI = ias.FindAction("Move");
        rotateI = ias.FindAction("Rotate");
    }

    // Update is called once per frame
    void Update()
    {
        move = moveI.ReadValue<Vector3>();
        rotate = rotateI.ReadValue<Vector2>();
        transform.position += speed *Time.deltaTime * (transform.forward * move.z + transform.up * move.y + transform.right * move.x);
        transform.rotation *= Quaternion.Euler( (Vector3.up * rotate.x - Vector3.right * -rotate.y) * 180*Time.deltaTime);
    }
}
