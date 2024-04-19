using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
public class QualityAssessment : MonoBehaviour
{

    public GameObject anotherPanel;
    public Slider slider;
    public bool state = false;
    public int score = 0;
    public float strength = 0.15f;
    protected Transform panelContainer;

    protected readonly Vector2 xAxis = new Vector2(1, 0);
    protected readonly Vector2 yAxis = new Vector2(0, 1);
    protected Vector2 touchStartPosition;
    protected Vector2 touchEndPosition;
    protected float touchStartTime;
    protected float currentAngle;
    protected bool isTrackingSwipe = false;
    protected bool isPendingSwipeCheck = false;

    // Swipe sensitivity / detection.
    protected const float AngleTolerance = 30f;
    protected const float SwipeMinDist = 0.2f;
    protected const float SwipeMinVelocity = 4.0f;
    private Color yellow = new Color(241f / 255f, 162f / 255f, 8f / 255f, 0.6f); //F1A208FF

    void Start()
    {

        //panelContainer = transform.GetChild(0).GetChild(0).transform;
        //panelContainer.Find("Border").gameObject.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        //if (isPendingSwipeCheck)
        //{
        //    CalculateSwipeAction();
        //}
    }
    protected void SetPanel(bool state)
    {
        panelContainer.Find("Border").gameObject.SetActive(state);
        this.state = state;

        anotherPanel.transform.GetChild(0).GetChild(0).Find("Border").gameObject.SetActive(!state);
        //anotherPanel.GetComponent<panelEvaluating>().state = !state;
    }

}

// Use this for initialization






//        protected virtual void OnEnable()
//        {
//            controllerEvents = GameObject.FindWithTag("GameController").GetComponent<VRTK_ControllerEvents>();

//        }

//        protected virtual void OnDisable()
//        {
//            //controllerEvents.GripClicked -= new ControllerInteractionEventHandler(DoGripCliked);

//        }

//        public virtual void BindControllerEvents()
//        {
//            if (panelMenuItemController != null && controllerEvents != null)
//            {
//                controllerEvents.TouchpadTouchStart += new ControllerInteractionEventHandler(DoTouchpadTouched);
//                controllerEvents.TouchpadTouchEnd += new ControllerInteractionEventHandler(DoTouchpadUntouched);
//                controllerEvents.TouchpadAxisChanged += new ControllerInteractionEventHandler(DoTouchpadAxisChanged);
//                controllerEvents.GripClicked += new ControllerInteractionEventHandler(DoGripCliked);
//                controllerEvents.TriggerClicked += new ControllerInteractionEventHandler(DoTriggerClicked);

//                panelMenuItemController.PanelMenuItemSwipeRight += PanelMenuItemSwipeRight;
//                panelMenuItemController.PanelMenuItemSwipeLeft += PanelMenuItemSwipeLeft;
//                Debug.Log(transform.name + " BindControllerEvents");


//            }
//        }



//        public virtual void UnbindControllerEvents()
//        {
//            if (panelMenuItemController != null && controllerEvents != null)
//            {
//                controllerEvents.TouchpadTouchStart -= new ControllerInteractionEventHandler(DoTouchpadTouched);
//                controllerEvents.TouchpadTouchEnd -= new ControllerInteractionEventHandler(DoTouchpadUntouched);
//                controllerEvents.TouchpadAxisChanged -= new ControllerInteractionEventHandler(DoTouchpadAxisChanged);
//                controllerEvents.GripClicked -= new ControllerInteractionEventHandler(DoGripCliked);
//                controllerEvents.TriggerClicked -= new ControllerInteractionEventHandler(DoTriggerClicked);

//                panelMenuItemController.PanelMenuItemSwipeRight -= PanelMenuItemSwipeRight;
//                panelMenuItemController.PanelMenuItemSwipeLeft -= PanelMenuItemSwipeLeft;

//                Debug.Log(transform.name + " UnbindControllerEvents");

//            }

//        }

//        protected virtual void PanelMenuItemSwipeRight(object sender, PanelMenuItemControllerEventArgs e)
//        {
//            if (slider != null)
//            {
//                slider.value++;
//                SetColor();
//            }
//        }

//        protected virtual void PanelMenuItemSwipeLeft(object sender, PanelMenuItemControllerEventArgs e)
//        {
//            if (slider != null)
//            {
//                slider.value--;
//                SetColor();
//            }
//        }

//        private void SetColor()
//        {
//            Transform textCanvas = panelContainer.Find("PanelScoreControls").Find("TextCanvas");
//            Debug.Log("value: " + (int)slider.value);
//            for (int i = (int)slider.minValue; i <= (int)slider.maxValue; i++)
//            {
//                if (i == (int)slider.value)
//                    textCanvas.GetChild(i).GetComponent<Text>().color = Color.red;
//                else
//                    textCanvas.GetChild(i).GetComponent<Text>().color = Color.white;

