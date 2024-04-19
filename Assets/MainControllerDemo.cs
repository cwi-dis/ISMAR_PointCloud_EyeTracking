using GazeMetrics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using ViveSR.anipal.Eye;
using Cwipc;
using System;
public class MainControllerDemo : MonoBehaviour
{
    private int flag = 0;  // 0: render, 1: rating, 2: calibration  init state only the render is working
    private RenderDemo renderController;
    private ratingControllerDemo ratingController;
    private CustomDemo cusGazeMetricController;
    private PointCloudPlayback pointcloudPlayback;

    
    [Header("Experiment setting")]
    public string userid = "1000";
    public string Session = "b";
    public string dataSaveDir = @"C:\";
  

    [Header("RightHand Controller")]
    public ActionBasedController rightHandController;
    [Tooltip("The Input System Action that will go to the next stage")]
    [SerializeField] InputActionProperty m_nextStageAction;
    private float ignoreNextUntil;

    public InputActionProperty nextStageAction { get => m_nextStageAction; }


    private void Awake()
    {

        // test dataSaveDir
        if (string.IsNullOrWhiteSpace(dataSaveDir))
        {
            Debug.LogError("dataSaveDir is empty!");
        }


        dataSaveDir = Path.Combine(dataSaveDir, $"user_{userid}");
        if (!System.IO.Directory.Exists(dataSaveDir))
        {
            try
            {
                Directory.CreateDirectory(dataSaveDir);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Create dataSaveDir error! [{dataSaveDir}]  {ex.Message}");
                throw;
            }
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        renderController = FindObjectOfType<RenderDemo>();
        ratingController = FindObjectOfType<ratingControllerDemo>();
        cusGazeMetricController = FindObjectOfType<CustomDemo>();
        pointcloudPlayback = FindObjectOfType<PointCloudPlayback>();

        //// TODO
        if (ratingController == null || cusGazeMetricController == null || pointcloudPlayback == null)
        {
            Debug.LogError("renderController == null || ratingController == null || cusGazeMetricController == null || pointcloudPlayback == null !!!");
            UnityEditor.EditorApplication.isPlaying = false;
        }

        renderController.gameObject.SetActive(false);
        pointcloudPlayback.gameObject.SetActive(true);
        ratingController.gameObject.SetActive(false);
        cusGazeMetricController.gameObject.SetActive(false);

        

    }

    // Update is called once per frame
    void Update()
    {

#if oldcoderemovedbyJacktotestnewercode
        bool nextWasTriggered = rightHandController.selectAction.action.triggered;
#endif
        bool nextWasTriggered = m_nextStageAction.action.triggered;

        if (pointcloudPlayback.isRenderFinished && flag == 0)  // the number of loops has been played 
        {
      
            Debug.Log("Now flag is 0 and will disable playing the Point cloud!");
            flag = 1;
            pointcloudPlayback.isRenderFinished = false;


        }

        else if (ratingController.FinishedRating && flag == 1)
        {

            renderController.SetRenderActive(false);
            pointcloudPlayback.gameObject.SetActive(false);
            ratingController.gameObject.SetActive(false);
            cusGazeMetricController.gameObject.SetActive(true);
            Debug.Log("Now flag is 1 and from Rating to Error Profiling!");
            flag = 2;

        }
        // now: calib switch to next render
        // before switch to next render, need do the re-calibration
        else if (nextWasTriggered && flag == 2)
        {
            if (cusGazeMetricController.Finished_calibration)
            {
                // added by xuemei.zykk, 2022-1-5, need to do the calibration again
                bool calibrationsucssful = SRanipal_Eye_v2.LaunchEyeCalibration();
                while (!calibrationsucssful)
                {
                    Debug.LogError("LaunchEyeCalibration failed!");
                    calibrationsucssful = SRanipal_Eye_v2.LaunchEyeCalibration();
                }

                Debug.Log("LaunchEyeCalibration Successuful!");

 

                renderController.RenderNext();
                cusGazeMetricController.gameObject.SetActive(false);
                ratingController.gameObject.SetActive(false);
                pointcloudPlayback.gameObject.SetActive(true);
                renderController.SetRenderActive(true); // Todo: remove the frist 800 miliseconds data
                pointcloudPlayback.Play(pointcloudPlayback.dirName);
                Debug.Log("Now flag is 2 and doing the Calibration!");
                flag = 0;
            }

        }


    }
}




