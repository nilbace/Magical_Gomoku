using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class Card : MonoBehaviourPunCallbacks
{
    public TMP_Text nameTMP;
    public TMP_Text effectTMP;
    public SpriteRenderer characterSprite;
    public SpriteRenderer cardSprite;
    public bool isMine;
    public Sprite cardBack;
    public Sprite cardFront;

    public CardData cardData;
    public int myHandIndex = -1;

    Vector3 originPos;
    Vector3 offset;

    
    public void Setup(CardData cardData, bool isMine)
    {
        this.cardData = cardData;
        this.isMine = isMine;

        if(this.isMine)
        {
            characterSprite.sprite = this.cardData.sprite;
            nameTMP.text = this.cardData.name;
            effectTMP.text = this.cardData.cardEffectInfoText;
        }
        else
        {
            cardSprite.sprite = cardBack;
            nameTMP.text = "";
            effectTMP.text = "";
            characterSprite.sprite = null;   
        }
    }

    private void OnMouseOver() {
        if(isMine )
        {
            bool enlarge = true;
            EnlargeCard(enlarge);
        }
            
    }

    private void OnMouseExit() {
        if(isMine)
        {
            bool exit = false;
            EnlargeCard(exit);
        }

        
    }

    private void OnMouseDown() {
        if(isMine)originPos = transform.position;
    }

    private void OnMouseDrag() {
        if(isMine) transform.position = MouseWorldPosition() + offset;
    }

    private void OnMouseUp() {
        if(isMine) 
        {
            transform.position = originPos;

            if(GameManager.instance.canuseCard && MouseWorldPosition().y>0) //발동
            {
                GameManager.instance.setMyuseCardStatus(cardData.indexNum);
                GameManager.instance.canuseCard= false;
                PlayerManager.myPlayerManager.destroyMe(myHandIndex);
                if(cardData.indexNum==3) GameManager.instance.setChangeEnemyStone();
            }
        }

    }



    Vector3 MouseWorldPosition()
    {
        var mouseScreenpos = Input.mousePosition;
        mouseScreenpos.z = 0;
        var returnVal = Camera.main.ScreenToWorldPoint(mouseScreenpos);
        returnVal.z = 0;
        return returnVal;
    }

   

    void EnlargeCard(bool isEnlarge)
    {
        if(isEnlarge)
        {
            transform.localScale = new Vector2(4,4);
        }
        else
        {
            transform.localScale = new Vector2(2f,2f);
        }
    }
    

    int originOrder;
    public Renderer[] backRenderers;
    public Renderer[] middleRenderers;
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
        foreach(var renderer in middleRenderers)
        {
            renderer.sortingLayerName=sortingLayerName;
            renderer.sortingOrder=mulOrder+1;
        }
    }   

    public void SetMostFrontOrder(bool isMostFront)
    {
        SetOrder(isMostFront?100:originOrder);
    }
}
