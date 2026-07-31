namespace Legacy2D {
using UnityEngine;

public class Parallax : MonoBehaviour
{
    public float animationSpeed = 1f;
    private MeshRenderer meshRenderer;
    public bool canMove = false;
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (!canMove)
            return;

        meshRenderer.material.mainTextureOffset += new Vector2(animationSpeed * Time.deltaTime, 0);
    }

}

}
