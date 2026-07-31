namespace Legacy2D {
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasSpriteSheet : MonoBehaviour
{
    [SerializeField] private List<Sprite> _sprites;
    [SerializeField] private int _speed;

    private Image image;
    private Coroutine coroutine;

    private void Start()
    {
        image = GetComponent<Image>();

        coroutine = StartCoroutine(SpriteSheet());
    }

    private void OnDisable()
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
    }

    private IEnumerator SpriteSheet()
    {
        int index = 0;
        while (true)
        {
            index++;
            if(index >= _sprites.Count)
                index = 0;

            image.sprite = _sprites[index];

            for (int i = 0; i < _speed; i++)
            {
                yield return new WaitForEndOfFrame();
            }
        }
    }
}

}
