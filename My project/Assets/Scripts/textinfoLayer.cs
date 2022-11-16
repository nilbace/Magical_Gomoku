using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class textinfoLayer : MonoBehaviour
{
    public Renderer textRenderer;
    void Start()
    {
        textRenderer.sortingLayerName = "UI";
        textRenderer.sortingOrder = 2;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
