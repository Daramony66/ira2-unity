using UnityEngine;

public class ObjectMove3D : MonoBehaviour
{
    private Plane dragPlane;
    private Vector3 offset;

    void OnMouseDown()
    {
        //dragPlane = new Plane(Vector3.right, transform.position); // plan y et z
        dragPlane = new Plane(Vector3.forward, transform.position); // plan x et y
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(camRay, out float enter))
        {
            offset = transform.position - camRay.GetPoint(enter);
        }
    }

    void OnMouseDrag()
    {
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(camRay, out float enter))
        {
            Vector3 newPos = camRay.GetPoint(enter) + offset;
            transform.position = newPos;
        }
    }

    void Update()
    {
        // Déplacement selon z via roulette
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            Vector3 pos = transform.position;
            pos.z += scroll * 0.01f; 
            transform.position = pos;
        }
    }
}
