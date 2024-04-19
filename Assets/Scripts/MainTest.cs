using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class MainTest : MonoBehaviour
{
    private int flag = 0;
    private PointCloudRenderer pcRenderer;
    private PrerecordedPointCloudReader pcReader;
    private RatingPC ratingPC;
    
    [Header("RightHand Controller")]
    public ActionBasedController leftHandController;
    public ActionBasedController rightHandController;
   
    // Start is called before the first frame update
    void Start()
    {
        pcRenderer = FindObjectOfType<PointCloudRenderer>();
        Debug.Log(pcRenderer.gameObject.name);
        pcReader = FindObjectOfType<PrerecordedPointCloudReader>();
        ratingPC = FindObjectOfType<RatingPC>();

        if (ratingPC.isActiveAndEnabled)
        {

            ratingPC.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (flag==0 && rightHandController.activateAction.action.triggered)
        {
            Debug.Log("The right hand controller is pressed!"); // sucusseful!!!
            Debug.Log("Now Rating!");
            ratingPC.gameObject.SetActive(true);
            pcRenderer.gameObject.SetActive(false);
            pcReader.gameObject.SetActive(false);
            // Now start rating // folder_name(pc_name) score txt
            flag = 1;
        }
        else if (flag ==1 && rightHandController.activateAction.action.triggered)
        {
            ratingPC.gameObject.SetActive(false);
            pcRenderer.gameObject.SetActive(true);
            pcReader.gameObject.SetActive(true);
            Debug.Log("Now looking at these point clouds!");
            flag = 0;
        }

    }

}
