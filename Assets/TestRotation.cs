using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRotation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        //Debug.Log("TestRoation: After the transform.rotation" + transform.rotation.eulerAngles);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
