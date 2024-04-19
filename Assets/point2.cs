using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class point2 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
 
        GameObject point2 = GameObject.Find("point2");
        
        GameObject cube = point2.transform.Find("Cube").gameObject;
        GameObject cube1 = point2.transform.Find("Cube1").gameObject;
        Debug.Log("point2's location (before setting the transform.rotation) is " + point2.transform.position);
        Debug.Log("cube's location (before setting the transform.rotation) is " + cube.transform.position);
        Debug.Log("cube1's location (before setting the transform.rotation) is " + cube1.transform.position);


        point2.transform.rotation = Quaternion.Euler(0, 90, 0);
        Debug.Log("point2's location (after setting the transform.rotation) is " + point2.transform.position);
        Debug.Log("cube's location (after setting the transform.rotation) is " + cube.transform.position);
        Debug.Log("cube1's location (after setting the transform.rotation) is " + cube1.transform.position);
        GameObject sphereCreated = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sphereCreated.transform.position = cube.transform.position;
        sphereCreated.transform.rotation = Quaternion.identity;
        sphereCreated.transform.localScale = new Vector3(2f, 1f, 1f);
        Renderer rend = sphereCreated.GetComponent<Renderer>();
        rend.material.color = Color.green;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
