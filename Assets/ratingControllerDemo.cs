using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;
using System.Text;
using Unity.VisualScripting;
public class ratingControllerDemo : MonoBehaviour
{
    //public Slider mainSlider;
    //Reference to new "RectTransform"(Child of FillArea).
    //public RectTransform newFillRect;
    public ActionBasedController rightHandController;

    private RenderDemo renderControl;


    public TextMeshPro textForSliderValue;


    private MainControllerDemo mainControl;
    private string dataOutputDir;
    private string experimentID;
    private string savePath;
    private string pc_id;
    private int pc_index; //added by xuemei index of the point cloud
    //public GameObject OkButton; 
    public GameObject ButtonBad;
    public GameObject ButtonPoor;
    public GameObject ButtonFair;
    public GameObject ButtonGood;
    public GameObject ButtonExcellent;




    private bool isFinised = false;
    public bool FinishedRating
    {
        get => isFinised;
        private set => isFinised = value;
    }


    void Awake()
    {
        mainControl = FindObjectOfType<MainControllerDemo>();
        if (mainControl == null)
        {
            Debug.LogError("Can not get a valid object of MainController!");
        }



        ButtonBad = GameObject.Find("ButtonBad");
        ButtonPoor = GameObject.Find("ButtonPoor");
        ButtonFair = GameObject.Find("ButtonFair");
        ButtonGood = GameObject.Find("ButtonGood");
        ButtonExcellent = GameObject.Find("ButtonExcellent");


    }



    private void OnEnable()
    {
        FinishedRating = false;
        ButtonBad.SetActive(true);
        ButtonPoor.SetActive(true);
        ButtonFair.SetActive(true);
        ButtonGood.SetActive(true);
        ButtonExcellent.SetActive(true);
        //ResetSlider();
    }


    //private void ResetSlider()
    //{
    //    mainSlider.fillRect.gameObject.SetActive(false);
    //    //mainSlider.fillRect = newFillRect;
    //    mainSlider.direction = Slider.Direction.LeftToRight;
    //    mainSlider.minValue = 1.0f;
    //    mainSlider.maxValue = 5.0f;
    //    mainSlider.wholeNumbers = true; // set the slider's value to accept not only the int value.
    //    mainSlider.value = 0;
    //}


    //Deactivates the old FillRect and assigns a new one.
    void Start()
    {
        renderControl = FindObjectOfType<RenderDemo>();
        if (renderControl == null)
        {
            Debug.LogError("Can not get a valid object of RenderController!");
        }

        dataOutputDir = mainControl.dataSaveDir;
        experimentID = string.Format("{0}_{1}{2}", mainControl.userid, mainControl.Session, ".txt");
        savePath = Path.Combine(dataOutputDir, experimentID);

        string pcdpath = renderControl.GetCurrentPcdPath();
        int pcIndex = renderControl.GetCurrentpcIndex();
        OnCurrDirPathUpdated(pcdpath, pcIndex);
        renderControl.OnCurrDirPathUpdated += this.OnCurrDirPathUpdated;

    }

    private void OnCurrDirPathUpdated(string dirpath, int curPCindex)
    {
   
        string pcdName = Path.GetFileName(dirpath);
        pc_id = pcdName;
        pc_index = curPCindex;
    }


    //Update is called once per frame
    void Update()
    {

    }


    public void FinishedRatingFun(int ButtonScore)
    {
        Debug.Log("On Click()");

        if (!this.FinishedRating)
        {
            // save rating data pc_id
            //int rating_score = mainSlider.value;
            int rating_score = ButtonScore;
            string allInfo = "pc_id: " + pc_id + " " + "pc_Index" + pc_index + " " + "MOS: " + rating_score.ToString() + "\n";
            // RecordRatingScore(allInfo, savePath);
            SaveRatingScoreButton(allInfo, savePath);
            FinishedRating = true;
            //OkButton.SetActive(false);
        }

    }





    public void SaveRatingScoreButton(string strs, string path)
    {
        //Debug.Log("Here is the rating score of the User" + mainSlider.value.ToString());

        if (!File.Exists(path))
        {
            FileStream fs = File.Create(path);
            fs.Dispose();
        }

        using (StreamWriter stream = new StreamWriter(path, true))
        {
            stream.WriteLine(strs);
        }

        //here is where the user should go to the calibration scene, remember to disable the rating
        //create seperate function to call (do everything by funcation then call them!) save the socre

    }

}





