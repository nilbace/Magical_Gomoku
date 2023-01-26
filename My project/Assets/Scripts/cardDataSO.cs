using UnityEngine;



[System.Serializable]
public class CardData
{
    public string name;  // ī�� �̸�
    public int indexNum;  // ī�� �ε��� ��ȣ
    public Sprite sprite;  // sprite
}

// ��ũ���ͺ� ������Ʈ (Scriptable Object)
[CreateAssetMenu(fileName = "cardDataSO", menuName = "Scriptable Object/cardDataSO", order = 0)]
public class cardDataSO : ScriptableObject {
    public CardData[] items;  // ī�� �����͵��� �迭
}
