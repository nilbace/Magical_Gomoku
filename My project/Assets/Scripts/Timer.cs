using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] float time;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //30초에 한바퀴
        transform.Rotate(Vector3.forward*Time.deltaTime*12);
        //시간체크용 인스펙터
        time += Time.deltaTime;
    }

    
}
