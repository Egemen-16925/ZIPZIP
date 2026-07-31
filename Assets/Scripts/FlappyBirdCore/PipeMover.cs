using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeMover : MonoBehaviour
{
    public Transform spawner;
    public bool canMove = false;
    public bool moveSpawner = false;
    public float speed = 5f;

    void LateUpdate()
    {
        if (moveSpawner)
            spawner.Translate(Vector3.right * speed * Time.deltaTime);

        if (!canMove)
            return;

        transform.Translate(Vector3.left * speed * Time.deltaTime);
    }
}
