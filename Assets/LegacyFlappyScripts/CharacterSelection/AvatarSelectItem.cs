namespace Legacy2D {
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class AvatarSelectItem : MonoBehaviour
{
    [SerializeField] private GameObject _selectedSprite;

    public async void Select()
    {
        await Task.Delay(10);
        ToggleButton(true);
    }

    public void ToggleButton(bool state)
    {
        _selectedSprite.SetActive(state);
    }
}

}
