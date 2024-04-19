using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTransfrom : MonoBehaviour
{
    Vector3 Origin_local = new Vector3(3, 4, 5);
    Vector3 direction_local = new Vector3(1, 1, 6);
    private static Matrix4x4 lastCameraMatrix;
    private static Matrix4x4 anothermatrix;
    private Camera cam;
    // Start is called before the first frame update
    void Start()
    {  cam = Camera.main;}

    // Update is called once per frame
    void Update()
    {
        lastCameraMatrix = Camera.main.cameraToWorldMatrix;
        anothermatrix = cam.cameraToWorldMatrix;
        Debug.Log(lastCameraMatrix);
        Debug.Log(anothermatrix);
        Debug.Log("Matrix is equal or not:" + (lastCameraMatrix == anothermatrix));
        Vector3 Unity_point = cam.transform.TransformPoint(Origin_local);
        Debug.Log("Unity Point :" + Unity_point);


        Vector3 Matrix_point = lastCameraMatrix.MultiplyPoint( Vector3.Scale(Origin_local,new Vector3(1,1,-1)));
        // This is because if I use the matrix and multiplyPoint I have to minus z 
        Debug.Log("My Point :" + Matrix_point);

        Vector3 GazeMetrics_point = cam.transform.localToWorldMatrix.MultiplyPoint(Origin_local);
        Debug.Log("GazeMetric Point :" + Matrix_point);


        /// summary
        /// if use 
        /// 1. cam.transform.TransformPoint: directly using the point, and Unity will do the minus z from camera(eye/viewing) space to world space
        /// 2. lastCameraMatrix.MultiplyPoint: the z value of the point need to mutiply by -1 (manually)
        /// 3. cam.transform.localToWorldMatrix.MultiplyPoint: directly using the point, and Unity will do the minus z from camera(eye/viewing) space to world space
        /// conclusion: if has the transform, do thing 
        /// else: z change to -z!!!!
        /// 
        //Camera.camera
        lastCameraMatrix.MultiplyPoint(Vector3.Scale(Origin_local, new Vector3(1, 1, -1)));
    }
}
