using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Este script sera llamado desde otros scripts y no se asignara a ningun objeto
[System.Serializable]
public class Textos
{
    [TextArea(2, 6)] //Indicamos entre parentesis el minimo y maximo de lineas
    public string[] _arrayText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
