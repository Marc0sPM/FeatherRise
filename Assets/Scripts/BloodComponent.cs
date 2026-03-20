using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BloodComponent : MonoBehaviour
{
    [SerializeField] private Image _bloodEffect;
    private float r;
    private float g;
    private float b;
    private float a;
    // Start is called before the first frame update
    void Start()
    {
        r = _bloodEffect.color.r;
        g = _bloodEffect.color.g;
        b = _bloodEffect.color.b;
        a = _bloodEffect.color.a;

    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.Soul == 1 && a < 0.05f)
        {

            a += 0.005f;
        }
        else if (GameManager.Instance.Soul > 1)
        {
            a = 0;
        }

        a = Mathf.Clamp(a, 0f, 1f);
        ChangeColor();

    }
    private void ChangeColor()
    {
        Color c = new Color(r, g, b, a);
        _bloodEffect.color = c;

    }
}
