using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

// Resources/Player에 붙어있음
public class PlayerManager : MonoBehaviourPunCallbacks
{
    public static PlayerManager myPlayerManager = null;
    public static PlayerManager enemyPlayerManager = null;
    private void Awake() {
        if(myPlayerManager == null)
        {
            myPlayerManager = this;
        }
        else
        {
            enemyPlayerManager = this;
            if(PhotonNetwork.IsMasterClient) GameManager.instance.coinToss();
        }
        audioSource = this.gameObject.GetComponent<AudioSource>();
    }

    public List<CardData> cardDataBuffer;
    int[] cardindex;
    public cardDataSO cardDataSO;
    public GameObject cardPrefab;
    public PhotonView PV;
    AudioSource audioSource;
    public List<Card> myCards;
    public List<GameObject> myCardsGameObj;
    public Vector3 myCardsLeft;  // 가장 왼쪽 카드의 위치
    public Vector3 myCardsRight;  // 가장 오른쪽 카드의 위치

    public bool drawready=false;
    public Sprite drawimg;
    public GameObject character_img;

    public bool isCardSelected = false;

    private void Start() {
        if(PV.IsMine){ 
            SetupItemBuffer();  // cardDataBuffoer에 모든 CardData들을 랜덤하게 섞음
            cardindex =new int[cardDataBuffer.Count];
            for(int i=0; i<cardDataBuffer.Count; i++) {
                cardindex[i]=cardDataBuffer[i].indexNum;
            }
            PV.RPC("cardsyncro", RpcTarget.OthersBuffered, cardindex);
            AddFiveCard();
        }
        
        transform.position = new Vector3(-2,-3.8f);
        if(PV.IsMine)
        {
            transform.position = new Vector3(-2,-3.8f,80);
            myCardsLeft = new Vector3(-0.5f,-4.2f,0);
            myCardsRight = new Vector3(2.24f,-4.2f,0);
        }
        else
        {
            transform.position = new Vector3(-2,3.8f,80);
            myCardsLeft = new Vector3(-0.5f,4.2f,0);
            myCardsRight = new Vector3(2.24f,4.2f,0);

        }
    }

