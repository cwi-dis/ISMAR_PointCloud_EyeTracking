using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMatrix : MonoBehaviour
{
    Matrix4x4 pcMatrixX;
    Matrix4x4 pcMatrixY;
    Vector3 point = new Vector3(2, 3, 4);
    // Start is called before the first frame update
    void Start()
    {
        Matrix4x4 pcMatrix = transform.localToWorldMatrix;
        Debug.Log(transform.localToWorldMatrix);
        pcMatrixX = pcMatrix * Matrix4x4.Scale(new Vector3(-1, 1, 1));
        Debug.Log(pcMatrixX);
        pcMatrixY = pcMatrix * Matrix4x4.Scale(new Vector3(1, 1, -1));
        Debug.Log(pcMatrixY);




        //Debug.Log("Y MATRIX IS:" + pcMatrixY * point);
        //Debug.Log("MutiplyPoint x MATRIX IS:" + pcMatrixX.MultiplyPoint(point));

        //Debug.Log("MutiplyPoint Y MATRIX IS:" + pcMatrixY.MultiplyPoint(point));
    }

    // Update is called once per frame
    void Update()
    {

    }
}
