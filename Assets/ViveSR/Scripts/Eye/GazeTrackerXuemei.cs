using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using System;
using System.IO;
using Cwipc;


public class GazeTrackerXuemei : MonoBehaviour
{
    private static EyeData_v2 eyeData = new EyeData_v2();
    private bool eye_callback_registered = false;
    private static Matrix4x4 matWorldToCamera;
    private static Matrix4x4 lastCameraMatrix;
    private static string PCName;
    private static string pcname;
    private static string framename;
    private static string pcIndex;
    [Tooltip("The reader for the pointclouds for which we get gaze data")]
    public PrerecordedPointCloudReader pcdReader;
    public PointCloudRenderer pcdRenderer;

    //stats
    public bool writeStats;
    public double interval = 0;
    static int instanceCounter = 0;
    int instanceNumber = instanceCounter++;
    private static System.IO.StreamWriter jstatsStream;
    private static bool initialized = false;
    public static long lasPointCloudTs;
    public string userid = "001";
    public string Session = "A";
    // for verify if the result will be correct or not
    public int LengthOfRay = 25;
    [SerializeField] private LineRenderer GazeRayRenderer; 



    // Unix time
    //Int32 unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

    private void Start()
    {
        //launch calibration? 

        //SRanipal_Eye_API.LaunchEyeCalibration(System.IntPtr.Zero);
        if (!SRanipal_Eye_Framework.Instance.EnableEye)
        {
            enabled = false;
            return;
        }

        if (pcdReader == null)
        {
            Debug.LogError("GazeTrackerXuemei needs a pointcloudreader");
            enabled = false;
            return;
        }

        // get the userid, folder name, vp of current running
        //json file: camera position camera rotation gaze origin gaze direction(global local) timestamp frameid
        // frame id = pcdname + filename
        string dirname = "D:/xuemei/RawData/";

        //init jstats
        // D:/userid//vpcc/EyeData/001_yymmdd.json
        string dirName = Path.Combine(dirname, "EyeData");

        if (!System.IO.Directory.Exists(dirName))
        {
            Directory.CreateDirectory(dirName);
        }

        // 20221218_001_A.json
        string filename = string.Format("{0}_{1}_{2}{3}", DateTime.Now.ToString("yyyyMMdd-HHmm"),userid,Session, ".json");
        Debug.Log("The saved file name is " + filename);

        try
        {
            jstatsStream = new System.IO.StreamWriter(Path.Combine(dirName, filename)); // filename
            string statsLine = "[";
            jstatsStream.WriteLine(statsLine);
            Debug.Log(statsLine);
            initialized = true;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            initialized = false;
        }

    }

    //void InputUserID(string filepath)
    //{
    //    // 001_dancer.json
    //    Debug.Log(filepath);
    //    if (File.Exists(filepath))
    //    {
    //        Debug.Log("File with the same UserID already exists. Please change the UserID in the C# code.");

    //        //  When the same file name is found, we stop playing Unity.
    //        if (UnityEditor.EditorApplication.isPlaying)
    //        {
    //            UnityEditor.EditorApplication.isPlaying = false;
    //        }
    //    }
    //    else
    //    {
    //        writeStats = true;
    //    }
    //}

    private void Update()
    {
        lastCameraMatrix = Camera.main.cameraToWorldMatrix; //  model matrix
        matWorldToCamera = Camera.main.worldToCameraMatrix;
        pcname = pcdReader.dirName; //H1_C1_R1
        framename = pcdRenderer.metadataMostRecentReception.filename;

        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
            SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
        {
            Debug.LogWarning(" Eye tracking framework not working or not supported");
            return;
        }

        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
        {
            SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = true;



        }
        else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }

        Vector3 GazeOriginCombinedLocal, GazeDirectionCombinedLocal;
        if (eye_callback_registered)
        {
            if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
            else return;
        }
        else
        {
            if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
            else return;
        }

        Vector3 GazeDirectionCombined = Camera.main.transform.TransformDirection(GazeDirectionCombinedLocal);
        GazeRayRenderer.SetPosition(0, Camera.main.transform.position - Camera.main.transform.up * 0.05f); //010*0.05
        GazeRayRenderer.SetPosition(1, Camera.main.transform.position + GazeDirectionCombined * LengthOfRay);
        Debug.Log("HTC Vive Origin:" + (Camera.main.transform.position - Camera.main.transform.up * 0.05f));
        //Debug.Log("HTC Vive Direction:" + GazeDirectionCombined);
        Vector3 GazeOriGlobal_L = lastCameraMatrix.MultiplyPoint(Vector3.Scale(eyeData.verbose_data.combined.eye_data.gaze_origin_mm * 0.001f, new Vector3(-1, 1, -1)));
        Vector3 GazeDirGlobal_L = lastCameraMatrix.MultiplyVector(Vector3.Scale(eyeData.verbose_data.combined.eye_data.gaze_direction_normalized, new Vector3(-1, 1, -1)));

