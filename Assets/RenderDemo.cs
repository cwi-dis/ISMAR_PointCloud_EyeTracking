using Cwipc;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using System;
using System.Collections;
using System.IO;
using Random = System.Random;

public class RenderDemo : MonoBehaviour
{
    [Tooltip("pointcloudPlayback for preload the path")]
    public PointCloudPlayback pointcloudPlayback;

    [Tooltip("Input the folder name for the sequence")]
    public List<string> pc_paths;

    private static EyeData_v2 eyeData = new EyeData_v2();
    private bool eye_callback_registered = false;
    private static Matrix4x4 localToWorld;
    private static string framename;
    private static int RotationAngleY; // why this need to be static?
    private static int currentIdx = 0;
    public string RandomOrderPath;


    //stats
    public double interval = 0;
    private static System.IO.StreamWriter jstatsStream;
    private static bool initialized = false;
    public static long lasPointCloudTs;
    // for verify if the result will be correct or not
    public int LengthOfRay = 25;
    public string pc_folder_name;


    [Tooltip("Get the mainControl and Randomizer")]
    private MainControllerDemo mainControl;


    [SerializeField]
    private bool ShowLineForGaze = true;

    private LineRenderer GazeRayRendererLeft;
    private LineRenderer GazeRayRendererRight;
    private LineRenderer GazeRayRendererCombined;

    private static Vector3 gazeOriginWorldLeft;
    private static Vector3 gazeDirectionWorldLeft;
    private static Vector3 gazeOriginWorldRight;
    private static Vector3 gazeDirectionWorldRight;
    private static Vector3 gazeOriginWorldCombined;
    private static Vector3 gazeDirectionWorldCombined;


    public event Action<string, int> OnCurrDirPathUpdated;



    private void Awake()
    {
        ReadPointCloudPath(RandomOrderPath);
        // all valid pc_paths
        if (pc_paths.Count == 0)
        {
            Debug.LogError("Need to load the point cloud!");
            UnityEditor.EditorApplication.isPlaying = false;
        }

        foreach (string pcdir in pc_paths)
        {
            if (!System.IO.Directory.Exists(pcdir))
            {
                Debug.LogError(string.Format("Path: [{0}] does not exists!", pcdir));
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }

        mainControl = FindObjectOfType<MainControllerDemo>();
        if (mainControl == null)
        {
            Debug.LogError("Can not get a valid object of MainController!");
        }

        pointcloudPlayback = FindObjectOfType<PointCloudPlayback>();
        if (mainControl == null)
        {
            Debug.LogError("Can not get a valid object of PointCloudPlayback!");
        }


        // init reader and renderer
        UpdateDirPath(pc_paths[currentIdx], RotationAngleY, currentIdx);
        Debug.Log("RenderControler currentIndex is " + currentIdx);

        // visualize the ray
        GazeRayRendererLeft = InitGameObjAndLineRender("left");
        GazeRayRendererRight = InitGameObjAndLineRender("right");
        GazeRayRendererCombined = InitGameObjAndLineRender("combined");
    }

    private void OnEnable()
    {
        RegisterEyeTracker();
    }


    private void OnDisable()
    {
        ReleaseEyeTracker();
    }

    private LineRenderer InitGameObjAndLineRender(string eye)
    {
        GameObject obj = new GameObject($"Gaze{eye}");
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.SetParent(Camera.main.transform);

        LineRenderer lr = obj.AddComponent<LineRenderer>();

        lr.startWidth = 0.005f;
        lr.endWidth = 0.005f;
        lr.material = new Material(Shader.Find("Sprites/Default"));

        return lr;
    }

    private List<string> ReadPointCloudPath(string user_1_s1)
    {
        string filepath = user_1_s1;
        string pc_dir = @"C:\Demo\LOOT_DUMP\"; //frmo the demo path
        string pc_folder;
        StreamReader reader = null;
        if (File.Exists(filepath))
        {
            reader = new StreamReader(File.OpenRead(filepath));
            string headerline = reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                pc_folder = values[1];
                pc_paths.Add(Path.Combine(pc_dir, pc_folder));
            }
        }
        return pc_paths;
    }



    public string GetCurrentPcdPath()
    {
        return pc_paths[currentIdx];
    }

    public int GetCurrentpcIndex()
    {
        return currentIdx;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!SRanipal_Eye_Framework.Instance.EnableEye)
        {
            enabled = false;
            return;
        }


        // gaze data file path
       
        string dataSavePath = mainControl.dataSaveDir;
        if (!System.IO.Directory.Exists(dataSavePath))
            Directory.CreateDirectory(dataSavePath);

