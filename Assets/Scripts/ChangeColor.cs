using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ChangeColor : MonoBehaviour
{
    [SerializeField] private Light2D _light;
    [SerializeField] private RawImage _image;
    
    // Start is called before the first frame update
    private void Start() //Player inicia el juego con el color asignado
    {
        if ((bool)this.GetComponent<InputComponent>())
            _light.color = SkinManager.Instance.Col;
        else ChangeColors(SkinManager.Instance.Col);
    }
    public void ChangeColors(Color color) //metodo de muestra en img player
    {
        _image.color = color;
    }
}
