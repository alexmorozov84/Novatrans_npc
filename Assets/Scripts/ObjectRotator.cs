using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    public Transform target; // ���� ��� ��������

    public float rotationSpeed = 500.0f;

    void Update()
    {
        if (target != null && Input.GetMouseButton(0))
        {
            // ��������, ��������� �� ������ � ������ ����� ������
            if (Input.mousePosition.x > Screen.width * 0.3f)
            {
                float h = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                float v = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

                target.Rotate(Vector3.up, -h, Space.World);
                target.Rotate(Vector3.right, v, Space.World);
            }
        }
    }
}