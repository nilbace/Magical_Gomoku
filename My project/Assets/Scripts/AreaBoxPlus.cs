using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 사용하려는 카드의 종류에 따라 영역 박스 오브젝트의 모양을 조절함. 추가적으로 사용하는 영역 박스
public class AreaBoxPlus : MonoBehaviour
{
    public static AreaBoxPlus instance;
    private void Awake()
    {
        instance = this;
        transform.Translate(8, 5, 0);
    }

    // 기능 : 박스의 모양을 바둑판 9*1 크기의 직사각형으로 바꿈 (가로)
    // 참조 : GameManager.AreaboxPlus_set9_1()
    public void setSize9_1()
    {
        transform.localScale = new Vector3(5.05f, 0.59f, 1);
    }

    // 기능 : 박스의 모양을 바둑판 2*2 크기의 정사각형으로 바꿈
    // 참조 : GameManager.
    public void setSize2_2()
    {
        transform.localScale = new Vector3(1.07f, 1.07f, 1.07f);
    }
}
