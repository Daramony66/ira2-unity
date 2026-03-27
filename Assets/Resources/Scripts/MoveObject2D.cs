using UnityEngine;

public class ObjectMove2D : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed of movement

    void Update()
    {
        // Get input from keyboard
        float moveZ = Input.GetAxis("Horizontal"); // A/D or Left/Right arrow keys
        float moveY = Input.GetAxis("Vertical");   // W/S or Up/Down arrow keys
        

        // Calculate movement vector
        Vector3 movement = new Vector3(0f, moveY, moveZ) * moveSpeed * Time.deltaTime;

        // Apply movement to the object's position
        transform.Translate(movement, Space.World);
    }
}

