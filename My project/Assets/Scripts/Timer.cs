using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [SerializeField] float time;
    public Image img;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //30초에 한바퀴
        transform.Rotate(Vector3.back*Time.deltaTime*6);
        img.fillAmount = (time / 60);
        //시간체크용 인스펙터
        time += Time.deltaTime;
    }

    
}