using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEditor;
using ViveSR.anipal.Eye;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.XR.Interaction.Toolkit;

namespace GazeMetrics
{
    public class CustomDemo : MonoBehaviour
    {

        [Header("Scene References")]
        public new Camera camera;
        public Transform marker;

        [Header("Settings")]
        public GazeMetricsSettings settings;
        public GazeMetricsTargets targets;

        [Header("RightHand Controller")]
        public ActionBasedController rightHandController;

        public bool showPreview;

        public bool IsCalibrating { get { return calibration.IsCalibrating; } }


        //[SerializeField] private LineRenderer GazeRayRenderer;
        private static EyeData_v2 _gazeData = new EyeData_v2();
        private bool eye_callback_registered = false;


        //events
        public event Action OnCalibrationStarted;
        public event Action OnCalibrationRoutineDone;
        public event Action OnCalibrationFailed;
        public event Action OnCalibrationSucceeded;
        public event Action<TargetMetrics> OnMetricsCalculated;

        //members
        GazeMetricsBase calibration = new GazeMetricsBase();

        int targetIdx;
        int targetSampleCount;
        Vector3 currLocalTargetPos;

        float tLastSample = 0;
        float tLastTarget = 0;
        List<GameObject> previewMarkers = new List<GameObject>();

        bool markersInitialized = false;
        bool _isSampleExcluded;


        private string Name { get { return GetType().Name; } }


        private MainControllerDemo mainControl;
        private RenderDemo renderControl;
        private string dataOutputDir;
        private string experimentID;


        private bool finished_calibration = false;
        public bool Finished_calibration
        {
            get => finished_calibration;
            private set => finished_calibration = value;
        }


        public void FinishedCalibrating()
        {
            if (!this.finished_calibration)
            {
                // save rating data pc_id
                Finished_calibration = true;
            }

        }

        void Awake()
        {
            Debug.Log($"{this.Name}: Awake()");


            mainControl = FindObjectOfType<MainControllerDemo>();
            if (mainControl == null)
            {
                Debug.LogError("Can not get a valid object of MainController!");
            }

          

            if (!SRanipal_Eye_Framework.Instance.EnableEye)
            {
                enabled = false;
            }



        }


        void OnEnable()
        {
            Finished_calibration = false; // first set the calibration state to false
            Debug.Log($"{this.Name}: OnEnable()");

            calibration.OnCalibrationSucceeded += CalibrationSucceeded;
            calibration.OnCalibrationFailed += CalibrationFailed;
            calibration.OnMetricsCalculated += MetricsCalcuated;

            if (marker == null || camera == null || settings == null || targets == null)
            {
                Debug.LogWarning("Required components missing.");
                enabled = false;
                return;
            }

            Time.fixedDeltaTime = (float)1 / settings.samplingRate;  // set the frame rate of FixedUpdate()
            InitPreviewMarker();
            var sranipal = CustomCalGazeMetric.FindObjectOfType<SRanipal_Eye_Framework>();
            sranipal.StartFramework();
        }


        void Start()
        {
            renderControl = FindObjectOfType<RenderDemo>();
            if (renderControl == null)
            {
                Debug.LogError("Can not get a valid object of RenderController!");
            }

            Debug.Log($"{this.Name}: Start()");

            dataOutputDir = mainControl.dataSaveDir;
            string pcdpath = renderControl.GetCurrentPcdPath();
            int pcIndex = renderControl.GetCurrentpcIndex();
            OnCurrDirPathUpdated(pcdpath, pcIndex);
            renderControl.OnCurrDirPathUpdated += this.OnCurrDirPathUpdated;
        }

        private void OnCurrDirPathUpdated(string dirpath, int curentPCindex)
        {
            // get "H5_C1_R5" from "E:\DUMP\H5_C1_R5"
            string pcdName = Path.GetFileName(dirpath);
            int pcIndex = curentPCindex;
            //experimentID = string.Format("{0}_{1}_{2}_{3}", DateTime.Now.ToString("yyyyMMdd-HHmm"), mainControl.userid, mainControl.Session, pcdName);
            experimentID = string.Format("{0}_{1}_{2}_{3}_{4}", DateTime.Now.ToString("yyyyMMdd-HHmm"), mainControl.userid, mainControl.Session, pcdName, pcIndex);
        }


