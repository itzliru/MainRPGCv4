using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Nocliping : MonoBehaviour
{
    public static Nocliping Instance;

    private CharacterController controller;
    private bool isFlying = false;
    private bool isNoclip = false;
    private float normalSpeed = 5f;
    private float flySpeed = 10f;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (controller == null) return;

        Vector3 move = Vector3.zero;

        if (isFlying)
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            float y = 0f;
            if (Input.GetKey(KeyCode.Space)) y = 1f;
            if (Input.GetKey(KeyCode.LeftControl)) y = -1f;

            move = new Vector3(x, y, z) * flySpeed * Time.deltaTime;
        }
        else
        {
            float x = Input.GetAxis("Horizontal") * normalSpeed * Time.deltaTime;
            float z = Input.GetAxis("Vertical") * normalSpeed * Time.deltaTime;
            move = transform.TransformDirection(new Vector3(x, 0, z));
        }

        if (isNoclip)
        {
            controller.enabled = false; // disables collisions
            transform.position += move;
        }
        else
        {
            controller.enabled = true;
            controller.Move(move);
        }
    }

    public void ToggleFly() => isFlying = !isFlying;
    public void ToggleNoclip() => isNoclip = !isNoclip;
}
