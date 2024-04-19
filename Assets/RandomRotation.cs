using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomRotation : MonoBehaviour
{
    public float randomRotationMatrix;
    // Start is called before the first frame update
    void Awake()
    {
        // Get a random rotation around the y-axis
        randomRotationMatrix = Random.Range(0.0f, 360.0f);
        Debug.Log("The rotation matrix is: " + randomRotationMatrix);
        // Rotate the game object around the y-axis by the random rotation
        transform.Rotate(new Vector3(0.0f, randomRotationMatrix, 0.0f));

    }

}
