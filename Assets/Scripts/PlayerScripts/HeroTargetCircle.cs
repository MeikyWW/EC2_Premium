using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class HeroTargetCircle : MonoBehaviour
{
    public float moveSpd;
    public CharacterController _controller;

    [LabelText("Turn Speed")]
    public float rotateSpeed = 20;
    private float gravity = 25;

    public bool reduceSizeOvertime;
    [ShowIf("reduceSizeOvertime")]
    public Transform targetResize;
    [ShowIf("reduceSizeOvertime")]
    public float reductonPerSecond = 0.1f;
    [ShowIf("reduceSizeOvertime")]
    public float minSize = 0.6f;

    private float currentSize;

    public void MovementUpdate(float h, float v, float deltaTime)
    {
        if (h != 0 || v != 0)
        {
            Rotating(h, v, deltaTime);
            _controller.Move(transform.forward * moveSpd * deltaTime);
            _controller.Move(Vector3.down * gravity * deltaTime);
        }
    }
    void Rotating(float horizontal, float vertical, float deltaTime)
    {
        Vector3 axisDirection = new Vector3(horizontal, 0, vertical);
        Vector3 targetDirection = Camera.main.transform.TransformDirection(axisDirection);
        targetDirection.y = 0;

        Quaternion lookRot = Quaternion.LookRotation(targetDirection);
        Vector3 rot = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * deltaTime).eulerAngles;
        transform.eulerAngles = rot;
    }

    private void Start()
    {
        currentSize = transform.localScale.x;
    }
    private void Update()
    {
        if (reduceSizeOvertime)
        {
            if (currentSize < minSize) return;
            currentSize -= reductonPerSecond * Time.deltaTime;
            targetResize.localScale = Vector3.one * currentSize;
        }
    }
}
