//This script is used to make UI elements face the player's camera

using UnityEngine;

public class LookAtCamera: MonoBehaviour 
{
    private Transform _mainCamera;   //The camera's transform

    private void Start()
    {
        //Set the Main Camera as the target
        _mainCamera = Camera.main.transform;
    }

    //Update after all other updates have run
    private void LateUpdate()
    {
        if (_mainCamera == null)
		{
			return;
		}
        
        //Apply the rotation needed to look at the camera. Note, since pointing a UI text element
        //at the camera makes it appear backwards, we are actually pointing this object
        //directly *away* from the camera.
        transform.rotation = Quaternion.LookRotation (transform.position - _mainCamera.position);
    }
}