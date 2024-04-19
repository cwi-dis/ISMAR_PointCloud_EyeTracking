using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using System;
using System.IO;


public class OutPutData : MonoBehaviour
{
    private static EyeData_v2 eyeData = new EyeData_v2();
    private bool eye_callback_registered = false;

    public static Matrix4x4 lastCameraMatrix;
    //public static Matrix4x4 lastWorld2ViewportMatrix;
    public static long lasPointCloudTs;
    public static string UserID = "001";       // Definte ID number such as 001, ABC001, etc.
    public static string Path = Directory.GetCurrentDirectory();
    public static string _DateTime = DateTime.Now.ToString("yyyyMMdd-HHmm");
    // Unix time
    Int32 unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    string File_Path = Directory.GetCurrentDirectory() + "/EyeData" + _DateTime + UserID + ".txt";

    // ********************************************************************************************************************
    //
    //  Parameters for time-related information.
    //
    // ********************************************************************************************************************
    public static int cnt_callback = 0;
    private static float time_stamp;
    private static int frame;

    // ********************************************************************************************************************
    //
    //  Parameters for eye data.
    //
    // ********************************************************************************************************************
    public EyeParameter eye_parameter = new EyeParameter();
    public GazeRayParameter gaze = new GazeRayParameter();
 

    // private const int maxframe_count = 120 * 300;                        // Maximum number of samples for eye tracking (120 Hz * time in seconds).
    private static UInt64 eye_valid_L, eye_valid_R, eye_valid_C;            // The bits explaining the validity of eye data.
    //private static float openness_L, openness_R;                            // The level of eye openness.
    // static float pupil_diameter_L, pupil_diameter_R;                // Diameter of pupil dilation.
    //private static Vector2 pos_sensor_L, pos_sensor_R;                        // Positions of pupils.
    private static Vector3 gaze_origin_L, gaze_origin_R, gaze_origin_C;             // Position of gaze origin.
    private static Vector3 gaze_direct_L, gaze_direct_R, gaze_direct_C;            // Direction of gaze ray.
    //private static float frown_L, frown_R;                          // The level of user's frown.
    //private static float squeeze_L, squeeze_R;                      // The level to show how the eye is closed tightly.
    //private static float wide_L, wide_R;                            // The level to show how the eye is open widely.
    private static double gaze_sensitive;                           // The sensitive factor of gaze ray.
    private static float distance_C;                                // Distance from the central point of right and left eyes.
    private static bool distance_valid_C;                           // Validity of combined data of right and left eyes.
    public bool cal_need;                                           // Calibration judge.
    public bool result_cal;                                         // Result of calibration.
    private static int track_imp_cnt = 0;
    private static TrackingImprovement[] track_imp_item;
    private void Start()
    {
        //launch calibration? 

        //SRanipal_Eye_API.LaunchEyeCalibration(System.IntPtr.Zero);
        if (!SRanipal_Eye_Framework.Instance.EnableEye)
        {
            enabled = false;
            return;
        }
        Data_txt();

    }

    void Data_txt()
    {
        string variable =
        "time(100ns)" + "   " +
        "time_stamp(ms)" + "    " +
        "frame" + " " +
        "eye_valid_L" + "   " +
        "eye_valid_R" + "   " +
        "eye_valid_C" + "   " +
        "gaze_origin_L.x(mm)" + "   " +
        "gaze_origin_L.y(mm)" + "   " +
        "gaze_origin_L.z(mm)" + "   " +
        "gaze_origin_R.x(mm)" + "   " +
        "gaze_origin_R.y(mm)" + "   " +
        "gaze_origin_R.z(mm)" + "   " +
        "gaze_origin_C.x(mm)" + "   " +
        "gaze_origin_C.y(mm)" + "   " +
        "gaze_origin_C.z(mm)" + "   " +
        "gaze_direct_L.x" + "   " +
        "gaze_direct_L.y" + "   " +
        "gaze_direct_L.z" + "   " +
        "gaze_direct_R.x" + "   " +
        "gaze_direct_R.y" + "   " +
        "gaze_direct_R.z" + "   " +
        "gaze_direct_C.x" + "   " +
        "gaze_direct_C.y" + "   " +
        "gaze_direct_C.z" + "   " +
        Environment.NewLine;

        File.AppendAllText("/EyeData" + _DateTime + UserID + ".txt", variable);
    }

