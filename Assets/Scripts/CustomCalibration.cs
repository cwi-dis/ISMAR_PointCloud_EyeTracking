using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomCalibration : MonoBehaviour
{

    private Camera sceneCamera;
    // private Vector2 gazePointLeft;
    // private Vector2 gazePointRight;
    private Vector3 gazePointCenter;
    private Vector3 standardViewportPoint = new Vector3(0.5f, 0.5f, 10);
    public float baseAngularError;
    public float casualCameraRotation;
    public float scaleFactorA, scaleFactorB;
    public bool head;

    public GameObject greenRing;
    private Vector3 oScale;
    public bool is3D;
    

    // Use this for initialization
    void Start()
    {
        sceneCamera = GetComponent<Camera>();
        oScale = greenRing.transform.localScale;
    }

    //private void OnEnable()
    //{
    //    if (PupilTools.IsConnected)
    //    {
    //        PupilTools.IsGazing = true;
    //        PupilTools.SubscribeTo("gaze");
    //    }
    //}
    //
    //private void OnDisable()
    //{
    //    PupilTools.UnSubscribeFrom("gaze");
    //}

    // Update is called once per frame
    void Update()
    {
        
        
        Vector3 viewportPoint = standardViewportPoint;

        Debug.Log(sceneCamera.transform.InverseTransformPoint(sceneCamera.ViewportToWorldPoint(Vector3.one)).ToString("F4"));

        if (head)
        {
            Ray ray = sceneCamera.ViewportPointToRay(viewportPoint);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                greenRing.transform.position = hit.point;
                float dist = Vector3.Distance(transform.position, hit.point);
                //float r = dist * Mathf.Tan(angularError * Mathf.PI / 180f);
                greenRing.transform.localScale = oScale * dist * Mathf.Tan(baseAngularError * Mathf.PI / 180f);
                greenRing.transform.LookAt(this.transform);
            }
        }
        else
        {
            //if (PupilTools.IsConnected && PupilTools.IsGazing)
            //{
            //    // gazePointLeft = PupilData._3D.GetEyePosition(sceneCamera, PupilData.leftEyeID);
            //    // gazePointRight = PupilData._2D.GetEyePosition(sceneCamera, PupilData.rightEyeID);
            //    gazePointCenter = PupilData._2D.GazePosition;
            //    viewportPoint = new Vector3(gazePointCenter.x, gazePointCenter.y, 1f);
            //    //Debug.Log(viewportPoint);
            //}
            float angularError = baseAngularError;

            float rotX = transform.eulerAngles.x;
            float rotZ = transform.eulerAngles.z;

            while (rotX > 180f)
            {
                rotX -= 360f;
            }

            while (rotZ > 180f)
            {
                rotZ -= 360f;
            }

            //Debug.Log(rotX + " " + rotZ);
            //
            //if (Mathf.Abs(rotX) > 25f )
            //{
            //    angularError = baseAngularError + scaleFactorX * (Mathf.Abs(rotX) - 25f);
            //}
            //
            //if(Mathf.Abs(rotZ) > 10f)
            //{
            //    angularError = baseAngularError + scaleFactorZ * (Mathf.Abs(rotZ) - 10f);
            //}

            //viewportPoint = new Vector3(middlePos.x, middlePos.y, 1f);



            //Ray ray = sceneCamera.ViewportPointToRay(viewportPoint);

            Vector3 localGazePos = sceneCamera.transform.InverseTransformPoint(sceneCamera.ViewportToWorldPoint(viewportPoint));
            float angle = Vector3.SignedAngle(Vector3.forward, new Vector3(0f, localGazePos.y, localGazePos.z), Vector3.right);
            Debug.Log(angle);

            float gazePosAngleCompensation = scaleFactorA * (rotX - (5f + scaleFactorB * angle));

            Vector3 newLocalGazePos = Quaternion.Euler(-gazePosAngleCompensation, 0f, 0f) * localGazePos;

            Ray ray = new Ray(sceneCamera.transform.position, sceneCamera.transform.TransformPoint(newLocalGazePos) - sceneCamera.transform.position);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                greenRing.transform.position = hit.point;
                float dist = Vector3.Distance(transform.position, hit.point);
                float r = dist * Mathf.Tan(angularError * Mathf.PI / 180f);
                greenRing.transform.localScale = oScale * r;
                greenRing.transform.LookAt(this.transform);
            }
        }
    }
}