        Vector3 GazeOriGlobal_L2 = lastCameraMatrix.MultiplyPoint(Vector3.Scale(eyeData.verbose_data.combined.eye_data.gaze_origin_mm * 0.001f, new Vector3(1, 1, -1)));
        //Debug.DrawLine(GazeOriGlobal_L, GazeDirGlobal_L + GazeDirGlobal_L * LengthOfRay, Color.blue);
        Debug.Log("My Computation Origin -1:" + GazeOriGlobal_L);
        Debug.Log("My Computation Origin 1:" + GazeOriGlobal_L2);
       // Debug.Log("My Computation Direction :" + GazeDirGlobal_L);



    }


    public void OnDestroy()
    {
        SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));

        // TODO
        if (jstatsStream.BaseStream.Length < 1)
        {
            jstatsStream.WriteLine("{} ]");
        }
        else
        {
            jstatsStream.WriteLine(" ]");
        }

        jstatsStream.WriteLine("{} ]");
        jstatsStream.Close();
    }


    private void Release()
    {
        if (eye_callback_registered == true)
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

        // 1. process local to world
        // convert from camera space to world space (mm to meters)
        Vector3 global_o_c = lastCameraMatrix.MultiplyPoint(Vector3.Scale(edata.verbose_data.combined.eye_data.gaze_origin_mm * 0.001f, new Vector3(1, 1, -1)));
        Vector3 global_d_c = lastCameraMatrix.MultiplyVector(Vector3.Scale(edata.verbose_data.combined.eye_data.gaze_direction_normalized, new Vector3(-1, 1, -1)));

        Vector3 global_o_l = lastCameraMatrix.MultiplyPoint(Vector3.Scale(edata.verbose_data.left.gaze_origin_mm * 0.001f, new Vector3(-1, 1, -1)));
        Vector3 global_d_l = lastCameraMatrix.MultiplyVector(Vector3.Scale(edata.verbose_data.left.gaze_direction_normalized, new Vector3(-1, 1, -1)));

        Vector3 global_o_r = lastCameraMatrix.MultiplyPoint(Vector3.Scale(edata.verbose_data.right.gaze_origin_mm * 0.001f, new Vector3(-1, 1, -1)));
        Vector3 global_d_r = lastCameraMatrix.MultiplyVector(Vector3.Scale(edata.verbose_data.right.gaze_direction_normalized, new Vector3(-1, 1, -1)));
        Vector3 mean_l_r_o = (global_o_l + global_o_r) * 0.5f;
        Vector3 mean_l_r_d = (global_d_l + global_d_r) * 0.5f;
        // this should be viewing and projection matrix
        //or if I can save this matrix, then I can do post processing But I feel directly get these information here is better
        // Matrix4x4 VP_matrix = Camera.main.previousViewProjectionMatrix * Camera.main.worldToCameraMatrix;
        FullData_cwi fData = new FullData_cwi()
        {
            camera_matrix = lastCameraMatrix,
            pointcloudTs = lasPointCloudTs,
            eye_data_cwi = edata,
            gaze_origin_global_combined = global_o_c,
            gaze_direction_global_combined = global_d_c,
            gaze_origin_global_left = global_o_l,
            gaze_direction_global_left = global_d_l,
            gaze_origin_global_right = global_o_r,
            gaze_direction_global_right = global_d_r,
            pcname = framename,
            mean_l_r_o = mean_l_r_o,
            mean_l_r_d = mean_l_r_d,
            head_position = lastCameraMatrix.GetColumn(3),
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
                //jstatsStream.Flush()
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
    /// Structure containing all informationa bout the eye_tracking_data
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
        public Vector3 gaze_origin_global_left;
        public Vector3 gaze_direction_global_left;
        public Vector3 gaze_origin_global_right;
        public Vector3 gaze_direction_global_right;
        //public Vector3 ViewPos;
        //public Vector3 ViewPos2;
        public Vector3 mean_l_r_o;
        public Vector3 mean_l_r_d;
        public Vector4 head_position;
        public string pcname;
    }
    // left right combined 



    //LeftVisual = new GameObject("Left Gaze Ray Visual");


    //public void RenderGazeRays(LeftGazeRays)
    //{
    //    LineRenderer lr = 

    //}



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

    //public class FrameMetadata
    //{
    //    public string filename;
    //}

}
