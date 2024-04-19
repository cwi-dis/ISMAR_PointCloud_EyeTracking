using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using Cwipc;




public class EyeDataProcess : MonoBehaviour
{

    //file reader
    [Header("Files")]
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
    public float globalAngleThreshold = 0.5f;
    public float acceptingDepthRange;
    public int angleSegments = 10;
    public int slices = 16;
    public float kappa = 1.5f;
    public float theta = 1.5f;

    public bool obtainOnlyResult = false;
    private List<Vector3> Valid_gaze_orgin = new List<Vector3>();
    private List<Vector3> Valid_gaze_direction = new List<Vector3>();

    public PrerecordedPointCloudReader pctReader;
    public PointCloudRenderer pcdRenderer;

    public int LengthOfRay = 25;
    [SerializeField] private LineRenderer GazeRayRenderer;
    bool isProcessing = false;

    Vector3 Valid_gaze_origin_world;
    Vector3 Valid_gaze_direction_world;
    string PC_path = "D:/xuemei/PointCloud_EyeTracking/Resource/loot_vox10_1000_good.ply";
    string EyeDatapath = "D:/xuemei/PointCloud_EyeTracking/Resource/eyetrackingdata-VRSMALL-20221129-1230.json";
    int curIdx = 0;


    // test for gaze result
    public GameObject sphere;




    void Awake()
    {
        // get the directory of PC_path
        //directoryName = Path.GetDirectoryName(PC_path);
        //// create temp directory
        //string tempFolder = "TempPCFolder"; 
        //string tempDirectory = Path.Combine(directoryName, tempFolder);
        //Debug.Log(tempDirectory);
        //Directory.CreateDirectory(tempDirectory);
        //// copy the file [PC_path] to temp directory
        //string temFile = tempDirectory + "/loot_vox10_1000.cwipcdump";
        //FileUtil.CopyFileOrDirectory(PC_path, temFile);
        // set temp directory to Reader.cs
        //pctReader.dirName = tempDirectory; //D:\xuemei\PointCloud_EyeTracking\Resource\TempPCFolder
        Debug.Log("Test start!");
        LoadPointCloud(PC_path);
        Debug.Log("Load Sucessful!");
        ReadEyeData(EyeDatapath);
        Debug.Log("Load EyeData Sucessful!");
        Debug.Log("Total Gaze Data:" + Valid_gaze_orgin.Count);
        Debug.Log(Valid_gaze_orgin[curIdx]);
        Debug.Log(Valid_gaze_direction[curIdx]);


        //if (sphere == null)
        //    Debug.LogError("sphere is null!");

    }


