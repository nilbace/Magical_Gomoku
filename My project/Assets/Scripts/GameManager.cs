using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
//using UnityEditor.TextCore.Text;

public class GameManager : MonoBehaviourPunCallbacks
{
    // 싱글턴
    public static GameManager instance;
    private void Awake() {
        instance = this;
    }


    public PhotonView PV;
    public Button[] gomokuTable;  // 버튼 81개로 오목판(테이블)을 구성함
    public Sprite whiteStone;  // 흰 돌 Sprite (이미지?)
    public Sprite blackStone;  // 검은 돌 Sprite
    public int[] gomokuData = new int[81];  // 81개 버튼들에 대한 데이터 (0:돌x, 1:검은돌, 2:흰돌)
    public GameObject timer;
    int deleteStartNum;  // 오목이 완성돼서 5개의 돌을 제거할 때 시작되는 돌(버튼)의 번호. 항상 가장 왼쪽 위에 있는 (번호가 가장 작은) 버튼
    public ParticleSystem part;
    public ParticleSystem part2;
    public ParticleSystem myshooting;
    public ParticleSystem enemyshooting;
    bool timeron=false;
    public float time=0;
    GameObject timerins=null;
    AudioSource audioSource;
    AudioSource turnsfx;

    
    enum stoneColor{ black = 1, white = 2 }

    public bool canuseCard;  // 카드를 드래그했을때 써지는지 여부 금방금방 꺼짐
    
    public void Start() {
        resetGameData();  // 게임 데이터 초기화 (돌, 나와 상대방 PlayerManager)
        unInteractableAllBTN();
        repaintBoard();  // 오목판을 칠함
        turnsfx = this.gameObject.GetComponent<AudioSource>();
        audioSource = gomokuTable[0].gameObject.GetComponent<AudioSource>();
    }

    void Update() {
        if(timeron) {
            time+=Time.deltaTime;
            if(time>=60) {
                if(PV.IsMine) endMyTurn();
            }
        }
    }

    

    #region 턴관련
    public bool isMyTurn = false;

    // 나와 상대방 중 누가 선공을 할지를 정함
    // 참조 : PlayerManager.Awake()
    public void coinToss()
    {
        StartCoroutine(coinTossProcess());
    }

    // 기능 : 1초를 기다리고 코인 토스를 한 뒤, 내가 먼저 시작할지, 상대방이 먼저 시작할지를 랜덤하게 정함
    // 참조 : 게임을 처음 시작할 때
    IEnumerator coinTossProcess()
    {
        yield return new WaitForSeconds(1f);  // 1초 기다림

        int tmp = Random.Range(0, 2);  // 값 : 0 또는 1 둘 중 하나
        if (tmp == 0)
            startMyTurn();  // 내가 먼저 시작
        else
            PV.RPC("startMyTurn", RpcTarget.OthersBuffered);  // 상대방이 먼저 시작
    }

    // 나의 차례가 되었을 때
    // 기능 : 아직 돌을 두지 않은 모든 부분에 돌을 둘 수 있게 하고 '나의 턴'을 출력함
    // 참조 : 게임을 처음 시작할 때 코인 토스를 한 이후
    [PunRPC] void startMyTurn()
    {
        isMyTurn = true;
        canuseCard = true;  // 카드를 사용할 수 있게 함
        timeron=true;
        for (int i = 0; i < 81; i++)
        {
            if (gomokuData[i] == 0)   // 아직 돌을 두지 않은 부분만 클릭할 수 있게 함
                gomokuTable[i].interactable = true;
        }
        PV.RPC("timermake", RpcTarget.AllBuffered);
        NetWorkManager.instance.printScreenString("나의 턴");  // '나의 턴' 출력
    }

    [PunRPC] void timermake() {
        if(timerins!=null) Destroy(timerins);
        timerins=Instantiate(timer, new Vector3(-50,580,10), Quaternion.identity);
        timerins.transform.SetParent(this.transform.parent.transform,false);
        time=0;
    }

    // 나의 차례가 끝났을 때
    // 기능 : 모든 버튼을 클릭할 수 없게 만들고 상대방에게 턴을 넘겨줌
    // 참조 : GameManager.touchBoard(),
    void endMyTurn()
    {
        isMyTurn = false;
        canuseCard = false;  // 나의 턴을 끝내고 카드를 사용할 수 없게 함
        for (int i = 0; i < 81; i++)   // 모든 버튼을 클릭할 수 없게 함
        {
            gomokuTable[i].interactable=false;
        }
        turnsfx.Play();
        PV.RPC("startMyTurn", RpcTarget.OthersBuffered);  // 상대방에게 턴을 넘겨줌
    }
    #endregion

    #region 오목관련+카드
    enum MyHandStatus{
        cannotUseCard = -1,  // 카드를 쓰면 안되는 상태 (상대턴이거나 혹은 내 턴인데 이미 카드를 쓴 경우)
        reassignment3_3, deleteVertical, putStoneTwice, changeEnemyStone, reverseStone2_2, allRandomRelocate, deleteCross, stoneExchange, exchangeArea2_2  // 카드 종류
    }


    /* 
     * reassignment3_3 : 3*3 지정 구역 내의 돌을 랜덤 위치 재배치하는 카드 (3) : index 0
     * deleteVertical : 한 줄 삭제하는 카드 (4) : index 1
     * putStoneTwice : 한 번에 둘 2번 놓는 카드 (8) : index 2
     * changeEnemyStone : 상대의 돌 1개를 내 돌로 바꾸는 카드 (1) : index 3
     * reverseStone2_2 : 원하는 2*2 영역에서 흰돌은 검은돌로, 검은돌은 흰돌로 바꾸는 카드 (2) : index 4
     * allRondomRelocate : 오목판 위의 돌을 모두 랜덤 재배치하는 카드 (5) : index 5
     * deleteCross : 십자가 줄 상의 모든 돌을 삭제하는 카드 (9) : index 6
     * stoneExchange : 상대의 돌 1개와 내 돌 1개 위치 변환하는 카드 (6) : index 7
     * exchangeArea2_2 : 원하는 2*2 영역의 돌들을 다른 2*2 영역의 돌들과 교체하는 카드 (7) : index 8
     */