        void OnDisable()
        {
            Debug.Log($"{this.Name}: OnDisable()");
            calibration.OnCalibrationSucceeded -= CalibrationSucceeded;
            calibration.OnCalibrationFailed -= CalibrationFailed;

            if (calibration.IsCalibrating)
            {
                StopCalibration();
            }
        }


        void Update()
        {
            SetPreviewMarkers(showPreview);
            ////how to make sure the user must do all the calibration then stop???
            ///mainControl.nextStageAction.action.triggered
            if (rightHandController.activateAction.action.triggered)
            {

                if (!marker.gameObject.activeInHierarchy)
                {
                    ToggleCalibration();
                }

            }




        }



        void FixedUpdate()
        {
            if (calibration.IsCalibrating)
            {
                UpdateCalibration();

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
        }

        public void ToggleCalibration()
        {
            if (calibration.IsCalibrating)
            {
                StopCalibration();
            }
            else
            {
                StartCalibration();
            }
        }

        public void StartCalibration()
        {
            if (!enabled)
            {
                Debug.LogWarning("Component not enabled!");
                return;
            }


            Debug.Log($"{this.Name}: StartCalibration()");
            //Debug.Log((_gazeProvider.GetType().ToString()));

            // not show all markers
            showPreview = false;
            SetPreviewMarkers(showPreview);

            // set marker to show
            targetIdx = 0;
            targetSampleCount = 0;

            UpdatePosition();

            marker.gameObject.SetActive(true);

            calibration.StartCalibration(settings);
            Debug.Log($"Sample Rate: {settings.samplingRate}");

            if (OnCalibrationStarted != null)
            {
                OnCalibrationStarted();
            }
        }

        public void StopCalibration()
        {
            if (!calibration.IsCalibrating)
            {
                Debug.Log("Nothing to stop.");
                return;
            }

            calibration.StopCalibration(dataOutputDir, experimentID);

            marker.gameObject.SetActive(false);
            SetPreviewMarkers(false);
            FinishedCalibrating();

            if (OnCalibrationRoutineDone != null)
            {
                OnCalibrationRoutineDone();
            }

            //subsCtrl.OnDisconnecting -= StopCalibration;
        }

        void OnApplicationQuit()
        {
            //calibration.Destroy();
        }

        private void UpdateCalibration()
        {
            UpdateMarker();

            float tNow = Time.time;
            // if (tNow - tLastSample >= 1f / settings.SampleRate - Time.deltaTime / 2f)
            // {
            _isSampleExcluded = false;
            if (tNow - tLastTarget < settings.ignoreInitialSeconds - Time.deltaTime / 2f)
            {
                _isSampleExcluded = true;
            }

            tLastSample = tNow;

            //Adding the calibration reference data to the list that will be passed on, once the required sample amount is met.

            AddSample();

            targetSampleCount++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

            if (tNow - tLastTarget >= settings.secondsPerTarget)
            {
                calibration.SendCalibrationReferenceData();

                if (targetIdx < targets.GetTargetCount())
                {
                    targetSampleCount = 0;

                    UpdatePosition();
                }
                else
                {
                    StopCalibration();
                }
            }
            // }
        }

        private void CalibrationSucceeded()
        {
            if (OnCalibrationSucceeded != null)
            {
                OnCalibrationSucceeded();
            }
        }

        private void CalibrationFailed()
        {
            if (OnCalibrationFailed != null)
            {
                OnCalibrationFailed();
            }
        }

        private void MetricsCalcuated()
        {
            if (OnMetricsCalculated != null)
            {
                OnMetricsCalculated(new TargetMetrics());
            }
        }


        private Vector3 _previousGazeDirection;
        private void AddSample()
        {
            SampleData pointData = new SampleData();

            pointData.timeStamp = _gazeData.timestamp;
            Vector3 tmp1; Vector3 tmp2;
            pointData.isValid = SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out tmp1, out tmp2);
            //pointData.isValid = (_gazeData.verbose_data.combined.eye_data.eye_data_validata_bit_mask == 3);// ask zyk
            pointData.exclude = _isSampleExcluded;
            pointData.targetId = targetIdx;
            pointData.localMarkerPosition = currLocalTargetPos;
            pointData.worldMarkerPosition = marker.position;
            pointData.cameraPosition = camera.transform.position; //Camera.main.cameraToWorldMatrix 
            pointData.localGazeOrigin = _gazeData.verbose_data.combined.eye_data.gaze_origin_mm;
            pointData.localGazeDirection = _gazeData.verbose_data.combined.eye_data.gaze_direction_normalized;
            pointData.worldGazeOrigin = camera.transform.localToWorldMatrix.MultiplyPoint(Vector3.Scale(_gazeData.verbose_data.combined.eye_data.gaze_origin_mm * 0.001f, new Vector3(-1, 1, 1)));
            pointData.worldGazeDirection = camera.transform.localToWorldMatrix.MultiplyVector(Vector3.Scale(_gazeData.verbose_data.combined.eye_data.gaze_direction_normalized, new Vector3(-1, 1, 1)));
            pointData.worldGazeDistance = 1.5f;  //_gazeData.verbose_data.combined.convergence_distance_mm;


            // added by xuemei.zyk, 2023-01-10
            float ignoredAngleThreshold = 7.5f;
            //direction of marker relative to eye position in world cordinates
            Vector3 markerDirection = pointData.worldMarkerPosition - pointData.worldGazeOrigin;
            //The accuracy of this single sample measured by offset angle
            float offsetAngle = Vector3.Angle(markerDirection, pointData.worldGazeDirection);

            if (offsetAngle > ignoredAngleThreshold)
            {
                pointData.exclude = true;
            }


            //Calculate sample metrics
            MetricsCalculator.CalculateSampleMetrics(ref pointData, _previousGazeDirection);

            _previousGazeDirection = pointData.worldGazeDirection;

            calibration.AddCalibrationPointReferencePosition(pointData);
        }

