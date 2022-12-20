using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaBox : MonoBehaviour
{
    public static AreaBox instance;
    private void Awake() {
        instance = this;
        transform.Translate(5,5,0);
    }
    
    public void setSize3_3() {
        transform.localScale = new Vector3(1.55f, 1.55f, 1.55f);
    }

    public void setSize9_1() {
        transform.localScale = new Vector3(5.05f, 0.59f, 1);
    }

    public void setSize1_9() {
        transform.localScale = new Vector3(0.59f, 5.05f, 1);
    }

}