        string filename = string.Format("{0}_{1}_{2}{3}", DateTime.Now.ToString("yyyyMMdd-HHmm"), mainControl.userid, mainControl.Session, ".json");
        Debug.Log("Gaze Data file name is: " + filename);
        try
        {
            jstatsStream = new System.IO.StreamWriter(Path.Combine(dataSavePath, filename)); // filename
            string statsLine = "[";
            jstatsStream.WriteLine(statsLine);
            initialized = true;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            initialized = false;
        }


    }

    // Update is called once per frame
    void Update()
    {

        //lastCameraMatrix = Camera.main.cameraToWorldMatrix; //  model matrix
        localToWorld = Camera.main.transform.localToWorldMatrix;
        framename = pointcloudPlayback.cur_renderer.metadataMostRecentReception.filename;  // how to get the filename??

        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
            SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
        {
            Debug.LogWarning(" Eye tracking framework not working or not supported");
            return;
        }

        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true)
        {
            RegisterEyeTracker();
        }
        else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false)
        {
            ReleaseEyeTracker();
        }


        if (ShowLineForGaze)
        {
           
            GazeRayRendererCombined.SetPosition(0, gazeOriginWorldCombined); //010*0.05
            GazeRayRendererCombined.SetPosition(1, gazeOriginWorldCombined + gazeDirectionWorldCombined * LengthOfRay);
            GazeRayRendererCombined.startColor = Color.blue;
            GazeRayRendererCombined.endColor = Color.blue;
            GazeRayRendererCombined.gameObject.transform.position = gazeOriginWorldCombined;
        }
        else
        {
            GazeRayRendererLeft.gameObject.SetActive(false);
            GazeRayRendererRight.gameObject.SetActive(false);
            GazeRayRendererCombined.gameObject.SetActive(false);
        }

    }


    public bool RenderNext()
    {
        currentIdx = currentIdx + 1;
        if (currentIdx > (pc_paths.Count - 1))
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return false;
        }

        string renderPath = pc_paths[currentIdx];
        // added by xuemei 2023.2.19
        int y_degree = UnityEngine.Random.Range(0, 180);
        pointcloudPlayback.transform.rotation = Quaternion.Euler(0, y_degree, 0); // Test if the camera face to the center of the object! added byxuemei 
        Debug.Log("Currrent Point Cloud is:" + renderPath + currentIdx);
        UpdateDirPath(renderPath, y_degree, currentIdx);

        return true;
    }


    public void SetRenderActive(bool value)
    {
        this.gameObject.SetActive(value);
    }


    public void UpdateDirPath(string dirpath, int RotationAngleYinDegree, int currentIdx)
    {
        bool activeState = this.gameObject.activeSelf;
        Debug.Log("Now render Obj is:" + activeState);

        // first set to false before giving the dir if true
        if (activeState)
        {
            this.gameObject.SetActive(false);
        }


        pc_folder_name = dirpath;
        //initilization for the first folder path added by xuemei 2023.3.1
        pointcloudPlayback.dirName = dirpath;
        // TODO: release the old pct reader, renderer
        RotationAngleY = RotationAngleYinDegree;

        this.gameObject.SetActive(activeState);


        if (OnCurrDirPathUpdated != null)
        {
            OnCurrDirPathUpdated(dirpath, currentIdx);
        }

        //mainControl.NewSequenceHasStarted();
    }


    public void OnDestroy()
    {
        ReleaseEyeTracker();

        // added by xuemei.zyk, 2022-12-30
        // as there is always an error: writeline in func Output(string s) after jstatsStream closed
        // so, set the initialized to false here
        initialized = false;

        //TODO
        if (jstatsStream.BaseStream.Length <= 1)
        {
            jstatsStream.WriteLine("{} ]");
        }
        else
        {
            jstatsStream.WriteLine(" ]");
        }
        jstatsStream.Close();
    }

    private void RegisterEyeTracker()
    {
        if (!eye_callback_registered)
        {
            SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = true;
        }

    }

    private void ReleaseEyeTracker()
    {
        if (eye_callback_registered)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }
    }


    private static void EyeCallback(ref EyeData_v2 eye_data)
    {
        eyeData = eye_data;
        object ed = eyeData;
        EyeData_v2_cwi edata = (EyeData_v2_cwi)CastTo(ref ed, typeof(EyeData_v2_cwi));

        // TODO, different with the sample of SRanipal, don't know why
        Vector3 global_o_c = localToWorld.MultiplyPoint(Vector3.Scale(edata.verbose_data.combined.eye_data.gaze_origin_mm * 0.001f, new Vector3(-1, 1, 1)));
        Vector3 global_d_c = localToWorld.MultiplyVector(Vector3.Scale(edata.verbose_data.combined.eye_data.gaze_direction_normalized, new Vector3(-1, 1, 1)));

        gazeOriginWorldCombined = global_o_c;
        gazeDirectionWorldCombined = global_d_c;

        // this should be viewing and projection matrix
        //or if I can save this matrix, then I can do post processing But I feel directly get these information here is better
        // Matrix4x4 VP_matrix = Camera.main.previousViewProjectionMatrix * Camera.main.worldToCameraMatrix;
        FullData_cwi fData = new FullData_cwi()
        {
            camera_matrix = localToWorld,
            pointcloudTs = lasPointCloudTs,
            eye_data_cwi = edata,
            gaze_origin_global_combined = global_o_c,
            gaze_direction_global_combined = global_d_c,
            pcname = framename,
            rotation_matrix = RotationAngleY,
            pcIndex = currentIdx,
        };
        Output(JsonUtility.ToJson(fData));
    }

    static object lockObj = new object();
    static int seq = 0;

    // statis method, for use when only one or two stats lines are produced.
    public static void Output(string s)
    {
        if (!initialized) return;
        lock (lockObj)
        {
            seq++;
            string jstatsLine = s + ",";
            if (jstatsStream == null)
            {
                Debug.Log(jstatsLine);
            }
            else
            {
                jstatsStream.WriteLine(jstatsLine);
                jstatsStream.Flush();
            }
        }
    }


    public static object CastTo(ref object obj, Type type)
    {
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(type));
        try
        {
            Marshal.StructureToPtr(obj, ptr, false);
            return Marshal.PtrToStructure(ptr, type);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    /// <summary>
    /// Structure containing all informationa about the eye_tracking_data
    /// </summary>
    [Serializable]
    public struct FullData_cwi
    {
        /// <summary>
        /// Most recent camera matrix when the eyeTracking sample was captured
        /// </summary>
        public Matrix4x4 camera_matrix;
        /// <summary>
        /// latest displayed pointcloud timestamp
        /// </summary>
        public long pointcloudTs;
        /// <summary>
        /// Eye tracking data structure
        /// </summary>
        public EyeData_v2_cwi eye_data_cwi;
        /// <summary>
        /// gaze point on the viewport
        /// </summary>
        public Vector2 gazepoint_viewport;
        public Vector3 gaze_origin_global_combined;
        public Vector3 gaze_direction_global_combined;
        public string pcname;
        public int rotation_matrix;
        public int pcIndex;
    }



    [Serializable]
    public struct EyeData_v2_cwi
    {
        /** Indicate if there is a user in front of HMD. */
        public bool no_user;
        /** The frame sequence.*/
        public int frame_sequence;
        /** The time when the frame was capturing. in millisecond.*/
        public int timestamp;
        /** The coordinates on the viewport of the gaze data*/
        public VerboseData_cwi verbose_data;
        public EyeExpression_cwi expression_data;
    }

    [Serializable]
    public struct VerboseData_cwi
    {
        /** A instance of the struct as @ref EyeData related to the left eye*/
        public SingleEyeData_cwi left;
        /** A instance of the struct as @ref EyeData related to the right eye*/
        public SingleEyeData_cwi right;
        /** A instance of the struct as @ref EyeData related to the combined eye*/
        public CombinedEyeData_cwi combined;
        public TrackingImprovements tracking_improvements;
    }

    [Serializable]
    public struct SingleEyeData_cwi
    {
        /** The bits containing all validity for this frame.*/
        public System.UInt64 eye_data_validata_bit_mask;
        /** The point in the eye from which the gaze ray originates in millimeter.(right-handed coordinate system)*/
        public Vector3 gaze_origin_mm;
        /** The normalized gaze direction of the eye in [0,1].(right-handed coordinate system)*/
        public Vector3 gaze_direction_normalized;
        /** The diameter of the pupil in millimeter*/
        public float pupil_diameter_mm;
        /** A value representing how open the eye is.*/
        public float eye_openness;
        /** The normalized position of a pupil in [0,1]*/
        public Vector2 pupil_position_in_sensor_area;

        public bool GetValidity(SingleEyeDataValidity_cwi validity)
        {
            return (eye_data_validata_bit_mask & (ulong)(1 << (int)validity)) > 0;
        }
    }

    [Serializable]
    public struct CombinedEyeData_cwi
    {
        public SingleEyeData_cwi eye_data;
        public bool convergence_distance_validity;
        public float convergence_distance_mm;
    }

    [Serializable]
    public enum SingleEyeDataValidity_cwi : int
    {
        /** The validity of the origin of gaze of the eye data */
        SINGLE_EYE_DATA_GAZE_ORIGIN_VALIDITY,
        /** The validity of the direction of gaze of the eye data */
        SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY,
        /** The validity of the diameter of gaze of the eye data */
        SINGLE_EYE_DATA_PUPIL_DIAMETER_VALIDITY,
        /** The validity of the openness of the eye data */
        SINGLE_EYE_DATA_EYE_OPENNESS_VALIDITY,
        /** The validity of normalized position of pupil */
        SINGLE_EYE_DATA_PUPIL_POSITION_IN_SENSOR_AREA_VALIDITY
    };

    [Serializable]
    public struct EyeExpression_cwi
    {
        public SingleEyeExpression_cwi left;
        public SingleEyeExpression_cwi right;
    };

    [Serializable]
    public struct SingleEyeExpression_cwi
    {
        public float eye_wide; /*!<A value representing how open eye widely.*/
        public float eye_squeeze; /*!<A value representing how the eye is closed tightly.*/
        public float eye_frown; /*!<A value representing user's frown.*/
    };
}




