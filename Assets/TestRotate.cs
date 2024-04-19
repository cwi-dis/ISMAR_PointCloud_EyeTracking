using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRotate : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // This is for test the transform.rotate
        Debug.Log("Test Rotate before rotation angles: " + transform.rotation.eulerAngles);
        Vector3 cubePositionBefore = this.transform.position;
        Debug.Log("Before transforming rotate:" + this.transform.rotation.eulerAngles);
        Debug.Log(" cube location is: " + cubePositionBefore);
        Debug.Log(" Before transfrom.rotation: " + transform.rotation);
        Vector3 point1 = new Vector3(3f, 4f, 5f);
        //transform.Rotate(0f, 90f, 0f,Space.Self);
        transform.rotation = Quaternion.Euler(0, 90, 0);
        Debug.Log(" After transfrom.rotation: " + transform.rotation);
        Vector3 cubePositionAfter = this.transform.position;
        Debug.Log("Test Rotate: After rotation angles" + transform.rotation.eulerAngles);
        Debug.Log("After transforming rotate:  " + this.transform.rotation.eulerAngles);
        Debug.Log("cube location is:" + cubePositionAfter);
        //reset
        transform.rotation = Quaternion.Euler(0, 0, 0);
        Debug.Log(" After Reset transfrom.rotation: " + transform.rotation);
        GameObject  parent = GameObject.Find("TestTransform");
        parent.transform.rotation = Quaternion.Euler(0, 90, 0);
        Vector3 cubePositionAfter2 = this.transform.position;
        Debug.Log("After transforming the parent game object:  " + this.transform.rotation.eulerAngles);
        Debug.Log("cube location is:" + cubePositionAfter2);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
