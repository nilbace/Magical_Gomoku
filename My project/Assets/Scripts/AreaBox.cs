using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 사용하려는 카드의 종류에 따라 영역 박스 오브젝트의 모양을 조절함
public class AreaBox : MonoBehaviour
{
    public static AreaBox instance;
    private void Awake() {
        instance = this;
        transform.Translate(5,5,0);
    }

    // 기능 : 박스의 모양을 바둑판 3*3 크기의 정사각형으로 바꿈
    // 참조 : GameManager.Areabox_set3_3()
    public void setSize3_3() {
        transform.localScale = new Vector3(1.55f, 1.55f, 1.55f);
    }

    // 기능 : 박스의 모양을 바둑판 9*1 크기의 직사각형으로 바꿈 (가로)
    // 참조 : GameManager.Areabox_set9_1()
    public void setSize9_1() {
        transform.localScale = new Vector3(5.05f, 0.59f, 1);
    }

    // 기능 : 박스의 모양을 바둑판 1*9 크기의 직사각형으로 바꿈 (세로)
    // 참조 : GameManager.Areabox_set1_9()
    public void setSize1_9() {
        transform.localScale = new Vector3(0.59f, 5.05f, 1);
    }

    // 기능 : 박스의 모양을 바둑판 2*2 크기의 정사각형으로 바꿈
    // 참조 : GameManager.Areabox_set2_2()
    public void setSize2_2()
    {
        transform.localScale = new Vector3(1.07f, 1.07f, 1.07f);
    }

}
