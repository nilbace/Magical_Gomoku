using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

// Resources/Player�� �پ�����
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
    public Vector3 myCardsLeft;  // ���� ���� ī���� ��ġ
    public Vector3 myCardsRight;  // ���� ������ ī���� ��ġ

    public bool drawready=false;
    public Sprite drawimg;
    public GameObject character_img;
    public Sprite omokman;

    private void Start() {
        if(PV.IsMine){ 
            SetupItemBuffer();  // cardDataBuffoer�� ��� CardData���� �����ϰ� ����
            cardindex =new int[cardDataBuffer.Count];
            for(int i=0; i<cardDataBuffer.Count; i++) {
                cardindex[i]=cardDataBuffer[i].indexNum;
            }
            PV.RPC("cardsyncro", RpcTarget.OthersBuffered, cardindex);
            AddFiveCard();
            character_img.GetComponent<SpriteRenderer>().sprite=omokman;
        }
        
        transform.position = new Vector3(-2,-3.8f);
        if(PV.IsMine)
        {
            transform.position = new Vector3(-2,-3.8f,80);
            myCardsLeft = new Vector3(-0.5f,-3.9f,0);
            myCardsRight = new Vector3(2.24f,-3.9f,0);
        }
        else
        {
            transform.position = new Vector3(2f,3.5f,80);
            myCardsLeft = new Vector3(-2f,3.1f,0);
            myCardsRight = new Vector3(1f,3.1f,0);

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

    // ��� : ī�� ���� (����ȭ)
    // ���� : ī�带 �ߵ��ϸ� ȣ��� (Card.OnMouseUp())
    public void destroyMe(int index)
    {
        PV.RPC("destroyCard", RpcTarget.AllBuffered, index);
    }

    // ��� : ī�� ���� ����
    // ���� : PlayerManager.destroyMe()
    [PunRPC] void destroyCard(int index)
    {
        audioSource.Play();
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

    // ���� : PlayerManager.destroyCard()
    IEnumerator cardflip(int num) {
        Sequence seq=DOTween.Sequence();
        GameObject card=enemyPlayerManager.myCardsGameObj[num];
        Card cardscript=card.GetComponent<Card>();
        seq.Join(card.transform.DOMove(card.transform.position-new Vector3(0,5,0),0.75f).SetEase(Ease.OutQuad));
        seq.Append(card.transform.DORotate(new Vector3(0,180,0),0.5f));
        yield return new WaitForSeconds(0.95f);
        card.GetComponent<SpriteRenderer>().flipX=true;
        card.GetComponent<SpriteRenderer>().sprite=cardscript.cardFront;
    }

    // ���� : PlayerManager.destroyCard()
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

    // ��� : cardDataBuffer���� ���� �տ� �ִ� CardData�� �����ϸ鼭 ��ȯ�� (cardDataBuffer�� �̹� ������ ���� ���� - �����ϰ� �̴� ȿ��)
    // ���� : PlayerManager.AddCard()
    public CardData PopItem()
    {
        if(cardDataBuffer.Count==0)
            SetupItemBuffer();

        CardData item=cardDataBuffer[0];
        cardDataBuffer.RemoveAt(0);
        return item;
    }

    // ��� : cardDataSO�� �ִ� ��� item���� �����ͼ� cardDataBuffer�� �����ѵ�, cardDataBuffer�� �ִ� CardData���� ����
    // ���� : PlayerManager.Start()
    void SetupItemBuffer(){
        cardDataBuffer = new List<CardData>(100);
        for(int i =0;i<cardDataSO.items.Length; i++)  // cardDataSO�� �ִ� ��� item���� �����ͼ� cardDataBuffer�� ������
        {
            CardData item = cardDataSO.items[i];
            cardDataBuffer.Add(item);
        }

        for(int i =0;i<cardDataBuffer.Count;i++)  // ����
        {
            int rand = Random.Range(i,cardDataBuffer.Count);
            CardData temp = cardDataBuffer[i];
            cardDataBuffer[i]=cardDataBuffer[rand];
            cardDataBuffer[rand]=temp;
        }
    }


    int handcount = 0;

    // ��� : ī�带 ������
    // ���� : PlayerManager.AddFiveCards()
    void AddCard()
    {
        if(PV.IsMine) 
        {
            var cardObject = Instantiate(cardPrefab,this.transform.position+new Vector3(30,20,0), Quaternion.identity);
            var card = cardObject.GetComponent<Card>();
            card.myHandIndex = handcount; handcount++;  // ī�忡 ��ȣ�� �ο���
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

    // ��� : ī����� ��������
    // ���� : PlayerManager.destroyCard()
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

    // ���� : PlayerManager.Start()
    [PunRPC]public void AddFiveCard()
    {
        StartCoroutine(AddFiveCards());
    }

    // ��� : ���ӿ��� ����� 5���� ī�带 ������
    // ���� : PlayerManager.AddFiveCard()
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
    public int MyHP = 3;  // HP, �⺻�� 3
    public GameObject hp1; public GameObject hp2; public GameObject hp3;

    // ��� : �������� ���� �� ȣ���
    // ���� : GameManager.reNewalBoard()
    public void GetDamaged()
    {
        MyHP--;
        renewalHPBar();
    }

    // ��� : HP ���¿� ���� HPBar�� �������� ����� ������
    // ���� : PlayerNamager.GetDamaged()
    void renewalHPBar()
    {
        if(MyHP==2) hp3.SetActive(false);
        else if(MyHP==1) {hp3.SetActive(false); hp2.SetActive(false);}
        else
        {
            hp3.SetActive(false); hp2.SetActive(false);
            hp1.SetActive(false);
            if(PV.IsMine) GameManager.instance.LoseGame();  // HP�� �� �������� ���� �й�
        }
    }

    
}

/*
 * PlayerManager ��ũ��Ʈ�� �� 4���� �����ϰ� �� (PC1-A!,B, PC2-A,B!)
 */