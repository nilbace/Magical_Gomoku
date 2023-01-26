using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Staff : MonoBehaviour
{
    public GameObject startPannel;
    public GameObject staffPannel;


    private void OnMouseDown()
    {
        if (staffPannel.activeSelf == true)
        {
            startPannel.SetActive(true);
            staffPannel.SetActive(false);
        }
    }

}