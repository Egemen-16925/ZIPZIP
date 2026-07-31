using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public List<Pipes> level1Obstacles;
    public List<Pipes> level2Obstacles;
    public List<Pipes> level3Obstacles;
    public List<Pipes> level4Obstacles;
    public List<Pipes> level5Obstacles;
    public Transform parent;
    public float spawnRate = 1f;
    private List<Pipes> currentObstacles;
    private void OnDisable()
    {
        CancelInvoke(nameof(Spawn));
    }

    private void Start()
    {
        SetNewObstacles(ObstacleTypes.Level1);
    }

    private void Spawn()
    {
        Pipes pipes = Instantiate(currentObstacles[Random.Range(0, currentObstacles.Count)], transform.position, Quaternion.identity);
        pipes.transform.parent = parent;
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(Spawn));
    }

    public void StartSpawning()
    {
        InvokeRepeating(nameof(Spawn), spawnRate, spawnRate);
    }

    public void SetNewObstacles(ObstacleTypes index)
    {
        switch (index)
        {
            case ObstacleTypes.Level1:
                currentObstacles = level1Obstacles;
                break;

            case ObstacleTypes.Level2:
                currentObstacles = level2Obstacles;
                break;

            case ObstacleTypes.Level3:
                currentObstacles = level3Obstacles;
                break;

            case ObstacleTypes.Level4:
                currentObstacles = level4Obstacles;
                break;

            case ObstacleTypes.Level5:
                currentObstacles = level5Obstacles;
                break;
        }
    }
}

[System.Serializable]
public enum ObstacleTypes
{
    Level1,
    Level2,
    Level3,
    Level4,
    Level5
}
