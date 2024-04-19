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

public class OnlyPlayback : MonoBehaviour
{
    private int flag = 0;  // 0: render, 1: rating, 2: calibration  init state only the render is working
    private RenderController renderController;
    private RatingController ratingController;
    private CustomCalGazeMetric cusGazeMetricController;
    private PointCloudPlayback pointcloudPlayback;

    //[Tooltip("The reader for the pointclouds for which we get gaze data")]
    //public PrerecordedPointCloudReader pcdReader;
    ////public PointCloudRenderer pcdRender;



    [Header("Experiment setting")]
    public string userid = "001";
    public string Session = "A";
    public string dataSaveDir = @"C:\";
    // TODO
    //public string pc_folder_name;

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
        renderController = FindObjectOfType<RenderController>();
        //ratingController = FindObjectOfType<RatingController>();
        //cusGazeMetricController = FindObjectOfType<CustomCalGazeMetric>();
        pointcloudPlayback = FindObjectOfType<PointCloudPlayback>();

        //// TODO
        if (pointcloudPlayback == null)
        {
            Debug.LogError("renderController == null || ratingController == null || cusGazeMetricController == null || pointcloudPlayback == null !!!");
            UnityEditor.EditorApplication.isPlaying = false;
        }

        renderController.gameObject.SetActive(false);
        pointcloudPlayback.gameObject.SetActive(true);
        //ratingController.gameObject.SetActive(false);
        //cusGazeMetricController.gameObject.SetActive(false);

        //Debug.Log("The first path is :" + renderController.pcdReader.dirName); //already checked
        //Debug.Log("pointcloudPlayback is " + pointcloudPlayback.gameObject.activeSelf); //already checked true!
        //Debug.Log("Now is playback:" + pointcloudPlayback.dirName);    //already checked
        //pointcloudPlayback.RendererStarted();

    }

    // Update is called once per frame
    void Update()
    {

        bool nextWasTriggered = m_nextStageAction.action.triggered;

        if (pointcloudPlayback.isRenderFinished && flag == 0)  // the number of loops has been played 
        {
            //    ratingController.gameObject.SetActive(true);
            //    cusGazeMetricController.gameObject.SetActive(false);
            //    renderController.SetRenderActive(false);  // OnDestroy call Stop then reader = null ;
            Debug.Log("Now flag is 0 and will disable playing the Point cloud!");
            flag = 1;
            pointcloudPlayback.isRenderFinished = false;


        }

        else if  (flag == 1)
        {

            renderController.RenderNext();
            //cusGazeMetricController.gameObject.SetActive(false);
            //ratingController.gameObject.SetActive(false);
            pointcloudPlayback.gameObject.SetActive(true);
            renderController.SetRenderActive(true); // Todo: remove the frist 800 miliseconds data
            pointcloudPlayback.Play(pointcloudPlayback.dirName);
            Debug.Log("Now flag is 2 and doing the Calibration!");
            flag = 0;

        }
        

    }

}