    private void Update()
    {
        lastCameraMatrix = Camera.main.cameraToWorldMatrix; //  model matrix
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

    }

    private static void EyeCallback(ref EyeData_v2 eye_data)
    {
        EyeParameter eye_parameter = new EyeParameter();
        SRanipal_Eye_API.GetEyeParameter(ref eye_parameter);
        eyeData = eye_data;
        while (true)
        {
            ViveSR.Error error = SRanipal_Eye_API.GetEyeData_v2(ref eyeData);

            if (error == ViveSR.Error.WORK)
            {
                // --------------------------------------------------------------------------------------------------------
                //  Measure each parameter of eye data that are specified in the guideline of SRanipal SDK.
                // --------------------------------------------------------------------------------------------------------
                time_stamp = eyeData.timestamp;
                frame = eyeData.frame_sequence;
                eye_valid_L = eyeData.verbose_data.left.eye_data_validata_bit_mask;
                eye_valid_R = eyeData.verbose_data.right.eye_data_validata_bit_mask;
                eye_valid_C = eyeData.verbose_data.combined.eye_data.eye_data_validata_bit_mask;
                gaze_origin_L = eyeData.verbose_data.left.gaze_origin_mm;
                gaze_origin_R = eyeData.verbose_data.right.gaze_origin_mm;
                gaze_origin_C = eyeData.verbose_data.combined.eye_data.gaze_origin_mm;
                gaze_direct_L = eyeData.verbose_data.left.gaze_direction_normalized;
                gaze_direct_R = eyeData.verbose_data.right.gaze_direction_normalized;
                gaze_direct_C = eyeData.verbose_data.combined.eye_data.gaze_direction_normalized;
                gaze_sensitive = eye_parameter.gaze_ray_parameter.sensitive_factor;
                distance_valid_C = eyeData.verbose_data.combined.convergence_distance_validity;
                distance_C = eyeData.verbose_data.combined.convergence_distance_mm;
                track_imp_cnt = eyeData.verbose_data.tracking_improvements.count;

                //  Convert the measured data to string data to write in a text file.
                string value =
                    time_stamp.ToString() + "   " +
                    frame.ToString() + "    " +
                    eye_valid_L.ToString() + "  " +
                    eye_valid_R.ToString() + "  " +
                    eye_valid_C.ToString() + "  " +
                    gaze_origin_L.x.ToString() + "  " +
                    gaze_origin_L.y.ToString() + "  " +
                    gaze_origin_L.z.ToString() + "  " +
                    gaze_origin_R.x.ToString() + "  " +
                    gaze_origin_R.y.ToString() + "  " +
                    gaze_origin_R.z.ToString() + "  " +
                    gaze_origin_C.x.ToString() + "  " +
                    gaze_origin_C.y.ToString() + "  " +
                    gaze_origin_C.z.ToString() + "  " +
                    gaze_direct_L.x.ToString() + "  " +
                    gaze_direct_L.y.ToString() + "  " +
                    gaze_direct_L.z.ToString() + "  " +
                    gaze_direct_R.x.ToString() + "  " +
                    gaze_direct_R.y.ToString() + "  " +
                    gaze_direct_R.z.ToString() + "  " +
                    gaze_direct_C.x.ToString() + "  " +
                    gaze_direct_C.y.ToString() + "  " +
                    gaze_direct_C.z.ToString() + "  " +
                    gaze_sensitive.ToString() + "   " +
                    distance_valid_C.ToString() + " " +
                    distance_C.ToString() + "   " +
                    track_imp_cnt.ToString() +
                    Environment.NewLine;

                    File.AppendAllText("/EyeData" + _DateTime + UserID + ".txt", value);

                cnt_callback++;
            }
        }
    }


    private void Release()
    {
        if (eye_callback_registered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }
    }





   
}