//            }

//        }

//        private void DoTriggerClicked(object sender, ControllerInteractionEventArgs e)
//        {

//        }

//        public void DoHoverPanel()
//        {

//        }

//        public void DoUnHoverPanel()
//        {

//        }

//        public void DoClickPanel()
//        {


//        }

//        private void DoGripCliked(object sender, ControllerInteractionEventArgs e)
//        {
//            Debug.Log("This is the end of DoGripCliked.");
//            TogglePanel();

//        }


//        public void TogglePanel()
//        {
//            Debug.Log("This is the begin of TogglePanel." + state + anotherPanel.GetComponent<panelEvaluating>().state);
//            if (state == true && anotherPanel.GetComponent<panelEvaluating>().state == false)
//            {
//                SetPanel(false);
//                UnbindControllerEvents();
//                anotherPanel.GetComponent<panelEvaluating>().BindControllerEvents();
//            }
//            else if (state == false && anotherPanel.GetComponent<panelEvaluating>().state == true)
//            {
//                SetPanel(true);
//                anotherPanel.GetComponent<panelEvaluating>().UnbindControllerEvents();
//                BindControllerEvents();
//            }
//            Debug.Log("This is the end of TogglePanel." + state + anotherPanel.GetComponent<panelEvaluating>().state);

//        }

//        protected virtual void CalculateSwipeAction()
//        {
//            isPendingSwipeCheck = false;

//            float deltaTime = Time.time - touchStartTime;
//            Vector2 swipeVector = touchEndPosition - touchStartPosition;
//            float velocity = swipeVector.magnitude / deltaTime;

//            if ((velocity > SwipeMinVelocity) && (swipeVector.magnitude > SwipeMinDist))
//            {
//                swipeVector.Normalize();
//                float angleOfSwipe = Vector2.Dot(swipeVector, xAxis);
//                angleOfSwipe = Mathf.Acos(angleOfSwipe) * Mathf.Rad2Deg;

//                // Left / right
//                if (angleOfSwipe < AngleTolerance)
//                {
//                    OnSwipeRight();
//                }
//                else if ((180.0f - angleOfSwipe) < AngleTolerance)
//                {
//                    OnSwipeLeft();
//                }
//                else
//                {
//                    // Top / bottom
//                    /*
//                    angleOfSwipe = Vector2.Dot(swipeVector, yAxis);
//                    angleOfSwipe = Mathf.Acos(angleOfSwipe) * Mathf.Rad2Deg;
//                    if (angleOfSwipe < AngleTolerance)
//                    {
//                        OnSwipeTop();
//                    }
//                    else if ((180.0f - angleOfSwipe) < AngleTolerance)
//                    {
//                        OnSwipeBottom();
//                    }
//                    */
//                }
//                AttemptHapticPulse(strength); //????

//                panelContainer.Find("PanelScoreControls").Find("ScorceLabelNum").GetComponent<Text>().text = (5 - (int)slider.value).ToString(); //??????
//            }
//        }

//        protected virtual void OnSwipeLeft()
//        {
//            if (panelMenuItemController != null)
//            {
//                panelMenuItemController.SwipeLeft(transform.gameObject);

//            }

//        }

//        protected virtual void OnSwipeRight()
//        {
//            if (panelMenuItemController != null)
//            {
//                panelMenuItemController.SwipeRight(transform.gameObject);
//            }
//        }

//        protected virtual void DoTouchpadTouched(object sender, ControllerInteractionEventArgs e)
//        {
//            touchStartPosition = new Vector2(e.touchpadAxis.x, e.touchpadAxis.y);
//            touchStartTime = Time.time;
//            isTrackingSwipe = true;
//        }

//        protected virtual void DoTouchpadUntouched(object sender, ControllerInteractionEventArgs e)
//        {
//            isTrackingSwipe = false;
//            isPendingSwipeCheck = true;
//        }

//        protected virtual void DoTouchpadAxisChanged(object sender, ControllerInteractionEventArgs e)
//        {
//            ChangeAngle(CalculateAngle(e));

//            if (isTrackingSwipe)
//            {
//                touchEndPosition = new Vector2(e.touchpadAxis.x, e.touchpadAxis.y);
//            }
//        }

//        protected virtual void ChangeAngle(float angle, object sender = null)
//        {
//            currentAngle = angle;
//        }

//        protected virtual float CalculateAngle(ControllerInteractionEventArgs e)
//        {
//            return e.touchpadAngle;
//        }

//        protected void AttemptHapticPulse(float strength)
//        {
//            VRTK_ControllerHaptics.TriggerHapticPulse(VRTK_ControllerReference.GetControllerReference(controllerEvents.gameObject), strength);
//        }
//    }

//}
