using UnityEngine;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 5.0f;
    public float height = 2.0f;
    public float damping = 5.0f;

    void LateUpdate()
    {
        if (!target) return;

        float wantedRotationAngle = target.eulerAngles.y;
        float wantedHeight = target.position.y + height;

        float currentRotationAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;

        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, damping * Time.deltaTime);
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, damping * Time.deltaTime);

        Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

        transform.position = target.position;
        transform.position -= currentRotation * Vector3.forward * distance;

        transform.position = new Vector3(transform.position.x, currentHeight, transform.position.z);

        transform.LookAt(target);
    }
}
