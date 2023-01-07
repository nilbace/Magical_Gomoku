using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ����Ϸ��� ī���� ������ ���� ���� �ڽ� ������Ʈ�� ����� ������. �߰������� ����ϴ� ���� �ڽ�
public class AreaBoxPlus : MonoBehaviour
{
    public static AreaBoxPlus instance;
    private void Awake()
    {
        instance = this;
        transform.Translate(8, 5, 0);
    }

    // ��� : �ڽ��� ����� �ٵ��� 9*1 ũ���� ���簢������ �ٲ� (����)
    // ���� : GameManager.AreaboxPlus_set9_1()
    public void setSize9_1()
    {
        transform.localScale = new Vector3(5.05f, 0.59f, 1);
    }

    // ��� : �ڽ��� ����� �ٵ��� 2*2 ũ���� ���簢������ �ٲ�
    // ���� : GameManager.
    public void setSize2_2()
    {
        transform.localScale = new Vector3(1.07f, 1.07f, 1.07f);
    }
}