    void Update()
    {

        //Debug.Log("Processing Gaze Data:" + curIdx);
        ShowGaze(Valid_gaze_orgin, Valid_gaze_direction, curIdx);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Spacebar was pressed 
            Debug.Log("Spacebar was pressed !");
            ProcessAsync();
        }

    }


    void ShowGaze(List<Vector3> Valid_gaze_orgin, List<Vector3> Valid_gaze_direction, int curIdx)
    {
        //Debug.Log(Valid_gaze_orgin[curIdx]);
        //Debug.Log(Valid_gaze_direction[curIdx]);
        GazeRayRenderer.SetPosition(0, Valid_gaze_orgin[curIdx]); //
        GazeRayRenderer.SetPosition(1, Valid_gaze_orgin[curIdx] + Valid_gaze_direction[curIdx] * LengthOfRay);
    }



    void ProcessAsync()
    {
        if (isProcessing)
            return;
        isProcessing = true;

        Thread th = new Thread(ProcessFunc);
        th.IsBackground = true;
        th.Start();
    }





    void ProcessFunc()
    {
        // get current point cloud and set to xuemeiPointCloud;
        //cwipc.pointcloud xuemeiPointCloud = pctReader.GetCurrentPointCloud();
        //cwipc.pointcloud pct = xuemeiPointCloud;
        //var currentPointCloud_ = pct.get_points();
        //currentPointCloud = currentPointCloud_[]; //(Point color buffersize)

        //currentPointCloud = xuemeiPointCloud.get_points();

        for (curIdx = 0; curIdx < Valid_gaze_orgin.Count; curIdx++)
        {
            Vector3 Valid_gaze_origin_world = Valid_gaze_orgin[curIdx];
            Vector3 Valid_gaze_direction_world = Valid_gaze_direction[curIdx];

            // current pct and rendered pct ,mirror x , z   
            //if mirror z
            //        currentPointCloud


            RegisterPoints(Valid_gaze_direction_world, Valid_gaze_origin_world, currentPointCloud, 0.5f, 0.05f);
            Debug.Log("Now is GazeData index:" + curIdx);
        }
        WritePointCloud(currentPointCloud, currentPointGazeImportance);
        Debug.Log("has already wirte the point cloud!");
        isProcessing = false;
    }





    void LoadPointCloud(String PC_path)
    {
        if (!obtainOnlyResult)
        {

            currentPointCloud.Clear();
            currentPointGazeImportance.Clear();

            if (File.Exists(PC_path))
            {
                var stream = File.Open(PC_path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var header = ReadDataHeader(new StreamReader(stream));
                var body = ReadDataBody(header, new BinaryReader(stream));

                for (int i = 0; i < body.vertices.Count; i++)
                {
                    Vector3 v = body.vertices[i];
                    currentPointCloud.Add(v);
                    currentPointGazeImportance.Add(0f);
                }
            }
            else
            {

                Debug.LogWarning("currentObject Not Found");
            }
        }
    }

    enum DataProperty
    {
        Invalid,
        X, Y, Z,
        R, G, B, A,
        Data8, Data16, Data32
    }

    static int GetPropertySize(DataProperty p)
    {
        Debug.Log(p);
        switch (p)
        {

            case DataProperty.X: return 4;
            case DataProperty.Y: return 4;
            case DataProperty.Z: return 4;
        }
        return 0;
    }

    class DataHeader
    {
        public List<DataProperty> properties = new List<DataProperty>();
        public int vertexCount = -1;
    }

    class DataBody
    {
        public List<Vector3> vertices;

        public DataBody(int vertexCount)
        {
            vertices = new List<Vector3>(vertexCount);
        }

        public void AddPoint(
            float x, float y, float z
        )
        {
            vertices.Add(new Vector3(x, y, z));
        }
    }

    DataHeader ReadDataHeader(StreamReader reader)
    {
        var data = new DataHeader();
        var readCount = 0;

        // Magic number line ("ply")
        var line = reader.ReadLine();
        readCount += line.Length + 1;
        if (line != "ply")
            throw new ArgumentException("Magic number ('ply') mismatch.");

        // Data format: check if it's binary/little endian.
        line = reader.ReadLine();
        readCount += line.Length + 1;
        if (line != "format binary_little_endian 1.0")
            throw new ArgumentException(
                "Invalid data format ('" + line + "'). " +
                "Should be binary/little endian.");

        // Read header contents.
        for (var skip = false; ;)
        {
            // Read a line and split it with white space.
            line = reader.ReadLine();
            readCount += line.Length + 1;
            if (line == "end_header") break;
            var col = line.Split();

            // Element declaration (unskippable)
            if (col[0] == "element")
            {
                if (col[1] == "vertex")
                {
                    data.vertexCount = Convert.ToInt32(col[2]);
                    skip = false;
                }
                else
                {
                    // Don't read elements other than vertices.
                    skip = true;
                }
            }

            if (skip) continue;

            // Property declaration line
            if (col[0] == "property")
            {
                var prop = DataProperty.Invalid;

                // Parse the property name entry.
                switch (col[2])
                {
                    case "x": prop = DataProperty.X; break;
                    case "y": prop = DataProperty.Y; break;
                    case "z": prop = DataProperty.Z; break;
                }

                if (col[1] == "char" || col[1] == "uchar")
                {
                    if (prop == DataProperty.Invalid)
                        prop = DataProperty.Data8;
                    else if (GetPropertySize(prop) != 1)
                        throw new ArgumentException("Invalid property type ('" + line + "').");
                }
                else if (col[1] == "short" || col[1] == "ushort")
                {
                    if (prop == DataProperty.Invalid)
                        prop = DataProperty.Data16;
                    else if (GetPropertySize(prop) != 2)
                        throw new ArgumentException("Invalid property type ('" + line + "').");
                }
                else if (col[1] == "int" || col[1] == "uint" || col[1] == "float")
                {
                    if (prop == DataProperty.Invalid)
                        prop = DataProperty.Data32;
                    else if (GetPropertySize(prop) != 4)
                        throw new ArgumentException("Invalid property type ('" + line + "').");
                }
                else
                {
                    throw new ArgumentException("Unsupported property type ('" + line + "').");
                }


                data.properties.Add(prop);
            }
        }

        // Rewind the stream back to the exact position of the reader.
        reader.BaseStream.Position = readCount;

        return data;
    }

    DataBody ReadDataBody(DataHeader header, BinaryReader reader)
    {
        var data = new DataBody(header.vertexCount);

        float x = 0, y = 0, z = 0;

        for (var i = 0; i < header.vertexCount; i++)
        {
            foreach (var prop in header.properties)
            {
                switch (prop)
                {
                    case DataProperty.X: x = reader.ReadSingle(); break;
                    case DataProperty.Y: y = reader.ReadSingle(); break;
                    case DataProperty.Z: z = reader.ReadSingle(); break;
                    case DataProperty.Data8: reader.ReadByte(); break;
                    case DataProperty.Data16: reader.BaseStream.Position += 2; break;
                    case DataProperty.Data32: reader.BaseStream.Position += 4; break;
                }
            }
            data.AddPoint(x, y, z);

        }

        return data;
    }


    void ReadEyeData(string EyeDatapath)
    {
        string dataJsonString = File.ReadAllText(EyeDatapath, Encoding.UTF8);
        List<FullData_cwi> fullDataList = JsonConvert.DeserializeObject<List<FullData_cwi>>(dataJsonString);
        foreach (var item in fullDataList)
        {
            CameraMatrix cm = item.camera_matrix;
            Matrix4x4 matrix = new Matrix4x4();
            //Marshal.
            matrix.m00 = cm.e00;
            matrix.m10 = cm.e10;
            matrix.m20 = cm.e20;
            matrix.m30 = cm.e30;
            matrix.m01 = cm.e01;
            matrix.m11 = cm.e11;
            matrix.m21 = cm.e21;
            matrix.m31 = cm.e31;
            matrix.m02 = cm.e02;
            matrix.m12 = cm.e12;
            matrix.m22 = cm.e22;
            matrix.m03 = cm.e03;
            matrix.m13 = cm.e13;
            matrix.m23 = cm.e23;
            matrix.m33 = cm.e33;
            Vector3 point = new Vector3(item.eye_data_cwi.verbose_data.combined.eye_data.gaze_origin_mm.x * (-1), item.eye_data_cwi.verbose_data.combined.eye_data.gaze_origin_mm.y, item.eye_data_cwi.verbose_data.combined.eye_data.gaze_origin_mm.z * (-1));
            Vector3 direction = new Vector3(item.eye_data_cwi.verbose_data.combined.eye_data.gaze_direction_normalized.x * (-1), item.eye_data_cwi.verbose_data.combined.eye_data.gaze_direction_normalized.y, item.eye_data_cwi.verbose_data.combined.eye_data.gaze_direction_normalized.z * (-1));
            Vector3 origin_world = matrix.MultiplyPoint(point / 1000); //matrix.transpose
            Vector3 direction_world = matrix.MultiplyVector(direction); //matrix.transpose
            Valid_gaze_orgin.Add(origin_world);
            Valid_gaze_direction.Add(direction_world);
        }
    }
    void RegisterPoints(Vector3 gazeRay, Vector3 camPos, List<Vector3> currentPointCloud, float currentAngleThreshold, float acceptingDepthRange)
    {
        if (!obtainOnlyResult)
        {
            pointWithinRange.Clear();
            pointIndices.Clear();
            float angleThreshold;
            angleThreshold = Mathf.Max(globalAngleThreshold, currentAngleThreshold);
            angleThreshold += 0.5f;
            float minDistance = float.MaxValue;
            Vector3 normalVector = new Vector3(1f, 1f, -(gazeRay.x + gazeRay.y) / gazeRay.z);
            List<int>[] segments = new List<int>[slices * angleSegments];
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
                point.z *= -1; // mirror z to mirror the point cloud
                Vector3 dir = point - camPos;
                float angleInDegree = Mathf.Abs(Vector3.Angle(gazeRay, dir));


                if (angleInDegree < angleThreshold)
                {
                    pointWithinRange.Add(point);
                    pointIndices.Add(i);
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

            for (int i = 0; i < segments.Length; i++)
            {
                Vector3 dirClose = closestPoints[i] - camPos;
                float mDist = Vector3.Dot(gazeRay, dirClose) / gazeRay.magnitude;
                float radius = (mDist + acceptingDepthRange) * Mathf.Tan(angleThreshold * Mathf.PI / 180);
                foreach (int j in segments[i])
                {
                    Vector3 point = currentPointCloud[j];
                    point.z *= -1; // To do change currentPointCloud mirror z
                    Vector3 diffvec = point - closestPoints[i];
                    float depth = Vector3.Dot(gazeRay, diffvec) / gazeRay.magnitude;

                    if (depth < acceptingDepthRange && depth > 0f)
                    {
                        Vector3 dir = point - camPos;
                        float angleInDegree = Mathf.Abs(Vector3.Angle(gazeRay, dir));
                        float pDist = Vector3.Dot(gazeRay, dir) / gazeRay.magnitude;
                        float pRadius = pDist * Mathf.Tan(angleInDegree * Mathf.PI / 180);
                        float var = radius * radius / 3f / 3f;
                        currentPointGazeImportance[j] += Mathf.Exp(-Mathf.Pow(pRadius, 2f) / (2f * var)) / Mathf.Sqrt(2f * Mathf.PI * var);
                    }


                    // for current point,  creat a sphere obj to render
                    //var aa = Instantiate(sphere);




                }
            }

        }
    }






    void WritePointCloud(List<Vector3> currentPointCloud, List<float> currentPointGazeImportance)
    {
        if (!obtainOnlyResult)
        {
            string oPath = "D:/xuemei/PointCloud_EyeTracking/Resource/loot_leftfoot_Mirrorz_VisualAngle5_1Depth_v1.txt";
            string path = oPath;
            File.WriteAllText(path, string.Empty);
            sw = new StreamWriter(path, true);
            sw.WriteLine("PosX PosY PosZ GazeCount");
            sw.Flush();

            for (int i = 0; i < currentPointCloud.Count; i++)
            {
                sw.WriteLine(currentPointCloud[i].x + " " + currentPointCloud[i].y + " " + currentPointCloud[i].z * (-1) + " " + currentPointGazeImportance[i]);
                sw.Flush();
            }
            sw.Dispose();
        }
    }

    public class FullData_cwi
    {
        public CameraMatrix camera_matrix { get; set; }
        public int pointcloudTs { get; set; }
        public EyeDataCwi eye_data_cwi { get; set; }
    }

    [Serializable]
    public class CameraMatrix
    {
        public float e00 { get; set; }
        public float e01 { get; set; }
        public float e02 { get; set; }
        public float e03 { get; set; }
        public float e10 { get; set; }
        public float e11 { get; set; }
        public float e12 { get; set; }
        public float e13 { get; set; }
        public float e20 { get; set; }
        public float e21 { get; set; }
        public float e22 { get; set; }
        public float e23 { get; set; }
        public float e30 { get; set; }
        public float e31 { get; set; }
        public float e32 { get; set; }
        public float e33 { get; set; }
    }

    public class Combined
    {
        public EyeData eye_data { get; set; }
        public bool convergence_distance_validity { get; set; }
        public double convergence_distance_mm { get; set; }
    }

    public class ExpressionData
    {
        public Left left { get; set; }
        public Right right { get; set; }
    }

    public class EyeData
    {
        public int eye_data_validata_bit_mask { get; set; }
        public GazeOriginMm gaze_origin_mm { get; set; }
        public GazeDirectionNormalized gaze_direction_normalized { get; set; }
        public double pupil_diameter_mm { get; set; }
        public double eye_openness { get; set; }
        public PupilPositionInSensorArea pupil_position_in_sensor_area { get; set; }
    }

    public class EyeDataCwi
    {
        public bool no_user { get; set; }
        public int frame_sequence { get; set; }
        public int timestamp { get; set; }
        public VerboseData verbose_data { get; set; }
        public ExpressionData expression_data { get; set; }
    }

    public class GazeDirectionNormalized
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class GazeOriginMm
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class Left
    {
        public int eye_data_validata_bit_mask { get; set; }
        public GazeOriginMm gaze_origin_mm { get; set; }
        public GazeDirectionNormalized gaze_direction_normalized { get; set; }
        public double pupil_diameter_mm { get; set; }
        public double eye_openness { get; set; }
        public PupilPositionInSensorArea pupil_position_in_sensor_area { get; set; }
        public float eye_wide { get; set; }
        public float eye_squeeze { get; set; }
        public float eye_frown { get; set; }
    }

    public class PupilPositionInSensorArea
    {
        public float x { get; set; }
        public float y { get; set; }
    }

    public class Right
    {
        public int eye_data_validata_bit_mask { get; set; }
        public GazeOriginMm gaze_origin_mm { get; set; }
        public GazeDirectionNormalized gaze_direction_normalized { get; set; }
        public float pupil_diameter_mm { get; set; }
        public float eye_openness { get; set; }
        public PupilPositionInSensorArea pupil_position_in_sensor_area { get; set; }
        public float eye_wide { get; set; }
        public float eye_squeeze { get; set; }
        public float eye_frown { get; set; }
    }


    public class VerboseData
    {
        public Left left { get; set; }
        public Right right { get; set; }
        public Combined combined { get; set; }
    }
}
/// <summary>
/// 
/// </summary>
//public PointCloudPoint[] get_points()
//{
//    int npoint = count();
//    PointCloudPoint[] rv = new PointCloudPoint[npoint];
//    unsafe
//    {
//        int nbytes = get_uncompressed_size();
//        var pointBuffer = new Unity.Collections.NativeArray<point>(npoint, Unity.Collections.Allocator.Temp);
//        System.IntPtr pointBufferPointer = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(pointBuffer);
//        int ret = copy_uncompressed(pointBufferPointer, nbytes);
//        if (ret * 16 != nbytes || ret != npoint)
//        {
//            throw new Exception("cwipc.pointcloud.get_points unexpected point count");
//        }
//        for (int i = 0; i < npoint; i++)
//        {
//            rv[i].point = new Vector3(pointBuffer[i].x, pointBuffer[i].y, pointBuffer[i].z);
//            rv[i].color = new Color(((float)pointBuffer[i].r) / 256.0f, ((float)pointBuffer[i].g) / 256.0f, ((float)pointBuffer[i].b) / 256.0f);
//            rv[i].tile = pointBuffer[i].tile;
//        }
//    }

//    return rv;
//}
