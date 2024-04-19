using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;

public class switchPointcloud : MonoBehaviour
{
    public GameObject pcViewer;
    //public GameObject CalibrationCanvus;
    public List<string> pc_paths;
    private int pcr_id = 0;
    public string triggerKey;
    public PrerecordedPointCloudReader pcdReader;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UnityEngine.InputSystem.Keyboard.current.onTextInput +=
            inputText => {
                if (inputText.ToString() == triggerKey)
                {
                    pcViewer.SetActive(false);
                    PrerecordedPointCloudReader pcReader = pcViewer.GetComponentInChildren<PrerecordedPointCloudReader>();
                    pcr_id++;
                    if (pcr_id == pc_paths.Count) pcr_id = 0;
                    //Debug.Log("setting index to" + pcr_id);
                    Debug.Log(pc_paths[pcr_id]);

                    pcReader.dirName = pc_paths[pcr_id];
                    var X = pcReader.dirName;
                    //pcReader.SetDirName(pc_paths[pcr_id]);
                    pcViewer.SetActive(true);

                    //pcViewer.GetComponent<Canvas>().enabled = false;
                    //CalibrationCanvus = pcViewer.GetComponent<Canvas>();
                }
            };
    }


}
