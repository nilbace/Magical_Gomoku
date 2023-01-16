using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.IO;
using System;


// 네트워크 매니저 및 UI 매니저

public class NetWorkManager : MonoBehaviourPunCallbacks
{
    public PhotonView PV;
    public static NetWorkManager instance = null; 
    public TextMeshProUGUI status; 
    

    private void Awake() // 싱글턴
    {
        if (instance == null) 
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            if (instance != this)
                Destroy(this.gameObject);
        }
    }
    
    [Header("Setting")]
    public GameObject SettingPannel;  // 설정패널
    public TMP_InputField changenameInputfield;  // 설정패널 - 이름 입력 필드

    [Header("Start")]
    public GameObject StartPannel;  // 스타트화면
    public Button gotoSchoolBTN;  // 스타트화면 - '마법학교로' 버튼

    [Header("GameEndPannel")]
    public GameObject GameEndPannel;  // 게임종료패널

    [Header("Lobby")]
    public GameObject LobbyPannel;  // 로비 패널
    public TMP_InputField roomnameField;  // 로비 패널 - 방 이름 입력 필드
    public TextMeshProUGUI welcomeTMP;  // 로비 패널 - 상단 '~님 환영합니다' 문자
    public TextMeshProUGUI clientNumberTMP;  // 로비 패널 - 상단 'n Lobby / n Connected' 문자
    public Button[] PNrooms;
    public Button previousPage; public Button nextPage;
    List<RoomInfo> myList = new List<RoomInfo>();
    int currentPage = 1, multiple = 5;
    public bool AlreadyLobbyed = false;     //이미 로비에 갔었다면 Disconnected됐다가 다시 연결됐을 때 로비로 돌아감
                                            //처음에 마법학교로 버튼 누르면 true된후에 계속 true로 냅두면됨

    [Header("Game")]
    public GameObject GamePannel;  // 게임 패널
    public GameObject pausePannel;  // 일시정지
    public AudioMixer audiomixer;
    public Slider masterslid;
    public Slider bgmslid;
    public Slider sfxslid;
  
    void Update()
    {   
        if(LobbyPannel.activeSelf)  // 로비 패널이 열려있으면
        {
            clientNumberTMP.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) +
            "Lobby / " + PhotonNetwork.CountOfPlayers + "Connected";
            welcomeTMP.text = PhotonNetwork.LocalPlayer.NickName + "님 환영합니다";  // 상단 문자열 설정
        }

        if (GamePannel.activeSelf && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount == 2) 
        {
            status.text = "실습중";
        }

        if (Application.platform == RuntimePlatform.Android)   // 플랫폼이 안드로이드이면
        {
            if (Input.GetKey(KeyCode.Escape))  // 뒤로 가기 버튼 처리
            {
                HandlingBackButton(); 
            }
        }
    }

    #region 세팅화면
    public void closeSettingPannel()  // 일시정지로 돌아가지 않아도 되면 (스타트화면에서 환경설정을 연 경우)
    {
        if(returntoPause == false)
        {
            closeAllPannel();
            StartPannel.SetActive(true);  // 모든 패널을 닫고 스타트화면만 활성화함
        }
        else  // 일지정지로 돌아가야되면 (게임 중에 환경설정을 연 경우)
        {
            SettingPannel.SetActive(false);
            pausePannel.SetActive(true);
            returntoPause = false;
        }

    }

    public void prologueReview()
    {
        printScreenString("미구현");

    }

    public void howToPlayReview()
    {
        printScreenString("미구현");

    }

    // 플레이어의 이름을 변경하고 Json 데이터로 저장함
    // 설정패널 -> 이름변경 버튼의 이벤트 함수
    public void changenameBTN()
    {
        playerData.name = changenameInputfield.text;
        SavePlayerDataToJson();
        PhotonNetwork.NickName = playerData.name;
        printScreenString("이름이 변경되었습니다");
    }

    #endregion


    #region 스타트 화면
    
    void Start()
    {
        setResolution();  // 해상도 설정
        PhotonNetwork.ConnectUsingSettings();
        status.color = Color.magenta; status.text = "연결중";
        closeAllPannel(); StartPannel.SetActive(true); gotoSchoolBTN.interactable=false;  // 스타트화면만 활성화함
        LoadPlayerDatafromJson();  // json 파일로부터 플레이어 데이터를 가져옴

        masterslid.value=playerData.mastervol;
        sfxslid.value=playerData.sfxvol;
        bgmslid.value=playerData.bgmvol;
    }

    public override void OnConnectedToMaster()
    {
        status.color= Color.green;
        status.text = "학교가는중";
        gotoSchoolBTN.interactable=true;
        PhotonNetwork.LocalPlayer.NickName = playerData.name;
        if(AlreadyLobbyed)
        {
            closeAllPannel();
            LobbyPannel.SetActive(true);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        status.color = Color.red;
        status.text = "연결실패 재접속중";
        PhotonNetwork.ConnectUsingSettings();
    }

    // 스타트화면 -> 마법학교로 버튼의 이벤트 함수
    public void gotoMagicSchoolBTN() // '마법학교로' 버튼
    { 
        if(PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby();
            myList.Clear();
            AlreadyLobbyed = true;
        }
    }

    // 스타트화면 -> '나가기' 버튼의 이벤트 함수
    public void QuitGameBTN()  // 나가기 버튼
    {
        closeAllPannel();
        GameEndPannel.SetActive(true);  // 게임 종료 패널을 활성화함
    }

    // 스타트화면 -> '설정' 버튼의 이벤트 함수
    public void SettingBTN() //설정 버튼
    {
        closeAllPannel();
        SettingPannel.SetActive(true);  // 설정 패널을 활성화함
    }

    // 스타트화면 -> '제작진' 버튼의 이벤트 함수
    public void productionStaffBTN() 
    {
        printScreenString("미개발상태");
    }

    #endregion

    #region 게임종료패널

    // 게임종료패널 -> '예' 버튼의 이벤트 함수
    public void EndPannelYesBTN()
    {
        Application.Quit();  // 프로그램 종료
    }

    // 게임종료패널 -> '아니오' 버튼의 이벤트 함수
    public void EndPannelNoBTN()
    {
        closeAllPannel();
        StartPannel.SetActive(true);  // 스타트화면 활성화
    }

    #endregion

    #region 로비 화면

    // 로비패널 -> '이전 화면' 버튼의 이벤트 함수
    public void toStartPannelBTN()
    {
        closeAllPannel();
        StartPannel.SetActive(true);  // 스타트화면 활성화
    }
    public override void OnJoinedLobby()
    {
        closeAllPannel(); LobbyPannel.SetActive(true);
        MyListRenewal();
        myList.Clear();
        status.text="실습실 가는중";
    }

    public void CreateRoom() 
    {
        if(roomnameField.text == "") 
        {
            print("Please Enter room Name");
            return;
        }
        PhotonNetwork.CreateRoom(roomnameField.text, new RoomOptions{MaxPlayers=2}, null);
        closeAllPannel(); GamePannel.SetActive(true);
        GameManager.instance.Start();
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        print("another Room name required");
    }

    
    public void MyListClick(int num)
    {
        if(num == -2) --currentPage;
        else if(num == -1) ++currentPage;
        else {
        PhotonNetwork.JoinRoom(myList[multiple+num].Name);
        GameManager.instance.Start();}
        MyListRenewal();
    }

    public void MyListRenewal()
    {
        previousPage.interactable = (currentPage >= 2);
        nextPage.interactable = (currentPage <= 5);

        multiple = (currentPage-1)*5;

        for(int i =0;i<5;i++)
        {
            if(multiple + i < myList.Count)
            {
                PNrooms[i].interactable = (myList[multiple+i].PlayerCount == 1) ? true : false;
                PNrooms[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text
                 = (myList[multiple+i].PlayerCount == 1) ? myList[multiple+i].Name : "Full";                               
            }
            else
            {
                PNrooms[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Empty Room";
                PNrooms[i].interactable = false;
            }
        }
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        int roomCount = roomList.Count;
        for(int i = 0;i<roomCount;i++)
        {
            if(!roomList[i].RemovedFromList)
            {
                if(!myList.Contains(roomList[i])) myList.Add(roomList[i]);
                else myList[myList.IndexOf(roomList[i])] = roomList[i];
            }
            else if(myList.IndexOf(roomList[i])!= -1)myList.RemoveAt(myList.IndexOf(roomList[i]));
            MyListRenewal();
        }
    }
    #endregion


    #region 게임 화면
    public override void OnJoinedRoom()
    {
        closeAllPannel();
        GamePannel.SetActive(true);
        status.text="실습 상대 기다리는중";
        PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
    }
    
    
    
    public void ExitGamePannel()
    {
        PhotonNetwork.LeaveRoom();
        closeAllPannel(); LobbyPannel.SetActive(true);
    }

    #endregion

    #region 잡다한 코드들

    // 기능 : 패널들을 모두 닫음
    void closeAllPannel()
    {
        SettingPannel.SetActive(false);
        StartPannel.SetActive(false);
        GameEndPannel.SetActive(false);
        LobbyPannel.SetActive(false);
        gamepannelset();
        GamePannel.SetActive(false);
        pausePannel.SetActive(false);
    }

    void gamepannelset() {
        GameObject[] cards = GameObject.FindGameObjectsWithTag("Card");
        foreach(GameObject card in cards)  // 게임에서 사용한 카드들을 모두 파괴함
        {
            Destroy(card);
        }
        if(GameManager.instance) {
            GameManager.instance.areaSelected = false;  // 영역을 선택했는지 여부
            GameManager.instance.selectedBTNindex = -1;  // 영역을 선택했을 때, 기준이 되는 버튼의 번호
            GameManager.instance.subAreaSelected = false;  // 보조 영역을 선택했는지 여부
            GameManager.instance.subSelectedBTNindex = -1;  // 보조 영역을 선택했을 때, 기준이 되는 버튼의 번호
            GameManager.instance.isConfirmed = false;  // 메인 영역이 확정됐는지 여부
            GameManager.instance.putStoneTwice = true;  // 돌을 2번 둘지 여부 (기본값 : true)
            GameManager.instance.myHandStatus = GameManager.MyHandStatus.cannotUseCard;
        }
    }
    
    [Header("택스트 경고")]
    public GameObject WarningText;

    // 기능 : 화면에 텍스트를 출력함
    public void printScreenString(string str)
    {
        StartCoroutine(printString(str));
    }

    // 기능 : 화면에 텍스트를 출력하고 1초 뒤 없앰
    IEnumerator printString(string str)
    {
        var textInfo = Instantiate(WarningText);
        var text = textInfo.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        text.text = str;
        yield return new WaitForSeconds(1f);
        Destroy(textInfo);
    }
    public PlayerData playerData;

    public void setmaster(float sliderval) {
        audiomixer.SetFloat("Master", Mathf.Log10(sliderval)*20);
        playerData.mastervol=sliderval;
        SavePlayerDataToJson();
    }

    public void setbgm(float sliderval) {
        audiomixer.SetFloat("BGM", Mathf.Log10(sliderval)*20);
        playerData.bgmvol=sliderval;
        SavePlayerDataToJson();
    }
    public void setsfx(float sliderval) {
        audiomixer.SetFloat("SFX", Mathf.Log10(sliderval)*20);
        playerData.sfxvol=sliderval;
        SavePlayerDataToJson();
    }

    // 기능 : 플레이어의 데이터를 json 파일에 저장함
    // 참조 : NetWorkManager.changeNameBTN()
    [ContextMenu("To Json Data")]public void SavePlayerDataToJson()
    {
        string path;
        if (Application.platform == RuntimePlatform.Android)
        {
            path = Path.Combine(Application.persistentDataPath, "playerData.json");
        }
        else
        {
            path = Path.Combine(Application.dataPath, "playerData.json");
        }
        string jsonData = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(path, jsonData);
    }

    // 기능 : json 파일로부터 플레이어의 데이터를 가져옴
    // 참조 : NetWorkManager.Start()
    public void LoadPlayerDatafromJson()
    {
        string path;
        if (Application.platform == RuntimePlatform.Android)
        {
            path = Path.Combine(Application.persistentDataPath, "playerData.json");
        }
        else
        {
            path = Path.Combine(Application.dataPath, "playerData.json");
        }

        string jsonData = File.ReadAllText(path);
        playerData = JsonUtility.FromJson<PlayerData>(jsonData);
    }

    // 기능 : 해상도를 항상 1920*1080 (16:9)로 고정함
    // 참조 : NetWorkManager.Start()
    public void setResolution()  // 해상도 16:9 고정
    {
        int setWidth = 1080; // 사용자 설정 너비
        int setHeight = 1920; // 사용자 설정 높이

        int deviceWidth = Screen.width; // 기기 너비 저장
        int deviceHeight = Screen.height; // 기기 높이 저장

        Screen.SetResolution(setWidth, (int)(((float)deviceHeight / deviceWidth) * setWidth), true); // SetResolution 함수 제대로 사용하기

        if ((float)setWidth / setHeight < (float)deviceWidth / deviceHeight) // 기기의 해상도 비가 더 큰 경우
        {
            float newWidth = ((float)setWidth / setHeight) / ((float)deviceWidth / deviceHeight); // 새로운 너비
            Camera.main.rect = new Rect((1f - newWidth) / 2f, 0f, newWidth, 1f); // 새로운 Rect 적용

        }
        else // 게임의 해상도 비가 더 큰 경우
        {
            float newHeight = ((float)deviceWidth / deviceHeight) / ((float)setWidth / setHeight); // 새로운 높이
            Camera.main.rect = new Rect(0f, (1f - newHeight) / 2f, 1f, newHeight); // 새로운 Rect 적용
        }

        void OnPreCull() => GL.Clear(true, true, Color.black);  // 남는 여백을 모두 검정색으로 채움
    }

    // 기능 : pannel에 따라 뒤로 가기 버튼 처리 (게임 버튼에 붙어있는 이벤트 함수 호출)
    // 참조 : NetWorkManager.Update()
    void HandlingBackButton()
    {
        if (SettingPannel.activeSelf == true)  // 설정 패널 열려있음
        {
            closeSettingPannel(); 
        }
        else if (StartPannel.activeSelf == true)  // 스타트화면 열려있음
        {
            QuitGameBTN();
        }
        else if (GameEndPannel.activeSelf == true)  // 게임종료패널 열려있음
        {
            EndPannelNoBTN();
        }
        else if (LobbyPannel.activeSelf == true)  // 로비패널 열려있음
        {
            toStartPannelBTN();
        }
        else if (GamePannel.activeSelf == true && pausePannel.activeSelf == false)
        {  // 게임패널만 열려있음 (일시정지가 아닌 경우)
            PauseBTN();
        }
        else if (GamePannel.activeSelf == true && pausePannel.activeSelf == true)
        {  // 게임패널과 일시정지 모두 열려있음 (일시정지인 경우)
            ClosePausePannel();
        }
    }

    #endregion

    #region 게임종료, 일시정지

    // 게임 패널 -> 일시정지 버튼의 이벤트 함수
    public void PauseBTN()
    {
        pausePannel.SetActive(true);  // 다른 패널들을 닫지 않고, 게임 패널이 활성화된 상태에서 일시정지 패널을 추가로 활성화함
    }

    // 일시정지 -> 'X' 버튼의 이벤트 함수
    public void ClosePausePannel()
    {
        pausePannel.SetActive(false);  // 일시정지 패널을 비활성화함
    }

    // 일시정지 -> '환경설정' 버튼의 이벤트 함수
    public void PauseToSetting()
    {
        pausePannel.SetActive(false);
        SettingPannel.SetActive(true);
        returntoPause = true;  // 일시정지 패널로 돌아오게 설정함
    }

    bool returntoPause = false;  // 일시정지 화면으로 돌아가야하는지 여부

    // 기능 : 항복
    // 일시정지 -> '항복하기' 버튼의 이벤트 함수
    public void Surrender()
    {
        pausePannel.SetActive(false);
        GameManager.instance.LoseGame();
    }

    public void draw() 
    {
        PlayerManager.myPlayerManager.character_img.GetComponent<SpriteRenderer>().sprite=PlayerManager.myPlayerManager.drawimg;
        PlayerManager.myPlayerManager.drawready=true;
        PV.RPC("drawsyncro", RpcTarget.OthersBuffered);
        if(PlayerManager.myPlayerManager.drawready==true && PlayerManager.enemyPlayerManager.drawready==true) GameManager.instance.draw();
        
        
    }

    [PunRPC] void drawsyncro() {
        PlayerManager.enemyPlayerManager.character_img.GetComponent<SpriteRenderer>().sprite=PlayerManager.enemyPlayerManager.drawimg;
        PlayerManager.enemyPlayerManager.drawready=true;
    }

    // 기능 : 
    // 참조 : GameManager.BackToLobby()
    public void EndGame()
    {
        closeAllPannel();
        LobbyPannel.SetActive(true);  // 모든 패널을 끄고 로비 패널만 활성화함
        PhotonNetwork.LeaveRoom();
        StartCoroutine(afterEndGame());
    }

    IEnumerator afterEndGame()
    {
        while(true)
        {
            yield return new WaitForSeconds(1f);
            if(PhotonNetwork.IsConnected)
            {
                PhotonNetwork.JoinLobby();
                break;
            }
        }
    }

    #endregion
}

// 플레이어의 정보
[System.Serializable]
public class PlayerData
{
    public string name;  // 이름 (닉네임)
    public bool playeraHasPlayedTuitorial = false;  // 튜토리얼을 봤는지 여부
    public float mastervol;
    public float sfxvol;
    public float bgmvol;
}

/*
 * 환경 설정을 열 수 있는 경우가 2가지 존재
    (1) 스타트 화면에서
    (2) 게임 중에 (일시정지버튼을 누르고 난 뒤)
 * 
 * 
 */ 