        private void UpdatePosition()
        {
            currLocalTargetPos = targets.GetLocalTargetPosAt(targetIdx);

            targetIdx++;
            tLastTarget = Time.time;
        }

        private void UpdateMarker()
        {
            marker.position = camera.transform.localToWorldMatrix.MultiplyPoint(currLocalTargetPos);
            marker.LookAt(camera.transform.position);
        }

        void InitPreviewMarker()
        {
            if (markersInitialized) return;

            var previewMarkerParent = new GameObject("Calibration Targets Preview");
            previewMarkerParent.transform.SetParent(camera.transform);//
            previewMarkerParent.transform.localPosition = Vector3.zero;
            previewMarkerParent.transform.localRotation = Quaternion.identity;

            for (int i = 0; i < targets.GetTargetCount(); ++i)
            {
                var target = targets.GetLocalTargetPosAt(i);
                var previewMarker = Instantiate<GameObject>(marker.gameObject);
                previewMarker.transform.parent = previewMarkerParent.transform;
                previewMarker.transform.localPosition = target;
                previewMarker.transform.LookAt(camera.transform.position);
                // modified by zyk, 2022-12-29
                //previewMarker.SetActive(true);
                //previewMarker.SetActive(this.gameObject.activeInHierarchy);
                previewMarker.SetActive(false);

                previewMarkers.Add(previewMarker);
            }

            markersInitialized = true;
        }

        void SetPreviewMarkers(bool value)
        {
            foreach (var marker in previewMarkers)
            {
                marker.SetActive(value);
            }
        }

        private static void EyeCallback(ref EyeData_v2 eye_data)
        {
            _gazeData = eye_data;
        }

        public void OnDestroy()
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));

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
}

