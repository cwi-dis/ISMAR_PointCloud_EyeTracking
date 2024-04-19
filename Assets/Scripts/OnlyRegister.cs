using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Cwipc;
using UnityEditor;


public class OnlyRegister : MonoBehaviour
{

    //file reader
    [Header("Files")]
    public string JsonFile;
    public StreamReader sr;
    [Space(10)]


    public StreamWriter sw;
    public StreamReader srTransformData;


    private List<Vector3> currentPointCloud = new List<Vector3>();
    private List<float> currentPointGazeImportance = new List<float>();
    private int currentIndex;
    private List<Vector3> pointWithinRange = new List<Vector3>();
    private List<int> pointIndices = new List<int>();

    [Header("RegisterPoints")]
    //processing param
    public float globalAngleThreshold = 2.0f;
    public float acceptingDepthRange;
    public int angleSegments = 10;
    public int slices = 16;
    public float kappa = 1.5f;
    public float theta = 1.5f;


    Vector3 Valid_gaze_origin_world;
    Vector3 Valid_gaze_direction_world;

    int curIdx = 0;


    void Start()
    {
        float angle = 1f;
        double radians = angle * (Math.PI / 180);
        float radians_ = (float)radians;
        float z = (float)Math.Tan(radians_);
        // point cloud
        List<Vector3> currentPointCloud = new List<Vector3>() { new Vector3(0, 0, 2.0f), new Vector3(0, 1, z), new Vector3(1, 1, 1) };
        List<Vector3> Valid_gaze_direction = new List<Vector3>() { new Vector3(0, 0, 1), new Vector3(0, 1, z), new Vector3(1, 1, 1) };
        List<Vector3> Valid_gaze_orgin = new List<Vector3>() { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 0) };
        for (curIdx = 0; curIdx < Valid_gaze_orgin.Count; curIdx++)
        {
            Vector3 Valid_gaze_origin_world = Valid_gaze_orgin[curIdx];
            Vector3 Valid_gaze_direction_world = Valid_gaze_direction[curIdx];
            RegisterPoints(Valid_gaze_direction_world, Valid_gaze_origin_world);
            Debug.Log("Now is GazeData index:" + curIdx);
        }
        WritePointCloud();
        Debug.Log("has already wirte the point cloud!");
    }
    void Update()
    {

        
    }







    void RegisterPoints(Vector3 gazeRay, Vector3 camPos) // currentAngleThreshold = newAngularError which means new angle
    {
        if (true)
        {
            Debug.Log("Starting Register Point Cloud!");
            pointWithinRange.Clear();
            pointIndices.Clear();
            float angleThreshold;
            angleThreshold = globalAngleThreshold; // because the angle has the headset slippage
            angleThreshold += 0.5f;  // why you still need more 0.5 degree??
            float minDistance = float.MaxValue;
            Vector3 normalVector = new Vector3(1f, 1f, -(gazeRay.x + gazeRay.y) / gazeRay.z);
            List<int>[] segments = new List<int>[slices * angleSegments]; //16 * 10
            Vector3[] closestPoints = new Vector3[slices * angleSegments];
            float[] minDistances = new float[slices * angleSegments];

            for (int i = 0; i < slices * angleSegments; i++)
            {
                segments[i] = new List<int>();
                minDistances[i] = float.MaxValue;
            }

            for (int i = 0; i < currentPointCloud.Count; i++)
            {
                Vector3 point = currentPointCloud[i];
                Debug.Log("Original xyz:" + point);
                //point.z *= (-1);
                //Debug.Log("After mirror z" + point);

                Vector3 dir = point - camPos;
                float angleInDegree = Mathf.Abs(Vector3.Angle(gazeRay, dir));

                if (angleInDegree < angleThreshold)
                {
                    pointWithinRange.Add(point);
                    pointIndices.Add(i);
                    Debug.Log("WithinRangeIndex" + i + point);
                    float distance = Mathf.Abs(Vector3.Dot(dir, gazeRay) / gazeRay.magnitude);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }

                    float perAngle = angleThreshold / angleSegments;
                    for (int p = 0; p < angleSegments; p++)
                    {
                        if (angleInDegree <= (p + 1) * perAngle && angleInDegree > p * perAngle)
                        {
                            float lamda = gazeRay.x * point.x + gazeRay.y * point.y + gazeRay.z * point.z;
                            float k = (lamda - gazeRay.x * camPos.x - gazeRay.y * camPos.y - gazeRay.z * camPos.z) / (gazeRay.x * gazeRay.x + gazeRay.y * gazeRay.y + gazeRay.z * gazeRay.z);
                            Vector3 intersect = camPos + k * gazeRay;
                            Vector3 distanceVector = point - intersect;
                            float angle = Vector3.SignedAngle(normalVector, distanceVector, gazeRay) + 180f;
                            float perSlice = 360f / slices;
                            for (int q = 0; q < slices; q++)
                            {
                                if (angle <= (q + 1) * perSlice && angle > q * perSlice)
                                {
                                    segments[p * slices + q].Add(i);
                                    if (distance < minDistances[p * slices + q])
                                    {
                                        minDistances[p * slices + q] = distance;
                                        closestPoints[p * slices + q] = point;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            List<int> recordedPointIndices = new List<int>();

            for (int i = 0; i < segments.Length; i++)
            {
                Vector3 dirClose = closestPoints[i] - camPos;
                float mDist = Vector3.Dot(gazeRay, dirClose) / gazeRay.magnitude;
                float radius = (mDist + acceptingDepthRange) * Mathf.Tan(angleThreshold * Mathf.PI / 180);
                foreach (int j in segments[i])
                {
                    Vector3 point = currentPointCloud[j];
                    Vector3 diffvec = point - closestPoints[i];

                    float depth = Vector3.Dot(gazeRay, diffvec) / gazeRay.magnitude;

                    if (depth < acceptingDepthRange && depth > 0f)
                    {
                        Vector3 dir = point - camPos;
                        float angleInDegree = Mathf.Abs(Vector3.Angle(gazeRay, dir));
                        float pDist = Vector3.Dot(gazeRay, dir) / gazeRay.magnitude;
                        float pRadius = pDist * Mathf.Tan(angleInDegree * Mathf.PI / 180);
                        float var = radius * radius / 3f / 3f;
                        currentPointGazeImportance[j] += 100f * Mathf.Exp(-Mathf.Pow(pRadius, 2f) / (2f * var)) / Mathf.Sqrt(2f * Mathf.PI * var);
                        Debug.Log(j);
                    }
                }
            }

        }
    }





    void WritePointCloud()
    {
        if (true)
        {
            string oPath = "D:/xuemei/PointCloud_EyeTracking/Resource/loot_test5.txt";
            string path = oPath;
            File.WriteAllText(path, string.Empty);
            sw = new StreamWriter(path, true);
            sw.WriteLine("PosX PosY PosZ GazeCount");
            sw.Flush();

            for (int i = 0; i < currentPointCloud.Count; i++)
            {
                sw.WriteLine(currentPointCloud[i].x + " " + currentPointCloud[i].y + " " + currentPointCloud[i].z + " " + currentPointGazeImportance[i]);
                sw.Flush();
            }
            sw.Dispose();
        }
    }
}

