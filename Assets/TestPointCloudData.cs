using Cwipc;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;


public class TestPointCloudData : MonoBehaviour
{
    //file reader
    [Header("Files")]
    public StreamReader sr;
    [Space(10)]

    public StreamWriter sw;
    public StreamReader srTransformData;

    private List<Vector3> currentPointCloud = new List<Vector3>();
    private List<Vector3> currentPointCloudRotated = new List<Vector3>();
    private List<float> currentPointGazeImportance = new List<float>();
    private List<Tuple<Vector3, float>> markerpos = new List<Tuple<Vector3, float>>();
    private List<Vector3> pointWithinRange = new List<Vector3>();
    private List<int> pointIndices = new List<int>();

    [Header("RegisterPoints")]
    //processing param
    public float globalAngleThreshold = 1.0f;
    public float acceptingDepthRange = 0.1f;
    public int angleSegments = 10;
    public int slices = 16;
    public int ignoreTime = 400; //Ignore the initial 800ms
    public float fixationDispersionThreshold = 3f;
    public float fixationIntervalThreshold = 50;
    private int deleteTime;
    private int previousIndex = -1;

    public bool obtainOnlyResult = false;

    public PrerecordedPointCloudReader pctReader;
    public PointCloudRenderer pcdRenderer;

    public int LengthOfRay = 25;
    [SerializeField] private LineRenderer GazeRayRenderer;
    public Gradient gradient = new Gradient();


    bool isProcessing = false;

    Vector3 Valid_gaze_origin_world;
    Vector3 Valid_gaze_direction_world;
    // Dir of the collected json file
    string GazeDataDir = @"C:\xuemei\RawData\user_WithRotation\20230315-2306_WithRotation_A.json";
    string userid;
    string session;

   
    string PLYpath = @"F:\PointCloud_Dataset_Binary\";
    string pattern = "Static"; 
    string replace = "PointCloud_Dataset_Binary";
    public float CompensateAngle;
    public bool needWrite;
    public string savefoldername;
    public string save_txt_filename;
    List<ValidGaze> gazeWindowSet = new List<ValidGaze>();

    // Statistics Data 
    float DummyCount = 0;
    float NullCount = 0;
    float EyeDataInvalid = 0;
    float ErrorProfilingCount = 0;
    // For visualizing gaze 
    string dumpDir = @"C:\Static\";
    private Quaternion CurrentRotationAngle;
    private Quaternion PreviousRotationAngle;
    private bool Showpoints;
    int previouspcIndex = -1;
    int currentpcIndex;
    string currentpcName;
    int currentYdegree;
    bool isPressed;
    bool isnotNull;
    private Thread threadProcessGaze;

    void Start()
    {
        isPressed = false;
        Showpoints = false;
        isnotNull = false;
        PreviousRotationAngle = Quaternion.Euler(0, 0, 0);
        CurrentRotationAngle = Quaternion.Euler(0, 0, 0);
        Debug.Log("Initialization: " + PreviousRotationAngle);
        // get userid and session from GazeDataDir
        string jsonfilename = Path.GetFileNameWithoutExtension(GazeDataDir);
        string[] words = jsonfilename.Split('_');
        userid = words[1];
        session = words[2];
        Debug.Log("Current userid is " + userid + " and session is " + session);
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
       
        if (Keyboard.current.spaceKey.isPressed) // must press the spacekey then you can start processing the data? Need to change
        {
            // Spacebar was pressed 
            Debug.Log("Spacebar was pressed !");
            ProcessAsync();
            isPressed = true;
        }
        ShowGaze(Valid_gaze_origin_world, Valid_gaze_direction_world);
        
        if (currentpcIndex != previouspcIndex && isPressed && isnotNull) // first (000) current=previous second currrent!= previous
        {
            isnotNull = false;
            pcdRenderer.transform.rotation = Quaternion.Euler(0, currentYdegree, 0);
            var pointcloudPathname = Path.ChangeExtension(currentpcName, "ply");
            string folderName = Path.GetFileName(Path.GetDirectoryName(pointcloudPathname));
            string pointcloudFilename = Path.GetFileName(pointcloudPathname);
            string currentFilePath = Path.Combine(PLYpath, folderName, pointcloudFilename);

            currentPointCloudRotated = LoadPointCloud(currentFilePath, currentYdegree);
            previouspcIndex = currentpcIndex;
            
            ShowPoints(currentPointCloudRotated);
        }
        


    }


