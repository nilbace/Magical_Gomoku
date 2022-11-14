using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public List<CardData> cardDataBuffer;
    public cardDataSO cardDataSO;
    public GameObject cardPrefab;
    public PhotonView PV;

    public List<Card> myCards;
    public Vector3 myCardsLeft;
    public Vector3 myCardsRight;

    private void Start() {
        SetupItemBuffer();
        transform.position = new Vector3(-2,-3.8f);
        if(PV.IsMine)
        {
            transform.position = new Vector3(-2,-3.8f,0);
            myCardsLeft = new Vector3(-0.5f,-4.2f,0);
            myCardsRight = new Vector3(2.24f,-4.2f,0);
        }
        else
        {
            transform.position = new Vector3(-2,3.8f,0);
            myCardsLeft = new Vector3(-0.5f,4.2f,0);
            myCardsRight = new Vector3(2.24f,4.2f,0);

        }

        AddFiveCard();
    }

    public CardData PopItem()
    {
        if(cardDataBuffer.Count==0)
            SetupItemBuffer();

        CardData item=cardDataBuffer[0];
        cardDataBuffer.RemoveAt(0);
        return item;
    }

    void SetupItemBuffer(){
        cardDataBuffer = new List<CardData>(100);
        for(int i =0;i<cardDataSO.items.Length; i++)
        {
            CardData item = cardDataSO.items[i];
            cardDataBuffer.Add(item);
        }

        for(int i =0;i<cardDataBuffer.Count;i++)
        {
            int rand = Random.Range(i,cardDataBuffer.Count);
            CardData temp = cardDataBuffer[i];
            cardDataBuffer[i]=cardDataBuffer[rand];
            cardDataBuffer[rand]=temp;
        }
    }

    

    void AddCard()
    {
        var cardObject = Instantiate(cardPrefab, new Vector2(-20,-20), Quaternion.identity);
        var card = cardObject.GetComponent<Card>();
        card.Setup(PopItem(), PV.IsMine);
        myCards.Add(card);

        SetOriginOrder();
        CardAlignment();
    }

    void SetOriginOrder()
    {
        if(PV.IsMine)
        {
            int count = myCards.Count;
            for(int i = 0; i<count; i++)
            {
                var targetCard = myCards[i];
                targetCard.GetComponent<Card>().SetoriginOrder(i);
            }
        }
    }

    void CardAlignment()
    {
        float gap = myCardsRight.x - myCardsLeft.x;
        float interval = gap/(myCards.Count - 1);
        for(int i = 0;i<myCards.Count; i++)
        {
            myCards[i].transform.position = myCardsLeft + 
            new Vector3(interval*i,0,0);
        }
    }


    [PunRPC]public void AddFiveCard()
    {
        StartCoroutine(AddFiveCards());
    }

    IEnumerator AddFiveCards()
    {
        AddCard();
        yield return new WaitForSeconds(0.2f);
        AddCard();
        yield return new WaitForSeconds(0.2f);
        AddCard();
        yield return new WaitForSeconds(0.2f);
        AddCard();
        yield return new WaitForSeconds(0.2f);
        AddCard();
    }
}
