using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class test_controller : MonoBehaviour
{
    //public ActionBasedController leftHandController;
    public ActionBasedController rightHandController;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (rightHandController.activateAction.action.triggered)
        {
            Debug.Log("The right hand controller is pressed!"); // sucusseful!!!
        }
        //else
        //{
        //    Debug.LogError("Interaction with right controller failed!");
        //}

        
    }

    //[Tooltip("Actions to check")]
    //public InputAction action = null;

    //// When the button is pressed
    //public UnityEvent OnPress = new UnityEvent();

    //// When the button is released
    //public UnityEvent OnRelease = new UnityEvent();

    //private void Awake()
    //{
    //    action.started += Pressed;
    //    action.canceled += Released;
    //}

    //private void OnDestroy()
    //{
    //    action.started -= Pressed;
    //    action.canceled -= Released;
    //}

    //private void OnEnable()
    //{
    //    action.Enable();
    //}

    //private void OnDisable()
    //{
    //    action.Disable();
    //}

    //private void Pressed(InputAction.CallbackContext context)
    //{
    //    OnPress.Invoke();
    //}

    //private void Released(InputAction.CallbackContext context)
    //{
    //    OnRelease.Invoke();
    //}
}