    private void OnDestroy()
    {
        if (threadProcessGaze != null)
        {
            threadProcessGaze.Abort();  // send a signal to end the thread
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
        threadProcessGaze = new Thread(ProcessFunc);
        threadProcessGaze.IsBackground = true;
        threadProcessGaze.Start();
    }

    void ProcessFunc()
    {
        ReadEyeData(GazeDataDir);
        isProcessing = false;
    }


    Dictionary<string, List<Vector3>> namePointList = new Dictionary<string, List<Vector3>>();

    //already rotated the point cloud when load the point cloud 
    List<Vector3> LoadPointCloudReturn(string pc_filename, int rotationAngleYinDegree)
    {
        // whether in temp list
        bool dataExist = namePointList.ContainsKey(pc_filename);

        if (!dataExist)
        {
            // read points
            List<Vector3> pcd = new List<Vector3>();

            if (File.Exists(pc_filename))
            {
                var stream = File.Open(pc_filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                var header = ReadDataHeader(new StreamReader(stream));
                var body = ReadDataBody(header, new BinaryReader(stream));

                for (int i = 0; i < body.vertices.Count; i++)
                {
                    Vector3 v = body.vertices[i];
                    Vector3 v_Rotated = Quaternion.Euler(0, (rotationAngleYinDegree), 0) * v; // 
                    pcd.Add(v_Rotated);
                }
            }
            else
            {
                Debug.LogWarning("currentObject Not Found");
            }

            // count limitation ?? why the limitation  is 3 xuemei??
            if (namePointList.Count > 3)
            {
                var keys = namePointList.Keys.ToList();
                namePointList.Remove(keys[0]);
            }

            // add to list
            namePointList.Add(pc_filename, pcd);
        }

        return namePointList[pc_filename]; // return the pcd 
    }


    List<Vector3> LoadPointCloud(string pc_filename, int rotationAngleYinDegree)
    {
        
            // read points
            List<Vector3> pcd = new List<Vector3>();

            if (File.Exists(pc_filename))
            {
                var stream = File.Open(pc_filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                var header = ReadDataHeader(new StreamReader(stream));
                var body = ReadDataBody(header, new BinaryReader(stream));

                for (int i = 0; i < body.vertices.Count; i++)
                {
                    Vector3 v = body.vertices[i];
                    Vector3 v_Rotated = Quaternion.Euler(0, (rotationAngleYinDegree), 0) * v; // 

                    pcd.Add(v_Rotated);
                }
            }
            

        return pcd; // return the pcd 
    }


    void ResetGazeImportance()
    {
        currentPointGazeImportance.Clear();

        for (int i = 0; i < currentPointCloud.Count; i++)
        {
            currentPointGazeImportance.Add(0f);
        }
    }



    enum DataProperty
    {
        Invalid,
        X, Y, Z,
        Xd, Yd, Zd,
        R, G, B, A,
        Data8, Data16, Data32, Data64
    }

    static int GetPropertySize(DataProperty p)
    {
        //Debug.Log(p);
        switch (p)
        {
            case DataProperty.X: return 4;
            case DataProperty.Y: return 4;
            case DataProperty.Z: return 4;
            case DataProperty.Xd: return 8;
            case DataProperty.Yd: return 8;
            case DataProperty.Zd: return 8;
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
                    case "x": prop = (col[1] == "double") ? DataProperty.Xd : DataProperty.X; break;
                    case "y": prop = (col[1] == "double") ? DataProperty.Yd : DataProperty.Y; break;
                    case "z": prop = (col[1] == "double") ? DataProperty.Zd : DataProperty.Z; break;
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
                else if (col[1] == "double")
                {
                    if (prop == DataProperty.Invalid)
                        prop = DataProperty.Data64;
                    else if (GetPropertySize(prop) != 8)
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

    DataBody ReadDataBody(DataHeader header, BinaryReader reader)  //maybe I can rotate the point cloud from here?
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
                    case DataProperty.Xd: x = (float)reader.ReadDouble(); break;
                    case DataProperty.Yd: y = (float)reader.ReadDouble(); break;
                    case DataProperty.Zd: z = (float)reader.ReadDouble(); break;
                    case DataProperty.Data8: reader.ReadByte(); break;
                    case DataProperty.Data16: reader.BaseStream.Position += 2; break;
                    case DataProperty.Data32: reader.BaseStream.Position += 4; break;
                    case DataProperty.Data64: reader.BaseStream.Position += 8; break;
                }
            }

            data.AddPoint(x, y, z);

        }

        return data;
    }




    void ReadEyeData(string EyeDatapath)
    {
        string dataJsonString = File.ReadAllText(EyeDatapath);
        List<FullData_cwi> fullDataList = JsonConvert.DeserializeObject<List<FullData_cwi>>(dataJsonString);

        string previousFilename = "";

        for (int i = 0; i < fullDataList.Count; i++)
        {
            FullData_cwi datai = fullDataList[i];

            string dump_filename = datai.pcname; //rafa_001.ply
            //if ((datai.pcIndex == 0) || (datai.pcIndex == 1))
            //{
            //    DummyCount += 1;
            //    continue;
            //}
            
            if (string.IsNullOrWhiteSpace(dump_filename))  // invalid pcname
            {
                NullCount += 1;
                continue;
            }
            // for visualizing the rotated point cloud
            currentpcName = dump_filename;
            currentYdegree = datai.rotation_matrix;
            currentpcIndex = datai.pcIndex;
            isnotNull = true;


            int currentIndex = datai.pcIndex; // pcIndex = 2
            if (previousIndex != currentIndex) //if switched to the next point cloud  previousIndex = 1;
            {
                int firstTime = datai.eye_data_cwi.timestamp;
                deleteTime = firstTime + ignoreTime;  //deletetime = 2 first + 400
                previousIndex = currentIndex; //previousIndex = 2
                var folderName_ = Path.GetFileName(Path.GetDirectoryName(dump_filename));
                pctReader.dirName = Path.Combine(dumpDir, folderName_);
                CurrentRotationAngle = Quaternion.Euler(0, datai.rotation_matrix, 0); //In unity the rotation
                Debug.Log("CurrentRoattionAngle  is " + CurrentRotationAngle);
                Debug.Log("Dump File Name is: " + pctReader.dirName);
            }

            int currentTime = datai.eye_data_cwi.timestamp;
            if (currentTime < deleteTime)
            {
                continue;
            }

            // valid combined bit_mask  invalid bit_mask of eye data
            if (datai.eye_data_cwi.verbose_data.combined.eye_data.eye_data_validata_bit_mask != 3)
            {
                EyeDataInvalid += 1;
                continue;
            }
                
            // for showing the gaze
            Valid_gaze_origin_world = datai.gaze_origin_global_combined;
            Valid_gaze_direction_world = datai.gaze_direction_global_combined;
           

            // 1.create a ValidGaze
            ValidGaze gaze = new ValidGaze();
            Matrix4x4 matrix = new Matrix4x4();
            gaze.gaze_origin_global_combined = datai.gaze_origin_global_combined; 
            gaze.gaze_direction_global_combined = datai.gaze_direction_global_combined;
            gaze.timestamp = datai.eye_data_cwi.timestamp;
            gaze.filename = dump_filename;
            CameraMatrix cm = datai.camera_matrix;
            matrix.m00 = cm.e00; matrix.m10 = cm.e10; matrix.m20 = cm.e20; matrix.m30 = cm.e30;
            matrix.m01 = cm.e01; matrix.m11 = cm.e11; matrix.m21 = cm.e21; matrix.m31 = cm.e31;
            matrix.m02 = cm.e02; matrix.m12 = cm.e12; matrix.m22 = cm.e22; matrix.m32 = cm.e32;
            matrix.m03 = cm.e03; matrix.m13 = cm.e13; matrix.m23 = cm.e23; matrix.m33 = cm.e33;
            gaze.cameramatrix = matrix;
            gaze.rotation_matrix = datai.rotation_matrix;

            // 2. is fixation point or not? (velocity based method)
            int flag = CalculateFixation(gaze, fixationDispersionThreshold, fixationIntervalThreshold);

            if (flag == 2 || flag == 1)
            {
                // here, all the elements in gazeWindowSet are fixation gazes,  flag ==2
                // or are grouped but cannot been considered as fixation        flag ==1
                for (int q = 0; q < gazeWindowSet.Count; q++)
                {
                    // load the corresponding gaze data
                    ValidGaze gazeq = gazeWindowSet[q];

                    // 2. load corresponding point cloud, and reset gazeimportance, and csv to get the positions of markers
                    if (previousFilename != gazeq.filename) //first true
                    {
                        // here must load new point cloud, because the weight list should be reset
                        var pointcloudPathname = Path.ChangeExtension(gazeq.filename, "ply");
                        string folderName = Path.GetFileName(Path.GetDirectoryName(pointcloudPathname));
                        string pointcloudFilename = Path.GetFileName(pointcloudPathname);
                        string currentFilePath = Path.Combine(PLYpath, folderName, pointcloudFilename);

                        currentPointCloud = LoadPointCloudReturn(currentFilePath, gazeq.rotation_matrix);
                        // load the corresponding csv file, get the positions of markers
                        string pcFlag = Path.GetFileName(Path.GetDirectoryName(gazeq.filename)); // H1_C1_R1
                        string csvfn = $"*_{userid}_{session}_{pcFlag}_*_experiment_data_metrics.csv";
                        string[] fns = Directory.GetFiles(Path.GetDirectoryName(GazeDataDir), csvfn);
                        if (fns.Length != 1)
                            Debug.LogWarning("GazeMetrics has multiple metric csv file, should only has one csv file for one sequence!");
                        string csv_filename = fns[0];
                        markerpos = getMarkerPositionFromCSV(csv_filename);

                        previousFilename = gazeq.filename;
                    }

                    // all gazes, reset to zero
                    ResetGazeImportance();

                    if (flag == 2)  // should calc weight
                    {
                        // calc CompensateAngle
                        var localPos = gazeq.cameramatrix.inverse.MultiplyPoint(gazeq.gaze_origin_global_combined);
                        var localDir = gazeq.cameramatrix.inverse.MultiplyVector(gazeq.gaze_direction_global_combined);
                        bool gazeInnerCircle = ErrorInterpolation(markerpos, localPos, localDir, out CompensateAngle);

                        if (gazeInnerCircle)
                            ErrorProfilingCount += 1;
                        //// calc salency points, i.e., weights of the points of current point cloud
                        RegisterPoints(gazeq.gaze_direction_global_combined, gazeq.gaze_origin_global_combined, currentPointCloud, CompensateAngle, acceptingDepthRange);

                    }

                    //SaveDataWithWeightCounter(gazeq, flag == 2);  // only save pcd frames that have fixation gazes
                    SaveDataWithWeightCounter(gazeq, true);  // save every pcd frame 
                    // if FLAG = 1, save the gazeq but with all zeros. 
                }

                gazeWindowSet.Clear(); // gazeWindowSet will be empty 
            }
            else if (flag == 0)  // here should add current gaze, is same as next line
            { }                  // if flag ==0, means 1. no enough gaze points; 2 

            // 3. add current gaze
            gazeWindowSet.Add(gaze);  // if flag =1 , gazewindowset will include the last gaze as the first gaze in this windowset.
        }

        // save the last group data
        ValidGaze gazeTemp = new ValidGaze(); 
        SaveDataWithWeightCounter(gazeTemp, true);  // save every pcd frame

        string fileContents =   "DummyCount: " + DummyCount.ToString() + "\n" +
                                "NullCount: " + NullCount.ToString() + "\n" +
                                "EyeDataInvalid: " + EyeDataInvalid.ToString() + "\n" +
                                "ErrorProfilingCount: " + ErrorProfilingCount.ToString();


        // Write to disk
        StreamWriter writer = new StreamWriter(@"D:\xuemei\HeatMap\statistics.txt", true);
        writer.Write(fileContents);
        writer.Close();
        // end
        Debug.Log("process end");
    }


    WeightCounter counter = new WeightCounter();
    ValidGaze previousGaze = new ValidGaze();  //initilization
    void SaveDataWithWeightCounter(ValidGaze gaze, bool save)
    {

        // 5. weight counter, average
        if (gaze.filename != previousGaze.filename) // if corresponding to the same frame, counter ++  and then pick the avergae.
        {
            var weight_temp = counter.Average();
            if (weight_temp != null && save)  //
            {
                // save point cloud and weight, when a group of gazes of gazeq.filename have been processed
                savefoldername = Path.GetFileName(Path.GetDirectoryName(previousGaze.filename));
                save_txt_filename = Path.GetFileName(Path.ChangeExtension(previousGaze.filename, "txt"));
                var pointcloudPathname = Path.ChangeExtension(previousGaze.filename, "ply");
                string folderName = Path.GetFileName(Path.GetDirectoryName(pointcloudPathname));
                string pointcloudFilename = Path.GetFileName(pointcloudPathname);
                string newFilePath = Path.Combine(PLYpath, folderName, pointcloudFilename);
                var previouspcd = LoadPointCloudReturn(newFilePath, previousGaze.rotation_matrix);
                WritePointCloud(previouspcd, weight_temp,
                            savefoldername, save_txt_filename, previousGaze.timestamp);
            }

            // reset counter and previousFilename 
            counter = new WeightCounter();
        }

        previousGaze = gaze;
        counter.Add(currentPointGazeImportance);
    }
    void ShowPoints(List<Vector3> CurrentPointCloudRotated)
    {   
        float particleSize = 0.05f;
        var ps = GetComponent<ParticleSystem>();
        if (ps == null)
            return;
        var particles = new ParticleSystem.Particle[CurrentPointCloudRotated.Count];
        for (int i_points = 0; i_points < particles.Length; i_points++)
        {
            particles[i_points].position = CurrentPointCloudRotated[i_points];
            particles[i_points].startSize = particleSize;
            particles[i_points].startColor = Color.blue;
        }
        ps.SetParticles(particles);
        //ps.Pause();
    }


    /// <summary>
    /// The z value is 1.5 meters, which is correct.
    /// </summary>
    /// <param name="markerpos">the local position and error of marker list</param>
    /// <param name="gazeorigin">the local origin coordinate of gaze</param>
    /// <param name="gazedirection">the local direction of gaze</param>
    /// <returns></returns>
    bool ErrorInterpolation(List<Tuple<Vector3, float>> markerpos, Vector3 gazeorigin, Vector3 gazedirection, out float rstAgnel)
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
        int minIdx1 = int.MaxValue;
        int minIdx2 = int.MaxValue;
        GetNearestMarkersIdx(markerpos, gazepos, out minIdx1, out minIdx2);

        var marker0 = markerpos[0];
        var marker1 = markerpos[minIdx1];
        var marker2 = markerpos[minIdx2];

        // 3. interpolation
        float dist1 = Vector3.Distance(marker0.Item1, gazepos);
        float dist2 = Vector3.Distance(marker1.Item1, gazepos);
        float dist3 = Vector3.Distance(marker2.Item1, gazepos);
        List<float> dis_all = new List<float> { dist1, dist2, dist3 };
        List<float> error_all = new List<float> { marker0.Item2, marker1.Item2, marker2.Item2 };

        rstAgnel = WeightedSum(dis_all, error_all);

        // 4. whether gazepos in inner the marker circle
        // maybe, using cross-product for judgement
        return dist1 <= Vector3.Distance(marker1.Item1, marker0.Item1);
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


    void GetNearestMarkersIdx(List<Tuple<Vector3, float>> markerpos, Vector3 gazepos, out int minIdx1, out int minIdx2)
    {
        List<float> distances = new List<float>();

        // since the center marker is index 0
        for (int i = 0; i < markerpos.Count; i++)
        {
            if (i == 0)
            {
                distances.Add(float.MaxValue);
            }
            else
            {
                float dis = Vector3.Distance(markerpos[i].Item1, gazepos);
                distances.Add(dis);
            }
        }

        float dis1 = distances.Min();
        minIdx1 = distances.IndexOf(dis1);

        // find the second min value
        distances.RemoveAt(minIdx1);
        float dis2 = distances.Min();
        minIdx2 = distances.IndexOf(dis2);
    }




    /// <summary>
    /// 
    /// </summary>
    /// <param name="currGaze"></current gaze point to be considered>
    /// <param name="fixationMaxDispersion"></degree threshold>
    /// <param name="fixationMinInterval"></time threshold>
    /// <returns>flag, 0, 1, 2.
    /// 0: the elements in gazeWindowSet can not been considered as fixation or not, maybe no enough elements, or angles between them with next gaze is small
    /// 1: the elements in gazeWindowSet are grouped, i.e., angles between them with next gaze is bigger than threshold, but time span is too short
    /// 2: the elements in gazeWindowSet are fixation, i.e., angles between them with next gaze is bigger than threshold, and time span is larger than fixationMinInterval
    /// </returns>
    int CalculateFixation(ValidGaze currGaze, float fixationMaxDispersion, float fixationMinInterval)
    {
        // more than 2 gazes, can be considered as fixation point
        if (gazeWindowSet.Count > 1)
        {
            Vector3 currRay = currGaze.gaze_direction_global_combined;  //currRay is the ray of the last gaze point in the WindowSet list

            for (int i = 0; i < gazeWindowSet.Count; i++)
            {
                Vector3 rayi = gazeWindowSet[i].gaze_direction_global_combined;
                float angle = Mathf.Abs(Vector3.Angle(rayi, currRay)); // the angle betwwen last gaze and current gaze

                if (angle > fixationMaxDispersion) //angle is larger than threshold
                {
                    // timespan between the first gaze and the last gaze
                    float fixedTime = gazeWindowSet[gazeWindowSet.Count - 1].timestamp - gazeWindowSet[0].timestamp;
                    if (fixedTime > fixationMinInterval) // timespan larger than threshold
                        return 2;  // all the gaze in the windowset now is fixation point FLAG = 2,  processing a list of gaze in the windowset
                    // if angle judgement is ok, but the fixation time is too short, should be considered as saccade??
                    return 1;
                }
            }
        }
        return 0;
    }

    void RegisterPoints(Vector3 gazeRay, Vector3 camPos, List<Vector3> pointlist, float currentAngleThreshold, float acceptingDepthRange)
    {
        if (!obtainOnlyResult)
        {
            pointWithinRange.Clear();
            pointIndices.Clear();
            float angleThreshold;
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

            for (int i = 0; i < pointlist.Count; i++)
            {
                Vector3 point = pointlist[i];
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
                    Vector3 point = pointlist[j];
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


    void WritePointCloud(List<Vector3> pointList, List<float> currentPointGazeImportance, string savefoldername, string savefilename, int TimeStamp)
    {
        if (!obtainOnlyResult)
        {
            string HeatMapDir = @"D:\xuemei\HeatMap\";
            string SaveFolderName = Path.Combine(HeatMapDir, savefoldername);
            string SaveHeatmapDir = Path.Combine(SaveFolderName, TimeStamp.ToString() + "_" + savefilename);
            string path = SaveHeatmapDir;
            if (!System.IO.Directory.Exists(SaveFolderName))
                Directory.CreateDirectory(SaveFolderName);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
                sw = new StreamWriter(path, true);
                sw.WriteLine("PosX PosY PosZ GazeCount");
                sw.Flush();

                for (int i = 0; i < pointList.Count; i++)
                {
                    sw.WriteLine(pointList[i].x + " " + pointList[i].y + " " + pointList[i].z + " " + currentPointGazeImportance[i]);
                    //sw.Flush();
                }
                sw.Close();
                sw.Dispose();
            }
        }
    }





    [Serializable]
    public class FullData_cwi
    {
        public CameraMatrix camera_matrix { get; set; }
        //public int pointcloudTs { get; set; }
        public EyeDataCwi eye_data_cwi { get; set; }
        public Vector3 gaze_origin_global_combined { get; set; }
        public Vector3 gaze_direction_global_combined { get; set; }
        public string pcname { get; set; }
        public int rotation_matrix { get; set; } // rotation angle y
        public int pcIndex { get; set; } // pcIndex
    }

    public class ValidGaze
    {
        public Vector3 gaze_origin_global_combined { get; set; }
        public Vector3 gaze_direction_global_combined { get; set; }
        public int timestamp { get; set; }
        public Matrix4x4 cameramatrix { get; set; }
        public string filename { get; set; }
        public int rotation_matrix { get; set; } // rotation angle y
        public int pcIndex { get; set; } // pcIndex
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

