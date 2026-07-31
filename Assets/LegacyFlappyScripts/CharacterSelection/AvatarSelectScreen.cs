namespace Legacy2D {
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarSelectScreen : MonoBehaviour
{
    [SerializeField] private List<AvatarSelectItem> _avatarSelectItem;
    [SerializeField] private Transform _layout;
    [SerializeField] private Transform _target;
  
    public void CloseAllItems()
    {
        _avatarSelectItem.ForEach(x => x.ToggleButton(false));
    }

    public void Move(int dir)
    {
        if(dir == 1)
        {
            _layout.transform.position = _target.position;
        }

        else
        {
            _layout.transform.localPosition = Vector3.zero;
        }
    }
}

}
