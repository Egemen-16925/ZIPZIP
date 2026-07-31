namespace Legacy2D {
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundSystem : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _firstSprite;
    [SerializeField] private SpriteRenderer _secondSprite;
    [SerializeField] private SpriteRenderer _thirdSprite;

    [SerializeField] private float _speed;
    [SerializeField] private float _reduceAmount;

    [SerializeField] private List<Sprite> _sprites;
    [SerializeField] private List<int> _obstacleChangeIndexes;

    [SerializeField] private Spawner _spawner;

    private float speed = 0;
    private int index = 3;
    private float distance;
    private Transform lastSprite;
    private List<Vector3> spriteStartPosses = new List<Vector3>();
    private void Start()
    {
        spriteStartPosses.Add(_firstSprite.transform.localPosition);
        spriteStartPosses.Add(_secondSprite.transform.localPosition);
        spriteStartPosses.Add(_thirdSprite.transform.localPosition);

        distance = Vector2.Distance(_firstSprite.transform.position, _secondSprite.transform.position) - _reduceAmount;
        lastSprite = _thirdSprite.transform;
    }

    private void Update()
    {
        _firstSprite.transform.localPosition += Vector3.left * speed * Time.deltaTime;
        _secondSprite.transform.localPosition += Vector3.left * speed * Time.deltaTime;
        _thirdSprite.transform.localPosition += Vector3.left * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Background"))
            return;
        Transform collideObject = collision.transform;
        collideObject.position = (Vector2)lastSprite.position + (Vector2.right * distance);
        lastSprite = collideObject;
        collideObject.GetComponent<SpriteRenderer>().sprite = _sprites[index];
        index++;
        if (index >= _sprites.Count)
            index = _sprites.Count - 2;

        if(_obstacleChangeIndexes.Contains(index))
            _spawner.SetNewObstacles((ObstacleTypes)_obstacleChangeIndexes.IndexOf(index));
    }

    public void StartMoving()
    {
        speed = _speed;
    }

    public void StopMoving()
    {
        speed = 0;
    }

    public void ResetBG()
    {
        speed = 0;
        _firstSprite.transform.localPosition = spriteStartPosses[0];
        _secondSprite.transform.localPosition = spriteStartPosses[1];
        _thirdSprite.transform.localPosition = spriteStartPosses[2];
        index = 3;
        _firstSprite.sprite = _sprites[0];
        _secondSprite.sprite = _sprites[1];
        _thirdSprite.sprite = _sprites[2];

        lastSprite = _thirdSprite.transform;
    }
}

}
