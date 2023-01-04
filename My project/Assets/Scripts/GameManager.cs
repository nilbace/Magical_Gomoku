using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class GameManager : MonoBehaviourPunCallbacks
{
    //싱글턴
    public static GameManager instance;
    private void Awake() {
        instance = this;
    }


    public PhotonView PV;
    public Button[] gomokuTable;
    public Sprite whiteStone; 
    public Sprite blackStone; 
    public int[] gomokuData = new int[81];
    
    int deleteStartNum;
    public ParticleSystem part;
    public ParticleSystem part2;
    public ParticleSystem myshooting;
    public ParticleSystem enemyshooting;
    AudioSource audioSource;
    AudioSource turnsfx;

    
    enum stoneColor{ black = 1, white = 2 }

    public bool canuseCard; //카드를 드래그했을때 써지는지 여부 금방금방 꺼짐
    
    public void Start() {
        resetGameData();
        unInteractableAllBTN();
        repaintBoard();
        turnsfx = this.gameObject.GetComponent<AudioSource>();
        audioSource = gomokuTable[0].gameObject.GetComponent<AudioSource>();
    }

    

    #region 턴관련
    public bool isMyTurn = false;

    public void coinToss()
    {
        StartCoroutine(coinTossProcess());
    }

    IEnumerator coinTossProcess()
    {
        yield return new WaitForSeconds(1f);
        int tmp = Random.Range(0,2);
        if(tmp == 0) startMyTurn();
        else PV.RPC("startMyTurn", RpcTarget.OthersBuffered);
    }

    [PunRPC] void startMyTurn()
    {
        isMyTurn = true;
        canuseCard = true;
        for(int i = 0;i <81;i++)
        {
            if(gomokuData[i]==0) gomokuTable[i].interactable=true;
        }
        turnsfx.Play();
        NetWorkManager.instance.printScreenString("나의 턴");
    }

    void endMyTurn()
    {
        isMyTurn = false;
        canuseCard = false;
        for(int i = 0;i <81;i++)
        {
            gomokuTable[i].interactable=false;
        }
        turnsfx.Play();
        PV.RPC("startMyTurn", RpcTarget.OthersBuffered);
    }
    #endregion

    #region 오목관련+카드
    enum MyHandStatus{
        cannotUseCard = -1,
        reassignment3_3, deleteVertical, putStoneTwice, changeEnemyStone, reverseStone3_3} // 지금이 어떤 상태인지 카드를 쓰고 있는 중인지 아닌지

    public void setMyuseCardStatus(int index)
    {
        myHandStatus = (MyHandStatus)index;
        interactableAllBTN();
    }

    [SerializeField] MyHandStatus myHandStatus = MyHandStatus.cannotUseCard;

    public void touchBoard(int place)
    {
        int i = place%9; int j = place/9;
        if(myHandStatus == MyHandStatus.cannotUseCard)
        {
            if(PhotonNetwork.IsMasterClient)
            {
                PV.RPC("putStonewithoutMagic", RpcTarget.AllBuffered, place, stoneColor.black);
                endMyTurn();
            }
            else
            {
                PV.RPC("putStonewithoutMagic", RpcTarget.AllBuffered, place, stoneColor.white);
                endMyTurn();
            }
        }
        else
        useMagicCard(i, j);
    }
    [PunRPC] void putStonewithoutMagic(int place, stoneColor color)
    {
        gomokuData[place] = (int)color;
        audioSource.Play();
        reNewalBoard();
    }
    [PunRPC] void putBlackStone(int place)
    {
        gomokuData[place] = (int)stoneColor.black;
        repaintBoard();
    }

    [PunRPC] void putWhilteStone(int place)
    {
        gomokuData[place] = (int)stoneColor.white;
        repaintBoard();
    }

    bool areaSelected = false;
    int selectedBTNindex = -1; //선택된영역
    bool putStoneTwice = true;
    public GameObject bluebox3_3;
    void useMagicCard(int i, int j)
    {
        switch(myHandStatus) 
        {
			case MyHandStatus.reassignment3_3:
            {
                if(i==0 || i ==8 || j ==0 || j == 8)
                {
                    NetWorkManager.instance.printScreenString("다시 선택하세요");
                    return;
                }
                else
                {
                    if(!areaSelected || (areaSelected&& selectedBTNindex!=(i+j*9)))
                    {
                        PV.RPC("AreaBox_set3_3", RpcTarget.AllBuffered); 
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f+0.55f*i, 2.21f-0.55f*j, 0));
                        areaSelected = true;
                        selectedBTNindex = i+j*9;
                        NetWorkManager.instance.printScreenString("선택됨");
                    }
                    else
                    {   
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(10, 10, 0));
                        unInteractableAllBTN();
                        areaSelected = false; selectedBTNindex = -1; 
                        myHandStatus = MyHandStatus.cannotUseCard;  //초기화
                        //시작
                        int temp;
                        for(int i2 = i-1; i2<=i+1; i2++) //재배치완료
                        {
                            for(int j2 = j-1; j2 <= j + 1; j2++)
                            {
                                int rand = i + Random.Range(-1,2) + (j+Random.Range(-1,2))*9;
                                int place = i2+j2*9;
                                temp = gomokuData[place];
                                gomokuData[place] = gomokuData[rand];
                                gomokuData[rand] = temp;
                            }
                        }
                        
                        for(int i2 = i-1; i2<=i+1; i2++) //재배치완료
                        {
                            for(int j2 = j-1; j2 <= j + 1; j2++)
                            {
                                int place = i2+j2*9;
                                int tempdata = gomokuData[place];
                                PV.RPC("ChangeData", RpcTarget.AllBuffered, place, tempdata);
                            }
                        }
                        PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                        endMyTurn();
                    }
                }
            }
			break;

			case MyHandStatus.deleteVertical:
            {
                if(!areaSelected || (areaSelected&&(selectedBTNindex%9)!=(i)))
                    {
                        PV.RPC("AreaBox_set1_9", RpcTarget.AllBuffered); 
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f+0.55f*i, 0, 0));
                        areaSelected = true;
                        selectedBTNindex = i+j*9;
                        NetWorkManager.instance.printScreenString("선택됨");
                    }
                else
                    {   
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(10, 10, 0));
                        unInteractableAllBTN();
                        areaSelected = false; selectedBTNindex = -1; 
                        myHandStatus = MyHandStatus.cannotUseCard;  //초기화
                        //시작
                        for(int i2 = 0; i2<9; i2++)
                        {
                            int place = i+i2*9;
                            PV.RPC("ChangeData", RpcTarget.AllBuffered, place, 0);
                        }
                        PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                        endMyTurn();
                    }
            }
			break;

			case MyHandStatus.putStoneTwice:
            {
                if(putStoneTwice == true)
                {
                    putStoneTwice = false;
                    int place = i + 9*j;
                    if(PhotonNetwork.IsMasterClient)
                    {
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, place, (int)stoneColor.black);
                    }
                    else
                    {
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, place, (int)stoneColor.white);
                    }
                    PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                }
                else
                {
                    putStoneTwice = true;
                    int place = i + 9*j;
                    if(PhotonNetwork.IsMasterClient)
                    {
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, place, (int)stoneColor.black);
                    }
                    else
                    {
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, place, (int)stoneColor.white);
                    }
                    PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                    myHandStatus = MyHandStatus.cannotUseCard;
                    endMyTurn();
                }
            }
			break;

            case MyHandStatus.changeEnemyStone:
            {
                if(PhotonNetwork.IsMasterClient)
                {
                    PV.RPC("ChangeData", RpcTarget.AllBuffered, i+9*j, (int)stoneColor.black);
                    PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                }
                else
                {
                    PV.RPC("ChangeData", RpcTarget.AllBuffered, i+9*j, (int)stoneColor.white);
                    PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                }
                myHandStatus = MyHandStatus.cannotUseCard;  //초기화
                endMyTurn();
            }
            break;

            case MyHandStatus.reverseStone3_3:
            {
               if(i==0 || i ==8 || j ==0 || j == 8)
                {
                    NetWorkManager.instance.printScreenString("다시 선택하세요");
                    return;
                }
                else
                {
                    if(!areaSelected || (areaSelected&& selectedBTNindex!=(i+j*9)))
                    {
                        PV.RPC("AreaBox_set3_3", RpcTarget.AllBuffered); 
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f+0.55f*i, 2.21f-0.55f*j, 0));
                        areaSelected = true;
                        selectedBTNindex = i+j*9;
                        NetWorkManager.instance.printScreenString("선택됨");
                    }
                    else
                    {   
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(10, 10, 0));
                        unInteractableAllBTN();
                        areaSelected = false; selectedBTNindex = -1; 
                        myHandStatus = MyHandStatus.cannotUseCard;  //초기화
                        //시작
                        for(int i2 = i-1; i2<=i+1; i2++) //재배치완료
                        {
                            for(int j2 = j-1; j2 <= j + 1; j2++)
                            {
                                int place = i2+j2*9;
                                if(gomokuData[place]==1) PV.RPC("ChangeData", RpcTarget.AllBuffered, place, 2);
                                else if(gomokuData[place]==2) PV.RPC("ChangeData", RpcTarget.AllBuffered, place, 1);
                            }
                        }
                        PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                        endMyTurn();
                    }
                } 
            }
            break;
        }	
    }

    [PunRPC] void ChangeData(int place, int data)
    {
        gomokuData[place] = data;
    }

    [PunRPC] void repaintBoard()
    {
        for(int i = 0;i <81;i++)
        {
            if(gomokuData[i]==0) 
            {   
                gomokuTable[i].GetComponent<Image>().sprite = whiteStone;
                Color color = gomokuTable[i].GetComponent<Image>().color;
                color.a = 0;
                gomokuTable[i].GetComponent<Image>().color = color;
            }

            if(gomokuData[i]==1) 
            {
                gomokuTable[i].GetComponent<Image>().sprite = blackStone;
                Color color = gomokuTable[i].GetComponent<Image>().color;
                color.a = 1;
                gomokuTable[i].GetComponent<Image>().color = color;
            }

            if(gomokuData[i]==2) 
            {
                gomokuTable[i].GetComponent<Image>().sprite = whiteStone;
                Color color = gomokuTable[i].GetComponent<Image>().color;
                color.a = 1;
                gomokuTable[i].GetComponent<Image>().color = color;
            }
        }
    }

    void dolmove(Image img) {
        Vector3 tmp=img.transform.position;
        Sequence seq=DOTween.Sequence();
        seq.Join(img.transform.DOMove(charging.center,0.75f));
        seq.Join(img.transform.DOScale(new Vector3(0,0,0),3f));
        seq.Join(img.DOFade(0, 2f).SetEase(Ease.InQuad));
        seq.Append(img.transform.DOMove(tmp,0));
        seq.Join(img.transform.DOScale(new Vector3(1,1,1),0));
    }

    
    [PunRPC]void reNewalBoard()
    {
        repaintBoard();
        //이제 오목 완성됐는지 검사
        while(checkGomoku((int)stoneColor.black)>=0) //검은돌 검사
        {
            switch(checkGomoku((int)stoneColor.black))
            {
                case 0: //  >방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+1]=0;
                gomokuData[deleteStartNum+2]=0;
                gomokuData[deleteStartNum+3]=0;
                gomokuData[deleteStartNum+4]=0;
                charging.center=gomokuTable[deleteStartNum+2].transform.position;
                Instantiate(Part,gomokuTable[deleteStartNum+2].transform.position,gomokuTable[deleteStartNum+2].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum].transform.position, gomokuTable[deleteStartNum].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+1].transform.position, gomokuTable[deleteStartNum+1].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+3].transform.position, gomokuTable[deleteStartNum+3].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+4].transform.position, gomokuTable[deleteStartNum+4].transform.rotation);
                var img=gomokuTable[deleteStartNum].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+1].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+2].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+3].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+4].GetComponent<Image>();
                dolmove(img);
                break;

                case 1: //  ↓방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+9]=0;
                gomokuData[deleteStartNum+18]=0;
                gomokuData[deleteStartNum+27]=0;
                gomokuData[deleteStartNum+36]=0;
                charging.center=gomokuTable[deleteStartNum+18].transform.position;
                Instantiate(Part,gomokuTable[deleteStartNum+18].transform.position,gomokuTable[deleteStartNum+18].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum].transform.position, gomokuTable[deleteStartNum].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+9].transform.position, gomokuTable[deleteStartNum+9].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+27].transform.position, gomokuTable[deleteStartNum+27].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+36].transform.position, gomokuTable[deleteStartNum+36].transform.rotation);
                img=gomokuTable[deleteStartNum].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+9].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+18].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+27].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+36].GetComponent<Image>();
                dolmove(img);
                break;

                case 2: //  ↘방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+10]=0;
                gomokuData[deleteStartNum+20]=0;
                gomokuData[deleteStartNum+30]=0;
                gomokuData[deleteStartNum+40]=0;
                charging.center=gomokuTable[deleteStartNum+20].transform.position;
                Instantiate(Part,gomokuTable[deleteStartNum+20].transform.position,gomokuTable[deleteStartNum+20].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum].transform.position, gomokuTable[deleteStartNum].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+10].transform.position, gomokuTable[deleteStartNum+10].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+30].transform.position, gomokuTable[deleteStartNum+30].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+40].transform.position, gomokuTable[deleteStartNum+40].transform.rotation);
                img=gomokuTable[deleteStartNum].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+10].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+20].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+30].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+40].GetComponent<Image>();
                dolmove(img);
                break;

                case 3: //  ↙방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+8]=0;
                gomokuData[deleteStartNum+16]=0;
                gomokuData[deleteStartNum+24]=0;
                gomokuData[deleteStartNum+32]=0;
                charging.center=gomokuTable[deleteStartNum+16].transform.position;
                Instantiate(Part,gomokuTable[deleteStartNum+16].transform.position,gomokuTable[deleteStartNum+16].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum].transform.position, gomokuTable[deleteStartNum].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+8].transform.position, gomokuTable[deleteStartNum+8].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+24].transform.position, gomokuTable[deleteStartNum+24].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+32].transform.position, gomokuTable[deleteStartNum+32].transform.rotation);
                img=gomokuTable[deleteStartNum].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+8].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+16].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+24].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+32].GetComponent<Image>();
                dolmove(img);
                break;   
            } 
            if(PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(enemyshoot());
                PlayerManager.enemyPlayerManager.GetDamaged();
            }
            else
            {
                StartCoroutine(myshoot());
                PlayerManager.myPlayerManager.GetDamaged();
            }
        }

        while(checkGomoku((int)stoneColor.white)>=0) //흰돌 검사
        {
            switch(checkGomoku((int)stoneColor.white))
            {
                case 0: //  >방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+1]=0;
                gomokuData[deleteStartNum+2]=0;
                gomokuData[deleteStartNum+3]=0;
                gomokuData[deleteStartNum+4]=0;
                charging.center=gomokuTable[deleteStartNum+2].transform.position;
                Instantiate(Part,gomokuTable[deleteStartNum+2].transform.position,gomokuTable[deleteStartNum+2].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum].transform.position, gomokuTable[deleteStartNum].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+1].transform.position, gomokuTable[deleteStartNum+1].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+3].transform.position, gomokuTable[deleteStartNum+3].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+4].transform.position, gomokuTable[deleteStartNum+4].transform.rotation);
                var img=gomokuTable[deleteStartNum].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+1].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+2].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+3].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+4].GetComponent<Image>();
                dolmove(img);
                break;

                case 1: //  ↓방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+9]=0;
                gomokuData[deleteStartNum+18]=0;
                gomokuData[deleteStartNum+27]=0;
                gomokuData[deleteStartNum+36]=0;
                charging.center=gomokuTable[deleteStartNum+18].transform.position;
                Instantiate(Part,gomokuTable[deleteStartNum+18].transform.position,gomokuTable[deleteStartNum+18].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum].transform.position, gomokuTable[deleteStartNum].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+9].transform.position, gomokuTable[deleteStartNum+9].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+27].transform.position, gomokuTable[deleteStartNum+27].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+36].transform.position, gomokuTable[deleteStartNum+36].transform.rotation);
                img=gomokuTable[deleteStartNum].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+9].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+18].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+27].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+36].GetComponent<Image>();
                dolmove(img);
                break;

                case 2: //  ↘방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+10]=0;
                gomokuData[deleteStartNum+20]=0;
                gomokuData[deleteStartNum+30]=0;
                gomokuData[deleteStartNum+40]=0;
                charging.center=gomokuTable[deleteStartNum+20].transform.position;
                Instantiate(Part,gomokuTable[deleteStartNum+20].transform.position,gomokuTable[deleteStartNum+20].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum].transform.position, gomokuTable[deleteStartNum].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+10].transform.position, gomokuTable[deleteStartNum+10].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+30].transform.position, gomokuTable[deleteStartNum+30].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+40].transform.position, gomokuTable[deleteStartNum+40].transform.rotation);
                img=gomokuTable[deleteStartNum].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+10].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+20].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+30].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+40].GetComponent<Image>();
                dolmove(img);
                break;

                case 3: //  ↙방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+8]=0;
                gomokuData[deleteStartNum+16]=0;
                gomokuData[deleteStartNum+24]=0;
                gomokuData[deleteStartNum+32]=0;
                charging.center=gomokuTable[deleteStartNum+16].transform.position;
                Instantiate(Part,gomokuTable[deleteStartNum+16].transform.position,gomokuTable[deleteStartNum+16].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum].transform.position, gomokuTable[deleteStartNum].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+8].transform.position, gomokuTable[deleteStartNum+8].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+24].transform.position, gomokuTable[deleteStartNum+24].transform.rotation);
                Instantiate(part2, gomokuTable[deleteStartNum+32].transform.position, gomokuTable[deleteStartNum+32].transform.rotation);
                img=gomokuTable[deleteStartNum].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+8].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+16].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+24].GetComponent<Image>();
                dolmove(img);
                img=gomokuTable[deleteStartNum+32].GetComponent<Image>();
                dolmove(img);
                break;   
            }
            if(PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(myshoot());
                PlayerManager.myPlayerManager.GetDamaged();
            }
            else
            {
                StartCoroutine(enemyshoot());
                PlayerManager.enemyPlayerManager.GetDamaged();
            } 
        }
        
    }

    IEnumerator enemyshoot() {
        yield return new WaitForSeconds(1f); 
        Instantiate(enemyshooting,charging.center+new Vector3(0,0,-70),Quaternion.identity);
    }

    IEnumerator myshoot(){
        yield return new WaitForSeconds(1f);
        Instantiate(myshooting,charging.center+new Vector3(0,0,-70),Quaternion.identity);
    }

    enum dir{
        leftDir,
        downDir,
        leftdownDir,
        rightdownDir
    }

    int checkGomoku(int color)
    { 
        for(int i = 0;i<5;i++)     // -> 0
        {
            for(int j = 0;j<9;j++)
            {
                if(gomokuData[i+j*9]==color && gomokuData[i+j*9+1]==color && gomokuData[i+j*9+2]==color &&
                    gomokuData[i+j*9+3]==color &&gomokuData[i+j*9+4]==color)
                {
                    deleteStartNum = i+j*9;
                    return (int)dir.leftDir;
                }
            }
        }

        for(int i = 0; i<9;i++) //  ↓ 1
        {
            for(int j = 0; j<5;j++)
            {
                if(gomokuData[i+j*9]==color && gomokuData[i+9+j*9]==color && gomokuData[i+18+j*9]==color &&
                    gomokuData[i+27+j*9]==color &&gomokuData[i+36+j*9]==color)
                {
                    deleteStartNum = i+j*9;
                    return (int)dir.downDir;
                }
            }
        }

        for(int i = 0; i < 5; i++) //  ↘ 2
        {
            for(int j = 0; j<5; j++)
            {
                if(gomokuData[i+j*9]==color && gomokuData[i+j*9+10]==color && gomokuData[i+j*9+20]==color &&
                    gomokuData[i+j*9+30]==color &&gomokuData[i+j*9+40]==color)
                {   
                    deleteStartNum = i+j*9;
                    return (int)dir.leftdownDir;
                }
            }
        }

        for(int i = 4; i < 9; i++) // ↙ 3
        {
            for(int j = 0; j <5;j++)
            {
                if(gomokuData[i+(j*9)]==color && gomokuData[i+(j*9) +8]==color && gomokuData[i+(j*9)+16]==color &&
                    gomokuData[i+ (j*9) +24]==color &&gomokuData[i+(j*9) +32]==color)
                {   
                    deleteStartNum = i+j*9;
                    return (int)dir.rightdownDir;
                }
            }
        }
        return -1;
    }

    #endregion

    #region 게임종료

    [SerializeField] GameObject ResultPannel;
    [SerializeField] TMP_Text ResultTMP;

    public ParticleSystem Part { get => part; set => part = value; }

    public void LoseGame()
    {
        GameOver();
        PV.RPC("GameOver", RpcTarget.OthersBuffered, "승리");
    }

    [PunRPC]public void GameOver(string result = "패배")
    {
        ResultTMP.text = result;
        ResultPannel.SetActive(true);
        StartCoroutine(BackToLobby());
    }

    IEnumerator BackToLobby()
    {
        yield return new WaitForSeconds(2f);
        resetGameData();
        NetWorkManager.instance.EndGame();
        ResultPannel.SetActive(false);
        Destroy(PlayerManager.myPlayerManager);
        Destroy(PlayerManager.enemyPlayerManager);
        GameObject[] cards = GameObject.FindGameObjectsWithTag("Card");
        foreach(GameObject card in cards)
        {
            Destroy(card);
        }
    }

    #endregion
    
    #region 잡다한 코드들
    void interactableAllBTN()
    {
        for(int i = 0;i<81;i++)
        {
            gomokuTable[i].interactable = true;
        }
    }
    void unInteractableAllBTN()
    {
        for(int i = 0;i<81;i++)
        {
            gomokuTable[i].interactable = false;
        }
    }
    
    void resetGameData()
    {
        for(int i = 0;i<81;i++)
        {
            gomokuData[i]=0; gomokuTable[i].interactable=false;
        }
        //각 매니저 필요한거 초기화
        PlayerManager.myPlayerManager = PlayerManager.myPlayerManager;
        PlayerManager.enemyPlayerManager = PlayerManager.enemyPlayerManager;
    }

    [PunRPC] void moveAreaBox(Vector3 newposition)
    {
        bluebox3_3.transform.position = newposition;
    }

    [PunRPC]void AreaBox_set3_3(){
        AreaBox.instance.setSize3_3();
    }

    [PunRPC]void AreaBox_set9_1(){
        AreaBox.instance.setSize9_1();
    }

    [PunRPC] void AreaBox_set1_9(){
        AreaBox.instance.setSize1_9();
    }

    public void setChangeEnemyStone(){
        if(PhotonNetwork.IsMasterClient)
                {
                    for(int i2 = 0;i2 <81;i2++)
                    {
                        if(gomokuData[i2]==(int)stoneColor.white) gomokuTable[i2].interactable=true;
                        else gomokuTable[i2].interactable = false;
                    }
                }
                else
                {
                    for(int i2 = 0;i2 <81;i2++)
                    {
                        if(gomokuData[i2]==(int)stoneColor.black) gomokuTable[i2].interactable=true;
                        else gomokuTable[i2].interactable = false;
                    }
                }
    }

    #endregion
}
