using UnityEngine;

public class Pipes : MonoBehaviour
{
    public float minHeight = -1f;
    public float maxHeight = 2f;

    private float leftEdge;

    [SerializeField] private bool canMove;

    private void Start()
    {
        Vector3 pos = transform.localPosition;

        if (!canMove)
            pos.y = 0;
        else
            pos.y = Random.Range(minHeight, maxHeight);

        transform.localPosition = pos;

    }

    private void Update()
    {
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 5f;

        if (transform.position.x < leftEdge)
        {
            Destroy(gameObject);
        }
    }

}
