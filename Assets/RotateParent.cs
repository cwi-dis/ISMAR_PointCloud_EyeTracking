using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateParent : MonoBehaviour
{
    private GameObject parent;
    // Start is called before the first frame update
    void Start()
    {
        parent = GameObject.Find("RotateGameObject");
        Vector3 cubePositionBefore = this.transform.position; // this should be the cube
        Debug.Log("Before transforming the parent game object:" + this.transform.rotation.eulerAngles);
        Debug.Log(" cube location is: " + cubePositionBefore);
        parent.transform.rotation = Quaternion.Euler(0, 90, 0);
        Vector3 cubePositionAfter = this.transform.position; 
        Debug.Log("After transforming the parent game object:  " + this.transform.rotation.eulerAngles);
        Debug.Log("cube location is:" + cubePositionAfter);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
