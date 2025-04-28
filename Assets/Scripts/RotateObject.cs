using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0, 30, 0); // 1秒間に回転する角度（度）

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}