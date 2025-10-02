using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using Unity.VisualScripting;

struct CameraPos {
    public Vector3 position;
    public Quaternion rotation;
}
public class CameraFollow : MonoBehaviour
{
    List<CameraPos> cameraList = new List<CameraPos>();
    void Start()
    {
        //C:/Users/111370/Documents/Plys/75989198-0 2/75989198-0/cameras.json
        //C:/Users/111370/Documents/Plys/588c70b9-5/588c70b9-5/cameras.json
        //C:/Users/111370/Documents/Plys/f121d198-0/cameras.json
        StreamReader sr = new StreamReader("C:/Users/111370/Documents/Plys/f121d198-0/cameras.json");
        string fileText = sr.ReadToEnd();
        sr.Close();
        JArray array = JArray.Parse(fileText);
        foreach (JObject obj in array.Children<JObject>())
        {
            CameraPos camPos = new CameraPos();
    
            foreach (JProperty singleProp in obj.Properties())
            {

                switch (singleProp.Name)
                {
                    case "position":
                        
                        camPos.position = new Vector3((float)singleProp.Value[0], (float)singleProp.Value[1], (float)singleProp.Value[2]);
                        break;
                    case "rotation":
                        Matrix4x4 rotMatrix = new Matrix4x4();
                        rotMatrix.SetRow(0, new Vector4((float)singleProp.Value[0][0], (float)singleProp.Value[0][1], (float)singleProp.Value[0][2], 0));
                        rotMatrix.SetRow(1, new Vector4((float)singleProp.Value[1][0], (float)singleProp.Value[1][1], (float)singleProp.Value[1][2], 0));
                        rotMatrix.SetRow(2, new Vector4((float)singleProp.Value[2][0], (float)singleProp.Value[2][1], (float)singleProp.Value[2][2], 0));
                        rotMatrix.SetRow(3, new Vector4(0,0,0,1));
                        
                        camPos.rotation = rotMatrix.rotation;
                
                        break;
                    default:
                        break;
                }
            }
            cameraList.Add(camPos);
        }
    }

    [SerializeField] float speed;
    [SerializeField] float rotationSpeed;
    int nextCam = 1;
    void Update()
    {
        Vector3 dir = cameraList[nextCam].position - transform.position;
        dir = dir.normalized;
        
        if (Vector3.Distance(transform.position, cameraList[nextCam].position) > 0.1f)
        {
            transform.position += dir * Time.deltaTime * speed;
        }
       // transform.position = Vector3.Lerp(transform.position, cameraList[nextCam].position, speed * Time.deltaTime); //No es muy smooth
        transform.rotation = Quaternion.Lerp(transform.rotation, cameraList[nextCam].rotation, rotationSpeed * Time.deltaTime);
   
        if (Vector3.Distance(transform.position, cameraList[nextCam].position) < 0.1f) //Cambiar la condicion, pq si a poco framrate fallara
        {
            nextCam = (nextCam + 1) % cameraList.Count();
        }
    }
}
