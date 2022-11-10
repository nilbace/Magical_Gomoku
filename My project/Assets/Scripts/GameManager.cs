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
    public PhotonView PV;
    public Button[] gomokuTable;
    public Sprite whiteStone;  //2
    public Sprite blackStone; //1
    public int[] gomokuData = new int[81];
    public bool usingCard = false;
    public NetWorkManager netWorkManager;
    bool coinstossed = false;

    enum stoneColor{
        black = 1,
        white = 2
    }
    private void Start() {
        resetGameData(); //빈칸으로 초기화, interactable false
        reNewalBoard();
    }
    private void Update() {
        if(netWorkManager.GamePannel.activeSelf && !coinstossed &&PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount ==2)
        {
            coinstossed = true;
            if(PhotonNetwork.IsMasterClient)  coinToss();
        }
    }

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
        for(int i = 0;i <81;i++)
        {
            if(gomokuData[i]==0) gomokuTable[i].interactable=true;
            
            //카드 던지기 가능
        }
        netWorkManager.printScreenString("나의 턴");
    }
    
    public void touchBoard(int place)
    {
        int i = place/9; int j = place%9;
        if(!usingCard)
        {
            if(PhotonNetwork.IsMasterClient)
            {
                WaitUntilTurnEnd();
                PV.RPC("putBlackStone", RpcTarget.AllBuffered, place);
                
            }
            else
            {
                WaitUntilTurnEnd();
                PV.RPC("putWhilteStone", RpcTarget.AllBuffered, place);
            }
        }
    }

    [PunRPC] void putBlackStone(int place)
    {
        gomokuData[place] = 1;
        reNewalBoard();
    }

    [PunRPC] void putWhilteStone(int place)
    {
        gomokuData[place] = 2;
        reNewalBoard();
    }

    int deleteStartNum;
    void reNewalBoard()
    {
        for(int i = 0;i <81;i++) //아까 둔 돌 표시
        {
            if(gomokuData[i]==0) 
            {
                gomokuTable[i].GetComponent<Image>().sprite = null;
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
        if(checkGomoku((int)stoneColor.white)<0 && checkGomoku((int)stoneColor.black)<0) endMyTurn();
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
            for(int j = 0; i <5;j++)
            {
                if(gomokuData[i+j*9]==color && gomokuData[i+j*9+8]==color && gomokuData[i+j*9+16]==color &&
                    gomokuData[i+j*9+24]==color &&gomokuData[i+j*9+32]==color)
                {   
                    deleteStartNum = i+j*9;
                    return (int)dir.rightdownDir;
                }
            }
        }
        return -1;
    }



    void WaitUntilTurnEnd()
    {
        for(int i = 0;i <81;i++)
        {
            gomokuTable[i].interactable=false;
            //카드 던지기 가능 비활성화
        }
        endMyTurn(); //임시
    }
    void endMyTurn()
    {
        PV.RPC("startMyTurn", RpcTarget.OthersBuffered);
    }
    void resetGameData()
    {
        for(int i = 0;i<81;i++)
        {
            gomokuData[i]=0; gomokuTable[i].interactable=false;
        }
    }
}