    [PunRPC] void cardsyncro(int[] indexs) {
        PlayerManager.enemyPlayerManager.cardDataBuffer=new List<CardData>(100);
        for(int i=0; i<indexs.Length; i++) {
            CardData item = PlayerManager.enemyPlayerManager.cardDataSO.items[indexs[i]];
            PlayerManager.enemyPlayerManager.cardDataBuffer.Add(item);
        }
        PlayerManager.enemyPlayerManager.AddFiveCard();
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.D) && PV.IsMine)
        {
            PV.RPC("destroyCard", RpcTarget.AllBuffered, 0);
        }
    }

    // 기능 : 카드 제거 (동기화)
    // 참조 : 카드를 발동하면 호출됨 (Card.OnMouseUp())
    public void destroyMe(int index)
    {
        PV.RPC("destroyCard", RpcTarget.AllBuffered, index);
    }

    // 기능 : 카드 제거 구현
    // 참조 : PlayerManager.destroyMe()
    [PunRPC] void destroyCard(int index)
    {
        if(!PV.IsMine){
            StartCoroutine(cardflip(index));
            StartCoroutine(delay(index));}
        else {
            Destroy(myCardsGameObj[index]);
            myCardsGameObj.RemoveAt(index);
            myCards.RemoveAt(index);
            for(int i = 0;i<myCards.Count;i++)
            {
                myCards[i].myHandIndex = i;
            }
            CardAlignment();
        }
    }

    // 참조 : PlayerManager.destroyCard()
    IEnumerator cardflip(int num) {
        Sequence seq=DOTween.Sequence();
        GameObject card=enemyPlayerManager.myCardsGameObj[num];
        Card cardscript=card.GetComponent<Card>();
        seq.Join(card.transform.DOMove(card.transform.position-new Vector3(0,5,0),0.75f).SetEase(Ease.OutQuad));
        seq.Join(cardscript.nameTMP.transform.DORotate(new Vector3(0,180,0),0.1f));
        seq.Join(cardscript.effectTMP.transform.DORotate(new Vector3(0,180,0),0.1f));
        seq.Append(card.transform.DORotate(new Vector3(0,180,0),0.5f));
        yield return new WaitForSeconds(0.96f);
        card.GetComponent<SpriteRenderer>().sprite=cardscript.cardFront;
        cardscript.characterSprite.sprite = cardscript.cardData.sprite;
        cardscript.nameTMP.text = cardscript.cardData.name;
        cardscript.effectTMP.text = cardscript.cardData.cardEffectInfoText;
    }

    // 참조 : PlayerManager.destroyCard()
    IEnumerator delay(int index) {
        yield return new WaitForSeconds(3f);
        Destroy(myCardsGameObj[index]);
        myCardsGameObj.RemoveAt(index);
        myCards.RemoveAt(index);
        for(int i = 0;i<myCards.Count;i++)
        {
            myCards[i].myHandIndex = i;
        }
        CardAlignment();
    }

    // 기능 : cardDataBuffer에서 가장 앞에 있는 CardData를 제거하면서 반환함 (cardDataBuffer는 이미 순서가 섞인 상태 - 랜덤하게 뽑는 효과)
    // 참조 : PlayerManager.AddCard()
    public CardData PopItem()
    {
        if(cardDataBuffer.Count==0)
            SetupItemBuffer();

        CardData item=cardDataBuffer[0];
        cardDataBuffer.RemoveAt(0);
        return item;
    }

    // 기능 : cardDataSO에 있는 모든 item들을 가져와서 cardDataBuffer에 저장한뒤, cardDataBuffer에 있는 CardData들을 섞음
    // 참조 : PlayerManager.Start()
    void SetupItemBuffer(){
        cardDataBuffer = new List<CardData>(100);
        for(int i =0;i<cardDataSO.items.Length; i++)  // cardDataSO에 있는 모든 item들을 가져와서 cardDataBuffer에 저장함
        {
            CardData item = cardDataSO.items[i];
            cardDataBuffer.Add(item);
        }

        for(int i =0;i<cardDataBuffer.Count;i++)  // 섞음
        {
            int rand = Random.Range(i,cardDataBuffer.Count);
            CardData temp = cardDataBuffer[i];
            cardDataBuffer[i]=cardDataBuffer[rand];
            cardDataBuffer[rand]=temp;
        }
    }


    int handcount = 0;

    // 기능 : 카드를 생성함
    // 참조 : PlayerManager.AddFiveCards()
    void AddCard()
    {
        if(PV.IsMine) 
        {
            var cardObject = Instantiate(cardPrefab,this.transform.position+new Vector3(30,20,0), Quaternion.identity);
            var card = cardObject.GetComponent<Card>();
            card.myHandIndex = handcount; handcount++;  // 카드에 번호를 부여함
            myCardsGameObj.Add(cardObject);
            card.Setup(PopItem(), PV.IsMine);
            myCards.Add(card);
        }
        else 
        {
            var cardObject = Instantiate(cardPrefab,this.transform.position+new Vector3(30,-20,0), Quaternion.identity);
            var card = cardObject.GetComponent<Card>();
            card.myHandIndex = handcount; handcount++;
            myCardsGameObj.Add(cardObject);
            card.Setup(PopItem(), PV.IsMine);
            myCards.Add(card);
        }

        audioSource.Play();
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
    public void AlignAfter1sec()
    {
        StartCoroutine(CorAlignAfter1sec());
    }

    IEnumerator CorAlignAfter1sec()
    {
        yield return new WaitForSeconds(1f);
        PV.RPC("CardAlignment", RpcTarget.AllBuffered);
    }

    // 기능 : 카드들을 재정렬함
    // 참조 : PlayerManager.destroyCard()
    [PunRPC] void CardAlignment()
    {
        float gap = myCardsRight.x - myCardsLeft.x;
        if(myCards.Count==1)
        {
            if(PV.IsMine)    myCards[0].transform.DOMove(new  Vector3(1,-4.2f,0),0.75f).SetEase(Ease.OutQuad);
            else myCards[0].transform.DOMove(new  Vector3(1,4.2f,0),0.75f).SetEase(Ease.OutQuad);
        }
        else
        {
            float interval = gap/(myCards.Count - 1);
            for(int i = 0;i<myCards.Count; i++)
            {
                myCards[i].transform.DOMove(myCardsLeft + 
                new Vector3(interval*i,0,0),0.75f).SetEase(Ease.OutQuad);
            }
        }
    }

    // 참조 : PlayerManager.Start()
    [PunRPC]public void AddFiveCard()
    {
        StartCoroutine(AddFiveCards());
    }

    // 기능 : 게임에서 사용할 5개의 카드를 생성함
    // 참조 : PlayerManager.AddFiveCard()
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


    [Header("Health Point")]
    public int MyHP = 3;  // HP, 기본값 3
    public GameObject hp1; public GameObject hp2; public GameObject hp3;

    // 기능 : 데미지를 입을 때 호출됨
    // 참조 : GameManager.reNewalBoard()
    public void GetDamaged()
    {
        MyHP--;
        renewalHPBar();
    }

    // 기능 : HP 상태에 따라 HPBar가 보여지는 모습을 조절함
    // 참조 : PlayerNamager.GetDamaged()
    void renewalHPBar()
    {
        if(MyHP==2) hp3.SetActive(false);
        else if(MyHP==1) {hp3.SetActive(false); hp2.SetActive(false);}
        else
        {
            hp3.SetActive(false); hp2.SetActive(false);
            hp1.SetActive(false);
            if(PV.IsMine) GameManager.instance.LoseGame();  // HP가 다 떨어지면 게임 패배
        }
    }

    
}

/*
 * PlayerManager 스크립트는 총 4개가 존재하게 됨 (PC1-A!,B, PC2-A,B!)
 */