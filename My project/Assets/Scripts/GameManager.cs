using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    bool coinstossed = false;
    
    int deleteStartNum;
    


    
    enum stoneColor{ black = 1, white = 2 }

    public bool canuseCard; //카드를 드래그했을때 써지는지 여부 금방금방 꺼짐
    
    private void Start() {
        unInteractableAllBTN();
        repaintBoard();
    }

    private void Update() {
        if(NetWorkManager.instance.GamePannel.activeSelf && !coinstossed &&PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount ==2)
        {
            coinstossed = true;
            if(PhotonNetwork.IsMasterClient)  coinToss();
        }
    }

    #region 턴관련
    public bool isMyTurn = false;

    void coinToss()
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
        PV.RPC("startMyTurn", RpcTarget.OthersBuffered);
    }
    #endregion

    #region 오목관련+카드
    enum MyHandStatus{
        cannotUseCard = -1,
        reassignment3_3, deleteVertical, putStoneTwice,} // 지금이 어떤 상태인지 카드를 쓰고 있는 중인지 아닌지

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
                    if(!areaSelected || (areaSelected&& selectedBTNindex!=(i+j*9) ))
                    {
                        bluebox3_3.transform.position = new Vector3(-2.21f+0.55f*i, 2.21f-0.55f*j, 0);
                        areaSelected = true;
                        selectedBTNindex = i+j*9;
                        NetWorkManager.instance.printScreenString("선택됨");
                    }
                    else
                    {   
                        bluebox3_3.transform.position += new Vector3(10,0,0);
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

            }
			break;

			case MyHandStatus.putStoneTwice:
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
    [PunRPC]void reNewalBoard()
    {
        repaintBoard();
        
        //이제 오목 완성됐는지 검사

        if(checkGomoku((int)stoneColor.black)>=0) //검은돌 검사
        {
            switch(checkGomoku((int)stoneColor.black))
            {
                case 0: //  >방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+1]=0;
                gomokuData[deleteStartNum+2]=0;
                gomokuData[deleteStartNum+3]=0;
                gomokuData[deleteStartNum+4]=0;
                reNewalBoard();
                break;

                case 1: //  ↓방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+9]=0;
                gomokuData[deleteStartNum+18]=0;
                gomokuData[deleteStartNum+27]=0;
                gomokuData[deleteStartNum+36]=0;
                reNewalBoard();
                break;

                case 2: //  ↘방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+10]=0;
                gomokuData[deleteStartNum+20]=0;
                gomokuData[deleteStartNum+30]=0;
                gomokuData[deleteStartNum+40]=0;
                reNewalBoard();
                break;

                case 3: //  ↙방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+8]=0;
                gomokuData[deleteStartNum+16]=0;
                gomokuData[deleteStartNum+24]=0;
                gomokuData[deleteStartNum+32]=0;
                reNewalBoard();
                break;   
            } 
        }

        if(checkGomoku((int)stoneColor.white)>=0) //흰돌 검사
        {
            switch(checkGomoku((int)stoneColor.white))
            {
                case 0: //  >방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+1]=0;
                gomokuData[deleteStartNum+2]=0;
                gomokuData[deleteStartNum+3]=0;
                gomokuData[deleteStartNum+4]=0;
                reNewalBoard();
                break;

                case 1: //  ↓방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+9]=0;
                gomokuData[deleteStartNum+18]=0;
                gomokuData[deleteStartNum+27]=0;
                gomokuData[deleteStartNum+36]=0;
                reNewalBoard();
                break;

                case 2: //  ↘방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+10]=0;
                gomokuData[deleteStartNum+20]=0;
                gomokuData[deleteStartNum+30]=0;
                gomokuData[deleteStartNum+40]=0;
                reNewalBoard();
                break;

                case 3: //  ↙방향 제거
                gomokuData[deleteStartNum]  =0;
                gomokuData[deleteStartNum+8]=0;
                gomokuData[deleteStartNum+16]=0;
                gomokuData[deleteStartNum+24]=0;
                gomokuData[deleteStartNum+32]=0;
                reNewalBoard();
                break;   
            } 
        }
        if(isMyTurn && checkGomoku((int)stoneColor.white)<0 && checkGomoku((int)stoneColor.black)<0) endMyTurn();
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
        PlayerManager.myPlayerManager = null;
        PlayerManager.enemyPlayerManager = null;
    }

    [PunRPC] void ReneWalData()
    {
        
    }

    #endregion
}
