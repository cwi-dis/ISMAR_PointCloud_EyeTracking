using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class point1 : MonoBehaviour
{
    public Sprite imageSprite;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(" Before apply transfrom.rotation: " + transform.rotation.eulerAngles);
        Vector3 Point1PositionBefore = this.transform.position;
        Debug.Log("point1 location is:" + Point1PositionBefore);
        Quaternion rotationMatrix = Quaternion.Euler(0, 90, 0);
        Vector3 point1 = new Vector3(2f, 3f, 0f); //global
        Debug.Log("position of point 1 (before):" + point1);
        Vector3 point1After = rotationMatrix * point1;
        Debug.Log("position of point 1 (after):" + point1After);

        Vector3 capsule2 = new Vector3(2f, 3f, 0f);
        Debug.Log("position of sphere (before):" + capsule2);
        Vector3 capsule2Transfored = transform.TransformPoint(capsule2);
        Debug.Log("point1's location (World Location) is " + capsule2Transfored.x+  " "+ " " + capsule2Transfored.y +" " + capsule2Transfored.z);
        Vector3 capsule2TransforedAfter = rotationMatrix * capsule2Transfored;
        Debug.Log("point1's position (after): " + capsule2TransforedAfter);
        GameObject sphereCreated = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        sphereCreated.transform.position = capsule2TransforedAfter;
        sphereCreated.transform.rotation = Quaternion.identity;
        sphereCreated.transform.localScale = new Vector3(1f, 1f, 1f);
        Renderer rend = sphereCreated.GetComponent<Renderer>();
        rend.material.color = Color.blue;

        //transform.rotation = Quaternion.Euler(0, 90, 0);

       
        //Vector3 Point1PositionAfter = this.transform.position;
        ////Debug.Log(" After apply transfrom.rotation" + transform.rotation);
        //Debug.Log("cube location is:" + Point1PositionAfter);

       

        GameObject capsuleCreated = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsuleCreated.transform.position = point1After;
        capsuleCreated.transform.rotation = Quaternion.identity;

        capsuleCreated.transform.localScale = new Vector3(1f, 1f, 1f);
        Renderer rend_ = capsuleCreated.GetComponent<Renderer>();
        rend_.material.color = Color.red;


        ////reset
        //transform.rotation = Quaternion.Euler(0, 0, 0);
        //Debug.Log(" After Reset transfrom.rotation: " + transform.rotation);
        //GameObject parent = GameObject.Find("TestTransform");
        //parent.transform.rotation = Quaternion.Euler(0, 90, 0);
        //Vector3 cubePositionAfter2 = this.transform.position;
        //Debug.Log("After transforming the parent game object:  " + this.transform.rotation.eulerAngles);
        //Debug.Log("cube location is:" + cubePositionAfter2);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