    // 기능 : 현재 사용하려고 하는 카드의 종류를 지정해줌
    // 매개변수 : index (사용하려고 하는 카드의 인덱스 번호)
    // 참조 : 어떤 카드를 발동할 때(Card.OnMouseUp())
    public void setMyuseCardStatus(int index)
    {
        myHandStatus = (MyHandStatus)index;
        interactableAllBTN();  // 모든 버튼들을 활성화함
    }


    /* myHandStatus : 현재 사용하려고 하는 카드의 정보
   기본적으로 카드를 사용하지 않는 것으로 초기화 함
   myHandStatus는 카드를 발동했을 때만 cannotUseCard 상태가 아니게됨 */
    [SerializeField] MyHandStatus myHandStatus = MyHandStatus.cannotUseCard;


    // 기능 : 버튼이 클릭됐을 때 버튼의 번호를 받아서 그 위치에 돌을 두거나 카드를 사용함
    // 매개변수 : place (버튼의 번호)
    // 참조 : 버튼 OnClick() 이벤트 함수
    public void touchBoard(int place)
    {
        // 버튼의 x 좌표와 y 좌표를 구함
        int i = place % 9;
        int j = place / 9;

        // 버튼을 누르는 경우가 2가지 존재 - 돌을 둘 때, 카드를 사용할 때
        if (myHandStatus == MyHandStatus.cannotUseCard)  // 카드가 발동되지 않은 경우 - 돌을 둠
        {
            if(PhotonNetwork.IsMasterClient)  // 내가 방을 생성한 사람이면 - 검은 돌을 둠
            {
                PV.RPC("putStonewithoutMagic", RpcTarget.AllBuffered, place, stoneColor.black);
                endMyTurn();
            }
            else  // 내가 방을 생성한 사람이 아니면 - 흰 돌을 둠
            {
                PV.RPC("putStonewithoutMagic", RpcTarget.AllBuffered, place, stoneColor.white);
                endMyTurn();
            }
        }
        else  // 카드가 발동된 경우
            useMagicCard(i, j);
    }

    // 기능 : 돌을 둠
    // 매개변수 : place (돌을 둘 위치. 버튼의 번호), color (돌의 색깔)
    // 참조 : 버튼을 클릭했을 때 (GameManager.touchBoard())
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


    bool areaSelected = false;  // 영역을 선택했는지 여부
    int selectedBTNindex = -1;  // 영역을 선택했을 때, 기준이 되는 버튼의 번호
    bool subAreaSelected = false;  // 보조 영역을 선택했는지 여부
    int subSelectedBTNindex = -1;  // 보조 영역을 선택했을 때, 기준이 되는 버튼의 번호
    bool isConfirmed = false;  // 메인 영역이 확정됐는지 여부
    bool putStoneTwice = true;  // 돌을 2번 둘지 여부 (기본값 : true)
    public GameObject bluebox3_3;  // 영역 선택 박스
    public GameObject areaboxPlus;  // 영역 선택 박스 2 (보조)

    // 구현 완료된 카드 : 1, 2, 3, 4, 5, 6, 7, 8, 9
    // 미구현 :  10

