using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Staff : MonoBehaviour
{
    public GameObject staffPannel;

    private void OnMouseDown()
    {
        if (staffPannel.activeSelf == true)
        {
            staffPannel.SetActive(false);
        }
    }
}