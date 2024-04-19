using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
//using UnityEngine.XR.Interaction.Toolkit.UI;

public class RatingPC : MonoBehaviour
{
    public Slider mainSlider;
    //Reference to new "RectTransform"(Child of FillArea).
    public RectTransform newFillRect;


    private Text textonSliderValue;
    private Text Excellent;
    private Text Good;
    private Text Fair;
    private Text Poor;
    private Text Bad;

    private MainController mainControl;
    private RenderController renderController;
    private string dataOutputDir;
    private string experimentID;

    void Awake()
    {


        mainControl = FindObjectOfType<MainController>();
        if (mainControl == null)
        {
            Debug.LogError("Can not get a valid object of MainController!");
        }
    }

    //Deactivates the old FillRect and assigns a new one.
    void Start()
    {
        //mainSlider = GetComponent<Slider>(); // find the slider object
        mainSlider.fillRect.gameObject.SetActive(false);
        mainSlider.fillRect = newFillRect;
        mainSlider.direction = Slider.Direction.LeftToRight;
        mainSlider.minValue = 1.0f;
        mainSlider.maxValue = 5.0f;
        mainSlider.wholeNumbers = false; // set the slider's value to accept not only the int value.
        Excellent.text = "Excellent";
        Good.text = "Good";
        Fair.text = "Fair";
        Poor.text = "Poor";
        Bad.text = "Bad";

        dataOutputDir = mainControl.dataSaveDir;
        string pc_id = renderController.pc_folder_name;
        experimentID = string.Format("{0}_{1}", mainControl.userid, mainControl.Session);


    }
    //Update is called once per frame
    void Update()
    {
        ShowSliderValue();
    }

    public void ShowSliderValue() 
    {
        mainSlider.onValueChanged.AddListener((v) =>
        {
            textonSliderValue.text = v.ToString("0.00");
        });
    }
    public void RecordRatingScore() {
        Debug.Log("Here is the votation of the User" + mainSlider.value.ToString());
        //here is where the user should go to the calibration scene, remember to disable the rating
        //create seperate function to call (do everything by funcation then call them!) save the socre

    }



}


