using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class coll : MonoBehaviour
{

    void Start()
    {
       
    }

    void Update()
    {
       
    }

    void OnCollisionEnter(Collision colli) { 
       Debug.Log("충돌Enter");

       Debug.Log(colli.gameObject.name);
    }
}