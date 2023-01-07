using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ����Ϸ��� ī���� ������ ���� ���� �ڽ� ������Ʈ�� ����� ������
public class AreaBox : MonoBehaviour
{
    public static AreaBox instance;
    private void Awake() {
        instance = this;
        transform.Translate(5,5,0);
    }

    // ��� : �ڽ��� ����� �ٵ��� 3*3 ũ���� ���簢������ �ٲ�
    // ���� : GameManager.Areabox_set3_3()
    public void setSize3_3() {
        transform.localScale = new Vector3(1.55f, 1.55f, 1.55f);
    }

    // ��� : �ڽ��� ����� �ٵ��� 9*1 ũ���� ���簢������ �ٲ� (����)
    // ���� : GameManager.Areabox_set9_1()
    public void setSize9_1() {
        transform.localScale = new Vector3(5.05f, 0.59f, 1);
    }

    // ��� : �ڽ��� ����� �ٵ��� 1*9 ũ���� ���簢������ �ٲ� (����)
    // ���� : GameManager.Areabox_set1_9()
    public void setSize1_9() {
        transform.localScale = new Vector3(0.59f, 5.05f, 1);
    }

    // ��� : �ڽ��� ����� �ٵ��� 2*2 ũ���� ���簢������ �ٲ�
    // ���� : GameManager.Areabox_set2_2()
    public void setSize2_2()
    {
        transform.localScale = new Vector3(1.07f, 1.07f, 1.07f);
    }

}
