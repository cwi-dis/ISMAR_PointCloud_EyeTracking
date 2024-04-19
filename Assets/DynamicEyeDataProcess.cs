using Cwipc;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class DynamicEyeDataProcess : MonoBehaviour
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

    public PrerecordedPointCloudReader pctReader;
    public PointCloudRenderer pcdRenderer;

    public int LengthOfRay = 25;
    [SerializeField] private LineRenderer GazeRayRenderer;
    public Gradient gradient = new Gradient();


    bool isProcessing = false;

    Vector3 Valid_gaze_origin_world;
    Vector3 Valid_gaze_direction_world;
    // H4C1R5
    string ply_Folder_path = @"E:\PLY\H4_C1_R5\";
    string dump_Folder_path = @"E:\DUMP\H4_C1_R5\";
    string GazeDataDir = @"D:\xuemei\RawData\user_001_shishir\20230106-1641_001_A.json";
    string userid;
    string session;



    float fixationDispersionThreshold = 3f;
    float fixationIntervalThreshold = 100;
    string pattern = "DUMP";
    string replace = "PLY";
    public string only_folder_filename;
    public string HeatMapSaveName;
    public string previous_pointcloud;
    public string previous_pcFlag;
    public float CompensateAngle;
    public bool needWrite;
    public string savefoldername;
    public string save_txt_filename;
    List<ValidGaze> gazeWindowSet = new List<ValidGaze>();
    // test for gaze result
    //public GameObject sphere;


    void Start()
    {
        // get userid and session from GazeDataDir
        // TODO
        string jsonfilename = Path.GetFileNameWithoutExtension(GazeDataDir);
        string[] words = jsonfilename.Split('_');
        userid = words[1];
        session = words[2];
        Debug.Log("Test start:  userid is" + userid + " and session is" + session);
        GazeRayRenderer = GetComponent<LineRenderer>();
        GazeRayRenderer.material = new Material(Shader.Find("Sprites/Default"));
        // a simple 2 color gradient with a fixed alpha of 1.0f.
        float alpha = 1.0f;
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
            );
    }


    void Update()
    {
        ShowGaze(Valid_gaze_origin_world, Valid_gaze_direction_world);
        if (Keyboard.current.spaceKey.isPressed)
        {
            // Spacebar was pressed 
            Debug.Log("Spacebar was pressed !");
            ProcessAsync();
        }

    }


    void ShowGaze(Vector3 Valid_gaze_orgin, Vector3 Valid_gaze_direction)
    {
        GazeRayRenderer.SetPosition(0, Valid_gaze_orgin);
        GazeRayRenderer.SetPosition(1, Valid_gaze_orgin + Valid_gaze_direction * LengthOfRay);
        GazeRayRenderer.startColor = Color.red;
        GazeRayRenderer.colorGradient = gradient;
        GazeRayRenderer.endColor = Color.green;
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
        ReadEyeData(GazeDataDir);
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

    /// <param name="EyeDatapath"></param>
    void ReadEyeData(string EyeDatapath)
    {
        string dataJsonString = File.ReadAllText(EyeDatapath);
        List<FullData_cwi> fullDataList = JsonConvert.DeserializeObject<List<FullData_cwi>>(dataJsonString);
        //HashSet<string> uniqueFilenames = new HashSet<string>();
        for (int i = 0; i < fullDataList.Count; i++) //
        {
            string dump_filename = fullDataList[i].pcname; // 
            if (dump_filename == "" || dump_filename.Contains("H4_C1_R5")) 
            { continue; }
                    
            if (fullDataList[i].eye_data_cwi.verbose_data.combined.eye_data.eye_data_validata_bit_mask == 3) // 1.valid
            {
                Vector3 origin_world = fullDataList[i].gaze_origin_global_combined;
                Vector3 direction_world = fullDataList[i].gaze_direction_global_combined;
                Valid_gaze_origin_world = origin_world;  // for showing the gaze
                Valid_gaze_direction_world = direction_world;
                int TimeStamp = fullDataList[i].eye_data_cwi.timestamp;
                CameraMatrix cm = fullDataList[i].camera_matrix;
                Matrix4x4 matrix = new Matrix4x4();
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
                /// 2. is fixation point or not? (velocity based method)
                ValidGaze gaze = new ValidGaze();
                gaze.gaze_origin_global_combined = origin_world;
                gaze.gaze_direction_global_combined = direction_world;
                gaze.timestamp = TimeStamp;
                gaze.filename = fullDataList[i].pcname; 
                gaze.cameramatrix = matrix;

                bool windowSetIsFixation = CalculateFixation(gaze, fixationDispersionThreshold, fixationIntervalThreshold);

                if (windowSetIsFixation)
                {
                    // here, all the elements in gazeWindowSet are fixation gazes, except the last one
                    for (int q = 0; q < (gazeWindowSet.Count - 1); q++)
                    {
                        if (q == 0) // the first gazedata of the gazeWindowSet
                        {
                            ValidGaze GazeFixation = gazeWindowSet[q];
                            string dump_filename_fixation = GazeFixation.filename;
                            string ply_filename = Path.ChangeExtension(dump_filename_fixation, "ply"); // from dump to ply
                            string ply_full_filename = Regex.Replace(ply_filename, pattern, replace); // load the point cloud frame by file name ply format
                            string pcFlag = Path.GetFileName(Path.GetDirectoryName(dump_filename_fixation)); // H1_C1_R1
                            previous_pcFlag = pcFlag;
                            LoadPointCloud(ply_full_filename); //right hand coordinate system // load corresponding point cloud data
                            previous_pointcloud = ply_full_filename;
                            Debug.Log("Load Sucessful!");
                            print(" the" + (i + 1) + "gazedata");
                            string csvfn = $"*_{userid}_{session}_{pcFlag}_experiment_data_metrics.csv";
                            string[] fns = Directory.GetFiles(Path.GetDirectoryName(GazeDataDir), csvfn);
                            if (fns.Length != 1)
                            {
                                Debug.LogError("GazeMetrics has multiple metric csv file, should only has one csv file for one sequence!");
                            }
                            string filename = fns[0];
                            List<Tuple<Vector3, float>> markerpos = getMarkerPositionFromCSV(filename);
                            var localPos = gaze.cameramatrix.inverse.MultiplyPoint(gaze.gaze_origin_global_combined);
                            var localDir = gaze.cameramatrix.inverse.MultiplyVector(gaze.gaze_direction_global_combined);
                            CompensateAngle = ErrorInterpolation(markerpos, localPos, localDir);
                            RegisterPoints(GazeFixation.gaze_direction_global_combined, GazeFixation.gaze_origin_global_combined, currentPointCloud, CompensateAngle, 0.05f);
                        }
                        else 
                        {
                            ValidGaze GazeFixation = gazeWindowSet[q];
                            string dump_filename_fixation = GazeFixation.filename;
                            string ply_filename = Path.ChangeExtension(dump_filename_fixation, "ply"); // from dump to ply
                            string ply_full_filename = Regex.Replace(ply_filename, pattern, replace); // load the point cloud frame by file name ply format
                            string pcFlag = Path.GetFileName(Path.GetDirectoryName(dump_filename_fixation)); // H1_C1_R1
                            if (ply_full_filename == previous_pointcloud)
                                {
                                    Debug.Log("Same Point Cloud already loaded!");
                                    print(" the" + (i + q + 1) + "gazedata");
                                    needWrite = false;
                                }
                            else 
                            {
                                needWrite = true;
                                LoadPointCloud(ply_full_filename);
                                savefoldername = pcFlag;
                                save_txt_filename = Path.GetFileName(Path.ChangeExtension(dump_filename_fixation, "txt"));


                            }
                            if (pcFlag == previous_pcFlag)
                                {
                                    Debug.Log("Same GazeMetric csv file already read!");
                                }
                            else 
                                {
                                    string csvfn = $"*_{userid}_{session}_{pcFlag}_experiment_data_metrics.csv";
                                    string[] fns = Directory.GetFiles(Path.GetDirectoryName(GazeDataDir), csvfn);
                                    if (fns.Length != 1)
                                    {
                                         Debug.LogError("GazeMetrics has multiple metric csv file, should only has one csv file for one sequence!");
                                    }
                                    string filename = fns[0];
                                    List<Tuple<Vector3, float>> markerpos = getMarkerPositionFromCSV(filename);
                                    var localPos = gaze.cameramatrix.inverse.MultiplyPoint(gaze.gaze_origin_global_combined);
                                    var localDir = gaze.cameramatrix.inverse.MultiplyVector(gaze.gaze_direction_global_combined);
                                    CompensateAngle = ErrorInterpolation(markerpos, localPos, localDir);
                                }

                            RegisterPoints(GazeFixation.gaze_direction_global_combined, GazeFixation.gaze_origin_global_combined, currentPointCloud, CompensateAngle, 0.05f);
                            if (needWrite)
                            {
                                WritePointCloud(currentPointCloud, currentPointGazeImportance, savefoldername, save_txt_filename);
                            }

                        }
                        savefoldername = Path.GetFileName(Path.GetDirectoryName(gazeWindowSet[q].filename));
                        save_txt_filename = Path.GetFileName(Path.ChangeExtension(gazeWindowSet[q].filename, "txt"));
                    }
                    
                    WritePointCloud(currentPointCloud, currentPointGazeImportance, savefoldername, save_txt_filename);
                    gazeWindowSet.RemoveRange(0, gazeWindowSet.Count - 1);  // only the last item is left
                }

            }

        }

    }


    /// <summary>
    /// The z value is 1.5 meters, which is correct.
    /// </summary>
    /// <param name="markerpos">the local position and error of marker list</param>
    /// <param name="gazeorigin">the local origin coordinate of gaze</param>
    /// <param name="gazedirection">the local direction of gaze</param>
    /// <returns></returns>
    float ErrorInterpolation(List<Tuple<Vector3, float>> markerpos, Vector3 gazeorigin, Vector3 gazedirection)
    {
        // item1 of tuple is local position, item2 is error
        // 1. calc the pos in plane of gaze ray
        Plane m_plane = new Plane(markerpos[0].Item1, markerpos[1].Item1, markerpos[2].Item1);
        Ray gazeRay = new Ray(gazeorigin, gazedirection);
        float gazedis;
        bool rst = m_plane.Raycast(gazeRay, out gazedis);

        //double eps = 1e-7;
        if (!rst && gazedis == 0)
        {
            Debug.LogError("parall");
        }
        else if (!rst && gazedis < 0)
        {
            Debug.LogError("negtive");
        }

        // ok, calc local pos from gazeRay and gazedis
        Vector3 gazepos = gazedirection * gazedis + gazeorigin;

        // 2. get the 3 markers
        var marker0 = markerpos[0].Item1;
        markerpos.RemoveAt(0);

        GetNearestMarkers(markerpos, gazepos, out Vector3 MinMarker, out Vector3 SecondMinMarker, out float MinError1, out float MinError2);
        var marker1 = MinMarker;
        var marker2 = SecondMinMarker;

        // 3. interpolation
        float dist1 = Vector3.Distance(marker0, gazepos);
        float dist2 = Vector3.Distance(marker1, gazepos);
        float dist3 = Vector3.Distance(marker2, gazepos);
        float MinError0 = markerpos[0].Item2;
        List<float> dis_all = new List<float> { dist1, dist2, dist3 };
        List<float> error_all = new List<float> { MinError0, MinError1, MinError2 };
        float CompensateAngle = WeightedSum(dis_all, error_all);

        return CompensateAngle;
    }

    public List<Tuple<Vector3, float>> getMarkerPositionFromCSV(string csvfilename) 
    {
        List<Tuple<Vector3, float>> markerpos = new List<Tuple<Vector3, float>>();
        using (var reader = new StreamReader(csvfilename))
        {
            Vector3 position = Vector3.zero;
            float angle = 0;
            string headerLine = reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                position = new Vector3(float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3]));
                angle = float.Parse(values[7]);
                markerpos.Add(new Tuple<Vector3, float>(position, angle));

            }
        }
        return markerpos;
    }

    /// <summary>
    /// already tested! correct!
    /// </summary>
    /// <param name="dis_all"></param>
    /// <param name="error_all"></param>
    /// <returns></returns>
    float WeightedSum(List<float> dis_all, List<float> error_all)
    {
        // 1. normalize the distance
        float normalized_dis_0 = dis_all[0] / dis_all.Sum();
        float normalized_dis_1 = dis_all[1] / dis_all.Sum();
        float normalized_dis_2 = dis_all[2] / dis_all.Sum();
        normalized_dis_0 = 1 / normalized_dis_0;
        normalized_dis_1 = 1 / normalized_dis_1;
        normalized_dis_2 = 1 / normalized_dis_2;
        float ratio_dis_0 = normalized_dis_0 / (normalized_dis_0 + normalized_dis_1 + normalized_dis_2);
        float ratio_dis_1 = normalized_dis_1 / (normalized_dis_0 + normalized_dis_1 + normalized_dis_2);
        float ratio_dis_2 = normalized_dis_2 / (normalized_dis_0 + normalized_dis_1 + normalized_dis_2);
        float weightedAverageError = ratio_dis_0 * error_all[0] + ratio_dis_1 * error_all[1] + ratio_dis_2 * error_all[2];
        return weightedAverageError;
    }


    void GetNearestMarkers(List<Tuple<Vector3, float>> markerpos, Vector3 gazepos, out Vector3 MinDis, out Vector3 SecondMinDis, out float MinError1, out float MinError2)
    {
        int idx1;
        int idx2;
        List<float> distances = new List<float>();

        for (int i = 0; i < markerpos.Count; i++)

        {
            var marker = markerpos[i];

            float dis = Vector3.Distance(marker.Item1, gazepos);
            distances.Add(dis);
        }
        float dis1 = distances.Min();
        idx1 = distances.IndexOf(dis1);
        MinDis = markerpos[idx1].Item1;
        MinError1 = markerpos[idx1].Item2;

        // find the second min value
        markerpos.RemoveAt(idx1);
        distances.RemoveAt(idx1);
        float dis2 = distances.Min();
        idx2 = distances.IndexOf(dis2);
        SecondMinDis = markerpos[idx2].Item1;
        MinError2 = markerpos[idx2].Item2;
    }



    

    bool CalculateFixation(ValidGaze currGaze, float fixationMaxDispersion, float fixationMinInterval)
    {
        gazeWindowSet.Add(currGaze);

        if (gazeWindowSet.Count > 1)
        {
            Vector3 currRay = gazeWindowSet[gazeWindowSet.Count - 1].gaze_direction_global_combined;

            for (int i = 0; i < (gazeWindowSet.Count - 1); i++)
            {
                Vector3 rayi = gazeWindowSet[i].gaze_direction_global_combined;
                float angle = Mathf.Abs(Vector3.Angle(rayi, currRay));
                if (angle > fixationMaxDispersion)
                {
                    float fixedTime = gazeWindowSet[gazeWindowSet.Count - 2].timestamp - gazeWindowSet[0].timestamp;
                    if (fixedTime > fixationMinInterval)
                    {
                        //// calc the average gaze (origin and direction) among the window set except the last one
                        //Vector3 avgOrigin = Vector3.zero;
                        //Vector3 avgDirection = Vector3.zero;
                        //for (int j = 0; j < gazeWindowSet.Count - 1; j++)
                        //{
                        //    avgOrigin += gazeWindowSet[j].gaze_origin_global_combined;
                        //    avgDirection += gazeWindowSet[j].gaze_direction_global_combined;
                        //}
                        //avgOrigin = avgOrigin / (gazeWindowSet.Count - 1);
                        //avgDirection = avgDirection / (gazeWindowSet.Count - 1);

                        return true;
                    }

                    // if there is one angle bigger tha fixationMaxDispersion, process and reset the window set (clear all except the last one)
                    gazeWindowSet.RemoveRange(0, gazeWindowSet.Count - 1);  // only the last item is left
                    return false;
                }  // if all the angles are smaller than fixationMaxDispersion, do nothing
            }
        }
        return false;
    }

    void RegisterPoints(Vector3 gazeRay, Vector3 camPos, List<Vector3> currentPointCloud, float currentAngleThreshold, float acceptingDepthRange)
    {
        if (!obtainOnlyResult)
        {
            pointWithinRange.Clear();
            pointIndices.Clear();
            float angleThreshold;
            // globalAngleThreshold = 1f;
            angleThreshold = Mathf.Max(globalAngleThreshold, currentAngleThreshold);
            angleThreshold += 0.5f; //
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
                point.z *= -1; // to unity left hand 
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

                }
            }

        }
    }


    //void WritePointCloud(List<Vector3> currentPointCloud, List<float> currentPointGazeImportance, string savefoldername, string savefilename)
    //{
    //    if (!obtainOnlyResult)
    //    {
    //        string HeatMapDir = @"D:\xuemei\HeatMap\";
    //        string SaveFolderName = Path.Combine(HeatMapDir, savefoldername);
    //        string SaveHeatmapDir = Path.Combine(HeatMapDir, savefilename);
    //        string path = SaveHeatmapDir;
    //        if (!System.IO.Directory.Exists(SaveFolderName))
    //            Directory.CreateDirectory(SaveFolderName);
    //        File.WriteAllText(path, string.Empty);
    //        sw = new StreamWriter(path, true);
    //        sw.WriteLine("PosX PosY PosZ GazeCount");
    //        sw.Flush();

    //        for (int i = 0; i < currentPointCloud.Count; i++)
    //        {
    //            sw.WriteLine(currentPointCloud[i].x + " " + currentPointCloud[i].y + " " + currentPointCloud[i].z * (-1) + " " + currentPointGazeImportance[i]);
    //            sw.Flush();

    //        }
    //        sw.Dispose();
    //    }
    //}

    void WritePointCloud(List<Vector3> currentPointCloud, List<float> currentPointGazeImportance, string savefoldername, string savefilename)
    {
        if (!obtainOnlyResult)
        {
            string HeatMapDir = @"D:\xuemei\HeatMap\";
            string SaveFolderName = Path.Combine(HeatMapDir, savefoldername);
            string SaveHeatmapDir = Path.Combine(SaveFolderName, savefilename);
            string path = SaveHeatmapDir;
            if (!System.IO.Directory.Exists(SaveFolderName))
                Directory.CreateDirectory(SaveFolderName);
            if (!File.Exists(path))
            {
                Console.WriteLine("This is the frist time to write data in this frame!");
                File.WriteAllText(path, string.Empty);
                sw = new StreamWriter(path, true);
                sw.WriteLine("[PosX PosY PosZ]");
                sw.WriteLine("GazeCount");
                sw.Flush();

                for (int i = 0; i < currentPointCloud.Count; i++)
                {
                    sw.Write("[" + (currentPointCloud[i].x + "," + currentPointCloud[i].y + "," + currentPointCloud[i].z * (-1)) + "]" + " ");
                    sw.Flush();
                }
                sw.Write("\r\n");
                for (int i = 0; i < currentPointCloud.Count; i++)
                {
                    sw.Write(currentPointGazeImportance[i] + " ");
                    sw.Flush();
                }

                sw.Dispose();
            }
            else
            {
                Console.WriteLine("File already exist!");
                File.AppendAllText(path, string.Empty);
                sw = new StreamWriter(path, true);
                sw.Write("\r\n");

                for (int i = 0; i < currentPointCloud.Count; i++)
                {
                    sw.Write(currentPointGazeImportance[i] + " ");
                    sw.Flush();
                }
                sw.Dispose();


            }
            
        }
    }



    [Serializable]
    public class FullData_cwi
    {
        public CameraMatrix camera_matrix { get; set; }
        public int pointcloudTs { get; set; }
        public EyeDataCwi eye_data_cwi { get; set; }
        public Vector3 gaze_origin_global_combined { get; set; }
        public Vector3 gaze_direction_global_combined { get; set; }
        public string pcname { get; set; }
    }

    public class ValidGaze
    {

        public Vector3 gaze_origin_global_combined { get; set; }
        public Vector3 gaze_direction_global_combined { get; set; }
        public int timestamp { get; set; }
        public Matrix4x4 cameramatrix { get; set; }
        public string filename { get; set; }
    }

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

