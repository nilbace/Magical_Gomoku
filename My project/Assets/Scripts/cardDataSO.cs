using UnityEngine;



[System.Serializable]
public class CardData
{
    public string name;
    public int indexNum;
    public string cardEffectInfoText;
    public Sprite sprite;
}


[CreateAssetMenu(fileName = "cardDataSO", menuName = "Scriptable Object/cardDataSO", order = 0)]
public class cardDataSO : ScriptableObject {
    public CardData[] items;
}