    // 기능 : 카드 구현 (카드 사용)
    // 매개변수 : i (돌의 x 좌표), j (돌의 y 좌표)
    // 참조 : GameManager.touchBoard()
    void useMagicCard(int i, int j)
    {
        switch(myHandStatus) 
        {
            // 3*3 지정 구역 내의 돌을 랜덤 위치 재배치하는 카드 (3) : index 0
            case MyHandStatus.reassignment3_3:
                if(i==0 || i ==8 || j ==0 || j == 8)
                {
                    NetWorkManager.instance.printScreenString("다시 선택하세요");
                    return;
                }
                else
                {
                    if(!areaSelected || (areaSelected&& selectedBTNindex!=(i+j*9))) // 아직 영역을 선택하지 않았거나. 이전과 다른 버튼을 클릭한 경우
                    {
                        PV.RPC("AreaBox_set3_3", RpcTarget.AllBuffered);   // 영역 선택 박스를 바둑판 3*3 크기의 정사각형으로 바꿈
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f+0.55f*i, 2.21f-0.55f*j, 0));  // 영역 박스를 지금 선택한 버튼의 위치로 변경함
                        areaSelected = true;
                        selectedBTNindex = i+j*9;
                        NetWorkManager.instance.printScreenString("선택됨");
                    }
                    else  // 영역을 선택한 상태에서 같은 버튼을 다시 누른 경우 → 카드 발동
                    {   
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(10, 10, 0));  // 영역 박스를 안보이는 위치로 치움
                            unInteractableAllBTN();
                        areaSelected = false; selectedBTNindex = -1; 
                        myHandStatus = MyHandStatus.cannotUseCard;  // 초기화
                        
                        // 랜덤 재배치 시작
                        int temp;
                        for (int i2 = i - 1; i2 <= i + 1; i2++)   // 3*3 영역의 모든 돌들을 랜덤하게 재배치
                        {
                            for(int j2 = j-1; j2 <= j + 1; j2++)
                            {
                                int rand = i + Random.Range(-1, 2) + (j + Random.Range(-1, 2)) * 9;
                                int place = i2 + j2 * 9;

                                // Swap
                                temp = gomokuData[place];
                                gomokuData[place] = gomokuData[rand];
                                gomokuData[rand] = temp;
                            }
                        }

                        for (int i2 = i - 1; i2 <= i + 1; i2++)   // 랜덤하게 재배치한 데이터를 두 플레이어가 동기화함
                        {
                            for(int j2 = j-1; j2 <= j + 1; j2++)
                            {
                                int place = i2 + j2 * 9;
                                int tempdata = gomokuData[place];
                                PV.RPC("ChangeData", RpcTarget.AllBuffered, place, tempdata);
                            }
                        }
                        PV.RPC("reNewalBoard", RpcTarget.AllBuffered);  // 오목판을 새로 그림
                        endMyTurn();  // 턴을 끝냄
                    }
                }
            
			    break;

            // 한 줄 삭제하는 카드 - 세로줄 삭제 (4) : index 1
            case MyHandStatus.deleteVertical:
                if(!areaSelected || (areaSelected&&(selectedBTNindex%9)!=(i)))  // 아직 영역을 선택하지 않았거나, 이전과 다른 영역을 선택한 경우
                    {
                        PV.RPC("AreaBox_set1_9", RpcTarget.AllBuffered);   // 박스의 모양을 바둑판 1*9 크기의 직사각형으로 변경 (세로)
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f+0.55f*i, 0, 0));  // 박스 위치 변경
                        areaSelected = true;
                        selectedBTNindex = i+j*9;
                        NetWorkManager.instance.printScreenString("선택됨");
                    }
                else  // 영역을 선택한 상태에서 같은 영역을 다시 누른 경우
                {   
                    PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(10, 10, 0));  // 박스를 보이지 않는 곳으로 치움
                    unInteractableAllBTN();
                    areaSelected = false; selectedBTNindex = -1; 
                    myHandStatus = MyHandStatus.cannotUseCard;  // 초기화
                    // 한 줄 삭제 시작
                    for (int i2 = 0; i2<9; i2++)
                    {
                        int place = i+i2*9;
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, place, 0);
                    }
                    PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                    endMyTurn();  // 턴을 끝냄
                }
            
			    break;

            // 한 번에 돌 2번 놓는 카드 (8) : index 2
            case MyHandStatus.putStoneTwice:
            {
                if(putStoneTwice == true)  // 1번째 돌
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
                    else  // 2번째 돌
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
                    endMyTurn();  // 턴을 끝냄
                    }
            }
			break;

            // 상대의 돌 1개를 내 돌로 교체하는 카드 (1) : index 3
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
                myHandStatus = MyHandStatus.cannotUseCard;  // 초기화
                endMyTurn();  // 턴을 끝냄
                }
            break;

            // 2*2로 바꿔야함!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!---------------------------------------------------------
            // 원하는 2*2 영역에서 흰돌은 검은돌로, 검은돌은 흰돌로 바꾸는 카드 (2) : index 4
            case MyHandStatus.reverseStone2_2:
               if(i ==8 || j == 8)
                {
                    NetWorkManager.instance.printScreenString("다시 선택하세요");
                }
                else
                {
                    if (!areaSelected || (areaSelected && selectedBTNindex != (i + j * 9)))   // 영역을 아직 선택하지 않았거나, 이전과 다른 버튼을 클릭한 경우
                    {
                        PV.RPC("AreaBox_set2_2", RpcTarget.AllBuffered);
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i + 0.3f, 2.21f - 0.55f * j - 0.3f, 0));
                        areaSelected = true;
                        selectedBTNindex = i+j*9;
                        NetWorkManager.instance.printScreenString("선택됨");
                    }
                    else  // 영역을 선택한 상태에서 같은 버튼을 다시 누른 경우 - 카드 발동
                    {   
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(10, 10, 0));
                        unInteractableAllBTN();

                        // 초기화
                        areaSelected = false; 
                        selectedBTNindex = -1; 
                        myHandStatus = MyHandStatus.cannotUseCard;

                        // 재배치 시작
                        for(int i2 = i; i2<=i+1; i2++)
                        {
                            for(int j2 = j; j2 <= j + 1; j2++)
                            {
                                int place = i2 + j2 * 9;

                                if(gomokuData[place]==1) 
                                    PV.RPC("ChangeData", RpcTarget.AllBuffered, place, 2);  // 검은돌을 흰돌로 바꿈
                                else if(gomokuData[place]==2) 
                                    PV.RPC("ChangeData", RpcTarget.AllBuffered, place, 1);  // 흰돌을 검은돌로 바꿈
                            }
                        }
                        PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                        endMyTurn();  // 턴을 끝냄
                    }
                } 

                break;

            // 오목판 위의 돌을 모두 랜덤 재배치하는 카드 (5) : index 5
            case MyHandStatus.allRandomRelocate:
                myHandStatus = MyHandStatus.cannotUseCard;

                // 랜덤 재배치 시작
                for (int i2 = 0; i2 < 9; i2++)  // 모든 돌들을 랜덤하게 재배치
                {
                    for (int j2 = 0; j2 < 9; j2++)
                    {
                        int rand = Random.Range(0, 81);
                        int place = i2 + j2 * 9;

                        // Swap
                        int temp = gomokuData[place];
                        gomokuData[place] = gomokuData[rand];
                        gomokuData[rand] = temp;

                    }
                }

                for (int i2 = 0; i2 < 9; i2++)  // 랜덤하게 재배치한 데이터 동기화
                {
                    for (int j2 = 0; j2 < 9; j2++)
                    {
                        int place = i2 + j2 * 9;
                        int tempdata = gomokuData[place];
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, place, tempdata);
                    }
                }

                PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                endMyTurn();

                break;

            // 십자가 줄 상의 모든 돌을 삭제하는 카드 (9) : index 6
            case MyHandStatus.deleteCross:
                if (!areaSelected || (areaSelected && selectedBTNindex != (i + j * 9)))  // 아직 영역을 선택하지 않았거나. 이전과 다른 버튼을 클릭한 경우
                {
                    // 박스 조정
                    PV.RPC("AreaBox_set1_9", RpcTarget.AllBuffered);   // 박스의 모양을 바둑판 1*9 크기의 직사각형으로 변경 (세로)
                    PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i, 0, 0));  // 박스 위치 변경

                    // 보조 박스 조정
                    PV.RPC("AreaBoxPlus_set9_1", RpcTarget.AllBuffered);  // 보조 박스의 모양을 바둑판 9*1 크기의 직사각형으로 변경 (가로)
                    PV.RPC("moveAreaBoxPlus", RpcTarget.AllBuffered, new Vector3(0, 2.21f - 0.55f * j, 0));  // 보조 박스 위치 변경

                    areaSelected = true;
                    selectedBTNindex = i + j * 9;
                    NetWorkManager.instance.printScreenString("선택됨");
                }
                else  // 영역을 선택한 상태에서 같은 영역을 다시 누른 경우
                {
                    PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(10, 10, 0));  // 박스를 보이지 않는 곳으로 치움
                    PV.RPC("moveAreaBoxPlus", RpcTarget.AllBuffered, new Vector3(12, 10, 0));  // 보조 박스를 보이지 않는 곳으로 치움

                    unInteractableAllBTN();
                    areaSelected = false; selectedBTNindex = -1;
                    myHandStatus = MyHandStatus.cannotUseCard;  // 초기화

                    // 세로 한 줄 삭제 시작
                    for (int i2 = 0; i2 < 9; i2++)
                    {
                        int place = i + i2 * 9;
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, place, 0);
                    }
                    // 가로 한 줄 삭제 시작
                    for (int i2 = 0; i2 < 9; i2++)
                    {
                        int place = i2 + j * 9;
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, place, 0);
                    }

                    PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                    endMyTurn();  // 턴을 끝냄
                }

                break;


            // 상대의 돌 1개와 내 돌 1개 위치 변환하는 카드 (6) : index 7
            case MyHandStatus.stoneExchange:
                if (!areaSelected && !subAreaSelected && !isConfirmed)  // 2개의 영역 모두 선택되지 않았고 영역 확정도 안된 경우 (카드 발동 직후)
                {
                    if (PhotonNetwork.IsMasterClient)  // 가장 먼저, 자기 돌만 선택할 수 있도록 함
                    {
                        int BTNindex = i + j * 9;

                        if (gomokuData[BTNindex] == (int)stoneColor.black)
                        {
                            areaSelected = true;
                            selectedBTNindex = BTNindex;

                            PV.RPC("AreaBox_set1_1", RpcTarget.AllBuffered);
                            PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i, 2.21f - 0.55f * j, 0));

                            NetWorkManager.instance.printScreenString("선택됨");
                        }
                        else
                            NetWorkManager.instance.printScreenString("다시 선택하세요 (검은 돌만 선택 가능함)");
                    }
                    else
                    {
                        int BTNindex = i + j * 9;

                        if (gomokuData[BTNindex] == (int)stoneColor.white)
                        {
                            areaSelected = true;
                            selectedBTNindex = BTNindex;

                            PV.RPC("AreaBox_set1_1", RpcTarget.AllBuffered);
                            PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i, 2.21f - 0.55f * j, 0));

                            NetWorkManager.instance.printScreenString("선택됨");
                        }
                        else
                            NetWorkManager.instance.printScreenString("다시 선택하세요 (흰 돌만 선택 가능함)");
                    }
                }

                else if (areaSelected && !subAreaSelected && !isConfirmed && selectedBTNindex != (i + j * 9))  // 영역이 선택됐는데, 다른 부분을 클릭한 경우
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        int BTNindex = i + j * 9;

                        if (gomokuData[BTNindex] == (int)stoneColor.black)
                        {
                            selectedBTNindex = BTNindex;

                            PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i, 2.21f - 0.55f * j, 0));

                            NetWorkManager.instance.printScreenString("선택됨");
                        }
                    }
                    else
                    {
                        int BTNindex = i + j * 9;

                        if (gomokuData[BTNindex] == (int)stoneColor.white)
                        {
                            selectedBTNindex = BTNindex;

                            PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i, 2.21f - 0.55f * j, 0));

                            NetWorkManager.instance.printScreenString("선택됨");
                        }
                    }
                }

                else if (areaSelected && !subAreaSelected && !isConfirmed && selectedBTNindex == (i + j * 9))  // 같은 영역을 2번 클릭한 경우
                {
                    isConfirmed = true;
                    NetWorkManager.instance.printScreenString("확정됨");
                }

                else if (areaSelected && !subAreaSelected && isConfirmed)  // 내 돌 선택까지 완료된 경우. 2번째 영역 선택
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        int BTNindex = i + j * 9;

                        if (gomokuData[BTNindex] == (int)stoneColor.white)  // 상대방의 돌만 선택할 수 있게
                        {
                            subAreaSelected = true;
                            subSelectedBTNindex = BTNindex;

                            PV.RPC("AreaBoxPlus_set1_1", RpcTarget.AllBuffered);
                            PV.RPC("moveAreaBoxPlus", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i, 2.21f - 0.55f * j, 0));

                            NetWorkManager.instance.printScreenString("선택됨");
                        }
                    }
                    else
                    {
                        int BTNindex = i + j * 9;

                        if (gomokuData[BTNindex] == (int)stoneColor.black)
                        {
                            subAreaSelected = true;
                            subSelectedBTNindex = BTNindex;

                            PV.RPC("AreaBoxPlus_set1_1", RpcTarget.AllBuffered);
                            PV.RPC("moveAreaBoxPlus", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i, 2.21f - 0.55f * j, 0));

                            NetWorkManager.instance.printScreenString("선택됨");
                        }
                    }
                }

                else if (areaSelected && subAreaSelected && isConfirmed && subSelectedBTNindex != (i + j * 9))  // 2번째 영역을 선택한 뒤, 다른 영역을 선택한 경우
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        int BTNindex = i + j * 9;

                        if (gomokuData[BTNindex] == (int)stoneColor.white)
                        {
                            subSelectedBTNindex = BTNindex;

                            PV.RPC("moveAreaBoxPlus", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i, 2.21f - 0.55f * j, 0));

                            NetWorkManager.instance.printScreenString("선택됨");
                        }
                    }
                    else
                    {
                        int BTNindex = i + j * 9;

                        if (gomokuData[BTNindex] == (int)stoneColor.black)
                        {
                            subSelectedBTNindex = BTNindex;

                            PV.RPC("moveAreaBoxPlus", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i, 2.21f - 0.55f * j, 0));

                            NetWorkManager.instance.printScreenString("선택됨");
                        }
                    }
                }

                else if (areaSelected && subAreaSelected && isConfirmed && subSelectedBTNindex == (i + j * 9))  // 2번째 영역까지 모두 선택 완료
                {
                    // 두 박스를 모두 보이지 않는 곳으로 치움
                    PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(10, 10, 0));
                    PV.RPC("moveAreaBoxPlus", RpcTarget.AllBuffered, new Vector3(12, 10, 0));
                    unInteractableAllBTN();

                    // 2개 돌 데이터 변경 (Swap)
                    int temp = gomokuData[selectedBTNindex];
                    gomokuData[selectedBTNindex] = gomokuData[subSelectedBTNindex];
                    gomokuData[subSelectedBTNindex] = temp;

                    // 데이터 변경 동기화
                    PV.RPC("ChangeData", RpcTarget.AllBuffered, selectedBTNindex, gomokuData[selectedBTNindex]);
                    PV.RPC("ChangeData", RpcTarget.AllBuffered, subSelectedBTNindex, gomokuData[subSelectedBTNindex]);

                    // 변수값 초기화
                    areaSelected = false;
                    subAreaSelected = false;
                    selectedBTNindex = -1;
                    subSelectedBTNindex = -1;
                    isConfirmed = false;
                    myHandStatus = MyHandStatus.cannotUseCard;

                    // 오목판 다시 그림
                    PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                    endMyTurn();  // 턴을 끝냄

                }

                break;


            // 원하는 2*2 영역의 돌들을 다른 2*2 영역의 돌들과 교체하는 카드 (7) : index 8
            case MyHandStatus.exchangeArea2_2:
                if (i == 8 || j == 8)
                {
                    NetWorkManager.instance.printScreenString("다시 선택하세요");
                }
                else
                {
                    if (!areaSelected && !subAreaSelected && !isConfirmed)  // 카드 발동 후 맨 처음 클릭
                    {
                        areaSelected = true;
                        selectedBTNindex = i + j * 9;

                        PV.RPC("AreaBox_set2_2", RpcTarget.AllBuffered);
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i + 0.3f, 2.21f - 0.55f * j - 0.3f, 0));

                        NetWorkManager.instance.printScreenString("선택됨");
                    }
                    else if (areaSelected && !subAreaSelected && !isConfirmed && selectedBTNindex != (i + j * 9))  // 영역 선택 후 다른 부분을 클릭했을 때
                    {
                        selectedBTNindex = i + j * 9;

                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i + 0.3f, 2.21f - 0.55f * j - 0.3f, 0));

                        NetWorkManager.instance.printScreenString("선택됨");
                    }
                    else if (areaSelected && !subAreaSelected && !isConfirmed && selectedBTNindex == (i + j * 9))  // 영역 선택 후 같은 버튼 클릭 -> 확정
                    {
                        isConfirmed = true;

                        NetWorkManager.instance.printScreenString("확정됨");
                    }
                    else if (areaSelected && !subAreaSelected && isConfirmed)  // 영역 확정 후 서브 영역 선택
                    {
                        int BTNindex = i + j * 9;

                        if ((BTNindex >= selectedBTNindex - 10 && BTNindex <= selectedBTNindex - 8) ||
                            (BTNindex >= selectedBTNindex - 1 && BTNindex <= selectedBTNindex + 1) || (BTNindex >= selectedBTNindex + 8 && BTNindex <= selectedBTNindex + 10))   // 겹치는 영역을 선택한 경우
                        {
                            NetWorkManager.instance.printScreenString("겹치는 영역을 선택할 수 없습니다");
                        }
                        else
                        {
                            subAreaSelected = true;
                            subSelectedBTNindex = BTNindex;

                            PV.RPC("AreaBoxPlus_set2_2", RpcTarget.AllBuffered);
                            PV.RPC("moveAreaBoxPlus", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i + 0.3f, 2.21f - 0.55f * j - 0.3f, 0));

                            NetWorkManager.instance.printScreenString("선택됨");
                        }
                    }
                    else if (areaSelected && subAreaSelected && isConfirmed && subSelectedBTNindex != (i + j * 9))  // 서브 영역 선택 시 이전과 다른 버튼을 클릭한 경우
                    {
                        int BTNindex = i + j * 9;

                        if ((BTNindex >= selectedBTNindex - 10 && BTNindex <= selectedBTNindex - 8) ||
                            (BTNindex >= selectedBTNindex - 1 && BTNindex <= selectedBTNindex + 1) || (BTNindex >= selectedBTNindex + 8 && BTNindex <= selectedBTNindex + 10))   // 겹치는 영역을 선택한 경우
                        {
                            NetWorkManager.instance.printScreenString("겹치는 영역을 선택할 수 없습니다");
                            // 영역이 겹치면 돌이 중복될 수 있으므로 아예 겹치면 안됨
                        }
                        else
                        {
                            subSelectedBTNindex = BTNindex;

                            PV.RPC("moveAreaBoxPlus", RpcTarget.AllBuffered, new Vector3(-2.21f + 0.55f * i + 0.3f, 2.21f - 0.55f * j - 0.3f, 0));

                            NetWorkManager.instance.printScreenString("선택됨");
                        }
                    }
                    else if (areaSelected && subAreaSelected && isConfirmed && subSelectedBTNindex == (i + j * 9))  // 서브 영역 선택 후 같은 버튼 클릭 -> 확정
                    {
                        // 두 박스를 모두 보이지 않는 곳으로 치움
                        PV.RPC("moveAreaBox", RpcTarget.AllBuffered, new Vector3(10, 10, 0));
                        PV.RPC("moveAreaBoxPlus", RpcTarget.AllBuffered, new Vector3(12, 10, 0));
                        unInteractableAllBTN();

                        // 영역 데이터 변경 (Swap)
                        int temp;

                        // 각 영역 좌측 상단 데이터 변경
                        temp = gomokuData[selectedBTNindex];
                        gomokuData[selectedBTNindex] = gomokuData[subSelectedBTNindex];
                        gomokuData[subSelectedBTNindex] = temp;

                        // 각 영역 우측 상단 데이터 변경
                        temp = gomokuData[selectedBTNindex + 1];
                        gomokuData[selectedBTNindex + 1] = gomokuData[subSelectedBTNindex + 1];
                        gomokuData[subSelectedBTNindex + 1] = temp;

                        // 각 영역 좌측 하단 데이터 변경
                        temp = gomokuData[selectedBTNindex + 9];
                        gomokuData[selectedBTNindex + 9] = gomokuData[subSelectedBTNindex + 9];
                        gomokuData[subSelectedBTNindex + 9] = temp;

                        // 각 영역 우측 하단 데이터 변경
                        temp = gomokuData[selectedBTNindex + 10];
                        gomokuData[selectedBTNindex + 10] = gomokuData[subSelectedBTNindex + 10];
                        gomokuData[subSelectedBTNindex + 10] = temp;

                        // 데이터 변경 동기화
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, selectedBTNindex, gomokuData[selectedBTNindex]);
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, selectedBTNindex + 1, gomokuData[selectedBTNindex + 1]);
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, selectedBTNindex + 9, gomokuData[selectedBTNindex + 9]);
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, selectedBTNindex + 10, gomokuData[selectedBTNindex + 10]);

                        PV.RPC("ChangeData", RpcTarget.AllBuffered, subSelectedBTNindex, gomokuData[subSelectedBTNindex]);
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, subSelectedBTNindex + 1, gomokuData[subSelectedBTNindex + 1]);
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, subSelectedBTNindex + 9, gomokuData[subSelectedBTNindex + 9]);
                        PV.RPC("ChangeData", RpcTarget.AllBuffered, subSelectedBTNindex + 10, gomokuData[subSelectedBTNindex + 10]);

                        // 변수값 초기화
                        areaSelected = false;
                        subAreaSelected = false;
                        selectedBTNindex = -1;
                        subSelectedBTNindex = -1;
                        isConfirmed = false;
                        myHandStatus = MyHandStatus.cannotUseCard;

                        // 오목판 다시 그림
                        PV.RPC("reNewalBoard", RpcTarget.AllBuffered);
                        endMyTurn();  // 턴을 끝냄
                    }
                }

                break;
        }	
    }

    // 기능 : place 위치에 해당하는 버튼의 데이터를 data 변수 값으로 바꿈
    // 매개변수 : place (버튼의 번호), data (돌의 색깔)
    // 참조 : GameManager.useMagicCard()
    [PunRPC] void ChangeData(int place, int data)
    {
        gomokuData[place] = data;
    }

    // 기능 : gomokuData를 가지고 gomokuTable을 변경함 (데이터를 가지고 각 버튼이 보여지는 모습을 조절함)
    // 호출 : 게임을 처음 시작할 때, 
    [PunRPC] void repaintBoard()
    {
        // 81개의 모든 돌들에 대해서
        for (int i = 0;i <81;i++)
        {
            if(gomokuData[i]==0)    // 돌을 두지 않은 상태
            {
                // .sprite로 Image 컴포넌트의 Source Image에 접근
                gomokuTable[i].GetComponent<Image>().sprite = whiteStone;  // 의미X

                // 투명도 조절
                Color color = gomokuTable[i].GetComponent<Image>().color;
                color.a = 0;  // alpha 값을 0으로 설정하여 보이지 않게 함
                gomokuTable[i].GetComponent<Image>().color = color;
            }

            if (gomokuData[i] == 1) 
            {
                // Source Image를 검은 돌로 변경
                gomokuTable[i].GetComponent<Image>().sprite = blackStone;
                Color color = gomokuTable[i].GetComponent<Image>().color;
                color.a = 1;  // alpha 값을 1로 설정하여 보이게 함
                gomokuTable[i].GetComponent<Image>().color = color;
            }

            if(gomokuData[i]==2) 
            {
                // Source Image를 흰 돌로 변경
                gomokuTable[i].GetComponent<Image>().sprite = whiteStone;
                Color color = gomokuTable[i].GetComponent<Image>().color;
                color.a = 1;
                gomokuTable[i].GetComponent<Image>().color = color;
            }
        }
    }

    // 기능 : 
    // 참조 : GameManager.reNewalBoard()
    void dolmove(Image img) {
        Vector3 tmp=img.transform.position;
        Sequence seq=DOTween.Sequence();
        seq.Join(img.transform.DOMove(charging.center,0.75f));
        seq.Join(img.transform.DOScale(new Vector3(0,0,0),3f));
        seq.Join(img.DOFade(0, 2f).SetEase(Ease.InQuad));
        seq.Append(img.transform.DOMove(tmp,0));
        seq.Join(img.transform.DOScale(new Vector3(1,1,1),0));
    }


    // 기능 : 오목판을 다시 그린 뒤 오목이 완성되었는지 검사하고, 오목이 완성됐으면 완성된 5개의 돌들을 오목판에서 제거함
    // 참조 : 돌을 둔 후 (GameManager.PutStoneWithoutMagic()), 카드를 사용한 후 (GameManager.useMagicCard())
    [PunRPC]void reNewalBoard()
    {
        repaintBoard();  // 오목판을 다시 그림

        // 이제 오목 완성됐는지 검사
        while (checkGomoku((int)stoneColor.black)>=0) // 검은돌 검사
        {       /* checkGomoku() : deleteStartNum 값을 설정하고 돌들이 놓여있는 방향을 반환함 */
            switch (checkGomoku((int)stoneColor.black))  // 검은 돌이 오목을 완성한 경우
            {
                case 0: //  → 방향 제거
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

                // 연속된 5개의 돌들을 보이지 않게함
                var img =gomokuTable[deleteStartNum].GetComponent<Image>();
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
            if(PhotonNetwork.IsMasterClient)  // 검은 돌이 오목을 완성한 경우. 내가 MasterClient이면 내가 검은 돌을 두는 사람이므로 내가 공격에 성공한 것임 → 상대방 HP를 깎음
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

        while(checkGomoku((int)stoneColor.white)>=0) // 흰 돌 검사
        {
            switch(checkGomoku((int)stoneColor.white))
            {
                case 0: // → 방향 제거
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
            if(PhotonNetwork.IsMasterClient)  // 흰 돌이 오목을 완성한 경우. 내가 MasterClient이면 내가 검은 돌을 두는 사람이므로 상대방이 공격에 성공한 것임 → 내 HP를 깎음
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

    // 돌들이 놓여있는 방향
    // 가로(→), 세로(↓), 대각선(↘, ↙)
    enum dir
    {
        leftDir,  // → 0
        downDir,  // ↓ 1
        leftdownDir,
        rightdownDir
    }

    // 기능 : 오목이 완성됐는지를 확인하고, 완성됐으면 시작되는 돌(버튼)의 번호를 설정하고 돌들이 놓여있는 방향을 반환함
    // 매개변수 : color (돌의 색깔)
    // 반환 : 오목이 완성됐으면 : (int)dir.~ (5개의 돌들이 놓여있는 방향), 오목이 완성되지 않았으면 : -1
    // 참조 : GameManager.reNewalBoard()
    int checkGomoku(int color)
    { 
        for(int i = 0;i<5;i++)     // → 0
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
        return -1;  // 오목이 완성되지 않은 상태라면 -1을 반환함
    }

    #endregion

    #region 게임종료

    [SerializeField] GameObject ResultPannel;
    [SerializeField] TMP_Text ResultTMP;

    public ParticleSystem Part { get => part; set => part = value; }

    // 기능 : 진 사람이 호출하며, GameOver(), BackToLobby()를 순서대로 호출함
    // 참조 : 항복 (NetworkManager.Surrender()), 게임 패배 (PlayerManager.renewalHPBar())
    public void LoseGame()
    {
        GameOver();
        PV.RPC("GameOver", RpcTarget.OthersBuffered, "승리");  // 상대방의 GameOver() 함수를 호출함
    }

    public void draw() {
        PV.RPC("GameOver", RpcTarget.AllBuffered, "무승부");
    }

    // 기능 : 이긴 사람에게는 '승리'를, 진 사람에게는 '패배'를 출력하고 로비로 돌아감
    // 매개변수 : result ('승리' 나 '패배' 둘 중 하나의 값을 가짐)
    // 참조 : GameManager.LoseGame()
    [PunRPC]public void GameOver(string result = "패배")
    {
        ResultTMP.text = result;
        ResultPannel.SetActive(true);  // 게임 결과 패널 활성화
        StartCoroutine(BackToLobby());
    }

    // 기능 : 게임을 끝내고 로비로 되돌아감
    // 참조 : GameManager.GameOver()
    IEnumerator BackToLobby()
    {
        yield return new WaitForSeconds(2f);  // 2초를 기다림
        resetGameData();
        NetWorkManager.instance.EndGame();
        ResultPannel.SetActive(false);  // 게임 결과 패널 비활성화
        Destroy(PlayerManager.myPlayerManager);
        Destroy(PlayerManager.enemyPlayerManager);  // PlayerManager 모두 파괴
        GameObject[] cards = GameObject.FindGameObjectsWithTag("Card");
        foreach(GameObject card in cards)  // 게임에서 사용한 카드들을 모두 파괴함
        {
            Destroy(card);
        }
    }

    #endregion

    #region 잡다한 코드들

    // 기능 : 오목 판의 모든 버튼들을 클릭할 수 있게 만듦
    // 참조 : 카드를 사용할 때 (GameManager.setMyuseCardStatus())
    void interactableAllBTN()
    {
        for(int i = 0;i<81;i++)
        {
            gomokuTable[i].interactable = true;
        }
    }

    // 기능 : 오목 판의 모든 돌(버튼)들을 클릭할 수 없게 만듦 (돌을 두지 못하게 함)
    // 호출 : 게임을 처음 시작할 때, 
    void unInteractableAllBTN()
    {
        for(int i = 0;i<81;i++)
        {
            gomokuTable[i].interactable = false;
        }
    }

    // 기능 : 게임 데이터 초기화 (돌, 나와 상대방 PlayerManager)
    // 참조 : 게임을 처음 시작할 때, 게임이 모두 끝났을 때 (GameManager.BackToLobby())
    void resetGameData()
    {
        // 81개의 모든 돌들의 데이터를 0으로, 모든 버튼(돌)을 클릭할 수 없게 만듦
        for (int i = 0;i<81;i++)
        {
            gomokuData[i]=0; gomokuTable[i].interactable=false;
        }

        // 각 매니저 필요한거 초기화
        PlayerManager.myPlayerManager = PlayerManager.myPlayerManager;  // 나
        PlayerManager.enemyPlayerManager = PlayerManager.enemyPlayerManager;  // 상대방
    }

    // 기능 : 영역 박스의 위치를 변경함
    // 매개변수 : newposition (영역 박스가 존재해야할 새로운 위치)
    // 참조 : GameManager.useMagicCard()
    [PunRPC] void moveAreaBox(Vector3 newposition)
    {
        bluebox3_3.transform.position = newposition;
    }

    // 기능 : 영역 박스의 모양을 바둑판 3*3 크기의 정사각형으로 바꿈
    // 참조 : GameManager.useMagicCard()
    [PunRPC]void AreaBox_set3_3(){
        AreaBox.instance.setSize3_3();
    }

    // 기능 : 영역 박스의 모양을 바둑판 9*1 크기의 직사각형으로 바꿈 (가로)
    // 참조 : GameManager.useMagicCard()
    [PunRPC]void AreaBox_set9_1(){
        AreaBox.instance.setSize9_1();
    }

    // 기능 : 영역 박스의 모양을 바둑판 1*9 크기의 직사각형으로 바꿈 (세로)
    // 참조 : GameManager.useMagicCard()
    [PunRPC] void AreaBox_set1_9(){
        AreaBox.instance.setSize1_9();
    }

    // 기능 : 영역 박스의 모양을 바둑판 2*2 크기의 정사각형으로 바꿈
    // 참조 : GameManager.useMagicCard()
    [PunRPC] void AreaBox_set2_2()
    {
        AreaBox.instance.setSize2_2();
    }

    // 기능 : 영역 박스의 모양을 바둑판 1*1 크기의 정사각형으로 바꿈
    // 참조 : GameManager.useMagicCard()
    [PunRPC]
    void AreaBox_set1_1()
    {
        AreaBox.instance.setSize1_1();
    }

    // 기능 : 보조 영역 박스의 위치를 변경함
    // 매개변수 : newposition (보조 영역 박스가 존재해야할 새로운 위치)
    // 참조 : GameManager.useMagicCard()
    [PunRPC] void moveAreaBoxPlus(Vector3 newposition)
    {
        areaboxPlus.transform.position = newposition;
    }

    // 기능 : 보조 영역 박스의 모양을 바둑판 9*1 크기의 직사각형으로 바꾸고 (가로), 박스의 색깔을 메인 영역 박스의 색깔과 동일하게 변경함
    // 참조 : GameManager.useMagicCard()
    [PunRPC] void AreaBoxPlus_set9_1()
    {
        Color color = areaboxPlus.GetComponent<SpriteRenderer>().color;
        //color = Color.blue;
        //color.a = 0.5f;
        ColorUtility.TryParseHtmlString("#1230B984", out color);
        areaboxPlus.GetComponent<SpriteRenderer>().color = color;

        AreaBoxPlus.instance.setSize9_1();
    }

    // 기능 : 보조 영역 박스의 모양을 바둑판 2*2 크기의 정사각형으로 바꾸고, 박스의 색깔을 빨간색으로 변경함
    // 참조 : GameManager.useMagicCard()
    [PunRPC] void AreaBoxPlus_set2_2()
    {
        Color color = areaboxPlus.GetComponent<SpriteRenderer>().color;
        color = Color.red;
        color.a = 0.3f;
        areaboxPlus.GetComponent<SpriteRenderer>().color = color;

        AreaBoxPlus.instance.setSize2_2(); 
    }

    // 기능 : 보조 영역 박스의 모양을 바둑판 1*1 크기의 정사각형으로 바꾸고, 박스의 색깔을 빨간색으로 변경함
    // 참조 : GameManager.useMagicCard()
    [PunRPC] void AreaBoxPlus_set1_1()
    {
        Color color = areaboxPlus.GetComponent<SpriteRenderer>().color;
        color = Color.red;
        color.a = 0.3f;
        areaboxPlus.GetComponent<SpriteRenderer>().color = color;

        AreaBoxPlus.instance.setSize1_1();
    }





    // 참조 : Card.OnMouseUp()
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

/*
 * 버튼 81개 (0 ~ 80)
 * 좌측 상단 : (0, 0) - 0, 우측 하단 : (8, 8) - 80
 * 좌측 상단 (0, 0) 기준으로 오른쪽으로 x 좌표 증가, 아래쪽으로 y 좌표 증가
 * i : x 좌표
 * j : y 좌표
 */ 