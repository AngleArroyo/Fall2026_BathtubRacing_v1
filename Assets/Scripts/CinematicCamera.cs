using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinematicCamera : MonoBehaviour
{
  float cameraSpeed = 0.05f;
    
    void Update()
    {
        /*if(Input.GetKey(KeyCode.Q)) //Move on positive X axis
            transform.position = new Vector3(transform.position.x + cameraSpeed, transform.position.y, transform.position.z);
        
        if(Input.GetKey(KeyCode.A)) //Move on negative X axis
            transform.position = new Vector3(transform.position.x - cameraSpeed, transform.position.y, transform.position.z);
        
        if(Input.GetKey(KeyCode.W)) //Move on positive Y axis
            transform.position = new Vector3(transform.position.x, transform.position.y  + cameraSpeed, transform.position.z);
        
        if(Input.GetKey(KeyCode.S)) //Move on negative Y axis
            transform.position = new Vector3(transform.position.x, transform.position.y - cameraSpeed, transform.position.z);
        
        if(Input.GetKey(KeyCode.E)) //Move on positive Z axis
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + cameraSpeed);
        
        if(Input.GetKey(KeyCode.D)) //Move on negative Z axis
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z -cameraSpeed);
        
        if(Input.GetKey(KeyCode.R)) //Rotate left
            transform.Rotate(new Vector3(0, 1, 0));
        
        if(Input.GetKey(KeyCode.F)) //Rotate right
            transform.Rotate(new Vector3(0, -1, 0));*/
        
        if(Input.GetKey(KeyCode.A)) //Rotate left
            transform.Rotate(new Vector3(0, -1, 0));
        
        if(Input.GetKey(KeyCode.D)) //Rotate right
            transform.Rotate(new Vector3(0, 1, 0));
        
        if(Input.GetKey(KeyCode.Space)) //Move on positive Y axis
            transform.position = new Vector3(transform.position.x, transform.position.y  + cameraSpeed, transform.position.z);
        
        if(Input.GetKey(KeyCode.LeftShift)) //Move on negative Y axis
            transform.position = new Vector3(transform.position.x, transform.position.y - cameraSpeed, transform.position.z);
        
        if(Input.GetKey(KeyCode.W)) //Move Forward
            transform.position += transform.forward * cameraSpeed * 2;
        
        if(Input.GetKey(KeyCode.S)) //Move back
            transform.position -= transform.forward * cameraSpeed * 2;

        if (Input.GetKey(KeyCode.LeftControl))
            cameraSpeed = 0.3f;
        else
            cameraSpeed = 0.05f;
    }
}
