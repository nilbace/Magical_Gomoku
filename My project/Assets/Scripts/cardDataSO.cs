using UnityEngine;



[System.Serializable]
public class CardData
{
    public string name;  // 카드 이름
    public int indexNum;  // 카드 인덱스 번호
    public string cardEffectInfoText;  // 카드 효과
    public Sprite sprite;  // sprite
}

// 스크립터블 오브젝트 (Scriptable Object)
[CreateAssetMenu(fileName = "cardDataSO", menuName = "Scriptable Object/cardDataSO", order = 0)]
public class cardDataSO : ScriptableObject {
    public CardData[] items;  // 카드 데이터들의 배열
}
