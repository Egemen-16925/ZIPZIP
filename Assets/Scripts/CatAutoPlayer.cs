using UnityEngine;

public class CatAutoPlayer : MonoBehaviour
{
    private void Start()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            anim.Play(0, 0, 0f);
        }
    }
}
