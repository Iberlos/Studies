using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeControlForDemonstration : MonoBehaviour
{
    [SerializeField, Range(0.1f, 100f)]
    private float speed = 10f;
    [SerializeField, Range(0.1f, 100f)]
    private float acc = 1f;
    [SerializeField, Range(0.1f, 100f)]
    private float angSpeed = 15f;

    private Vector3 eulerAngles;


    // Update is called once per frame
    void Update()
    {
        float vI = Input.GetAxis("Vertical");
        float hI = Input.GetAxis("Horizontal");
        if (vI != 0 || hI != 0)
        {
            transform.position += transform.forward * vI + transform.right * hI;
        }
        Vector3 mI = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
        if(mI != Vector3.zero)
        {
            eulerAngles += mI * angSpeed;
            transform.rotation = Quaternion.Euler(eulerAngles);
        }
        float sWI = Input.mouseScrollDelta.y;
        if(sWI != 0f)
        {
            speed += sWI * acc;
            speed = Mathf.Clamp(speed, 0.1f, 100f);
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
