using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using DG.Tweening;

public class Card : MonoBehaviourPunCallbacks
{
    public SpriteRenderer cardSprite;
    public bool isMine;
    public Sprite cardFront;
    public Sprite cardBack;  // 카드 뒷면 sprite

    public CardData cardData;  // 이 카드의 정보
    public int myHandIndex = -1;  // 이 카드의, 게임 상의 순서 (인덱스 번호)

    Vector3 originPos;  // 이 카드의 원래 위치 (아무 동작도 하지 않으면 카드 5개가 게임 하단, 캐릭터 우측에 배치되어있음)
    Vector3 offset;


    // 기능 : 카드를 설정해줌
    // 매개변수 : cardData (카드의 정보), isMine 
    // 참조 : PlayerManager.AddCard()
    public void Setup(CardData cardData, bool isMine)
    {
        this.cardData = cardData;
        this.isMine = isMine;
        this.cardFront = cardData.sprite;

        if(this.isMine)  // 카드 앞면
        {
            cardSprite.sprite = this.cardData.sprite;
        }
        else  // 카드 뒷면
        {
            cardSprite.sprite = cardBack; 
        }
    }

    // 기능 : 마우스를 카드 위에 올려놓으면 카드가 커지게 함 (클릭 X)
    private void OnMouseOver() {
        if(isMine)  // 내 카드만 조작할 수 있게 함
        {
            bool enlarge = true;
            EnlargeCard(enlarge);
        }
            
    }

    // 기능 : 마우스를 카드 위에서 카드 밖으로 움직이면 다시 카드가 작아지게 함 (클릭 X)

    private void OnMouseExit() {
        if(isMine)
        {
            bool exit = false;
            EnlargeCard(exit);
        }

        
    }

    // OnMouseDown, OnMouseDrag, OnMouseUp : 마우스 클릭 이벤트 함수


    private void OnMouseDown()
    {  // 이 카드를 클릭(터치)한 순간 호출됨
        if (isMine)
            originPos = transform.position;  // 카드의 원래 위치를 저장해둠
    }

    private void OnMouseDrag()
    {  // 이 카드를 움직일 때 호출됨
        if (isMine)
            transform.position = MouseWorldPosition() + offset;
    }

    private void OnMouseUp()
    {  // 이 카드를 누르고 있던 상태에서 떼는 순간 호출됨
        if (isMine) 
        {
            // 카드를 발동하던 하지 않던 카드는 원래 위치로 되돌림
            // 카드를 발동하면 카드를 파괴하므로 의미가 없지만, 카드를 발동하지 않으면 카드가 원래 있었던 위치로 되돌아가게 됨
            transform.position = originPos;  // 카드의 위치를 원래 이 카드가 있었던 위치로
            
            transform.DOKill();
            transform.localScale = new Vector2(2.09f, 2.09f);
            if (GameManager.instance.canuseCard && MouseWorldPosition().y>0)  // 이 카드를 사용할 수 있는 상태이고, 이 카드의 월드 좌표가 0보다 크면
            {
                GameManager.instance.setMyuseCardStatus(cardData.indexNum);
                GameManager.instance.canuseCard= false;
                PlayerManager.myPlayerManager.destroyMe(myHandIndex);
                if (cardData.indexNum == 3)
                    GameManager.instance.setChangeEnemyStone();
                else if (cardData.indexNum == 7)
                    GameManager.instance.setCardIndexSeven();
            }
        }

    }


    // 기능 : 현재 터치하고 있는 부분의 월드 좌표를 반환함
    // 참조 : 카드를 움직일 때(Card.OnMouseDrag()), 카드를 누르고 있떤 상태에서 떼는 순간(Card.OnMouseUp())
    Vector3 MouseWorldPosition()
    {
        // Input.mousePosition : Screen을 바로 출력해서 해상도가 1920*1080 이면 오른쪽 위를 클릭하면 (1920, 1080) 을 반환함
        var mouseScreenpos = Input.mousePosition;  // 현재 마우스의 screen 좌표
        mouseScreenpos.z = 0;  // 평면이므로 z값을 0으로

        var returnVal = Camera.main.ScreenToWorldPoint(mouseScreenpos);  // 스크린 좌표를 월드 좌표로 변환
        returnVal.z = 0;  // 평면이므로 z값을 0으로

        return returnVal;
    }


    // 기능 : 보여지는 카드 크기 조절
    void EnlargeCard(bool isEnlarge)
    {
        if(isEnlarge)
        {
            transform.localScale = new Vector2(0.25f, 0.25f);
            if(transform.position.y+4.2f>=2.8f) {
                transform.DOScale(new Vector3(0.5f,0.5f,0),0.25f);// 카드 크기를 키움
            }  
        }
        else
        {
            transform.localScale = new Vector2(0.2f, 0.2f);  // 카드 크기를 원래대로 되돌림
        }
    }
    

    int originOrder;
    public Renderer[] backRenderers;
    public string sortingLayerName;
    public void SetoriginOrder(int originOrder)
    {
        this.originOrder=originOrder;
        SetOrder(originOrder);
    }

    public void SetOrder(int order)
    {
        int mulOrder = order*10;
        foreach(var renderer in backRenderers)
        {
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = mulOrder;
        }
    }   

    public void SetMostFrontOrder(bool isMostFront)
    {
        SetOrder(isMostFront?100:originOrder);
    }
}

/*
카드 구성
- 상단 : 카드 이름
- 하단 : 카드 설명
- 카드 앞면 배경 이미지
- 카드 뒷면 이미지
- 각 카드 캐릭터

인덱스가 2가지가 존재 
- 1. card 자체의 인덱스번호 : cardData.indexNum
- 게임 상에서 카드가 놓여진 순서 : this.myHandIndex

 */