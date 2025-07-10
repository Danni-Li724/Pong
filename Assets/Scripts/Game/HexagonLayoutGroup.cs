using UnityEngine;
using UnityEngine.UI;
[ExecuteAlways]
public class HexagonLayoutGroup : MonoBehaviour
{
    public float radius = 80f;
    public bool includeCenter = true;
    public float rotationSpeed = 10f; // degrees per second (clockwise)

    private float currentRotation = 0f;

    void Update()
    {
        if (!Application.isPlaying) // Prevent rotation in edit mode
        {
            ArrangeChildren(currentRotation);
            return;
        }

        currentRotation += rotationSpeed * Time.deltaTime;
        ArrangeChildren(currentRotation);
    }

    void ArrangeChildren(float rotationOffset)
    {
        int childCount = transform.childCount;
        int index = 0;

        if (includeCenter && index < childCount)
        {
            transform.GetChild(index).localPosition = Vector3.zero;
            index++;
        }

        for (int i = 0; i < 6 && index < childCount; i++, index++)
        {
            float angleDeg = 60f * i - 30f + rotationOffset; // Rotate layout clockwise
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 pos = new Vector3(
                radius * Mathf.Cos(angleRad),
                radius * Mathf.Sin(angleRad),
                0f
            );
            transform.GetChild(index).localPosition = pos;
        }
    }
}
