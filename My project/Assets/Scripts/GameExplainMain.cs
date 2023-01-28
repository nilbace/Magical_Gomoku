using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameExplainMain : MonoBehaviour
{
    public GameObject SettingPannel;
    public GameObject GameExplainPannel;

    private void OnMouseDown()
    {
        if (GameExplainPannel.activeSelf)
        {
            GameExplainPannel.SetActive(false);
            SettingPannel.SetActive(true);
        }
    }
}
