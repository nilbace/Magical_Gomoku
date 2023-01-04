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

public class NetWorkManager : MonoBehaviourPunCallbacks
{
    public PhotonView PV;
    public static NetWorkManager instance = null; 
    public TextMeshProUGUI status; 
    

    private void Awake() //싱글턴
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
    public GameObject SettingPannel;
    public TMP_InputField changenameInputfield;
    
    [Header("Start")]
    public GameObject StartPannel;
    public Button gotoSchoolBTN;
    [Header("GameEndPannel")]
    public GameObject GameEndPannel;

    [Header("Lobby")]
    public GameObject LobbyPannel;
    public TMP_InputField roomnameField;
    public TextMeshProUGUI welcomeTMP;
    public TextMeshProUGUI clientNumberTMP;
    public Button[] PNrooms;
    public Button previousPage; public Button nextPage;
    List<RoomInfo> myList = new List<RoomInfo>();
    int currentPage = 1, multiple = 5;

    [Header("Game")]
    public GameObject GamePannel;
    public GameObject pausePannel;
    public AudioMixer audiomixer;
    public Slider masterslid;
    public Slider bgmslid;
    public Slider sfxslid;
  
    void Update()
    {   
        if(LobbyPannel.activeSelf)
        {
            clientNumberTMP.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) +
            "Lobby / " + PhotonNetwork.CountOfPlayers + "Connected";
            welcomeTMP.text = PhotonNetwork.LocalPlayer.NickName + "님 환영합니다";
        }

        if(GamePannel.activeSelf && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount==2)
        {
            status.text = "실습중";
        }

        if (Application.platform == RuntimePlatform.Android) 
        {
            if (Input.GetKey(KeyCode.Escape))  // 뒤로 가기 버튼
            {
                HandlingBackButton(); 
            }
        }
    }

    #region 세팅화면
    public void closeSettingPannel()
    {
        if(returntoPause == false)
        {closeAllPannel(); StartPannel.SetActive(true);}
        else
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
        //Screen.SetResolution(1080, 1920, false);
        //Screen.SetResolution(540, 960, false);
        setResolution();
        PhotonNetwork.ConnectUsingSettings();
        status.color = Color.magenta; status.text = "연결중";
        closeAllPannel(); StartPannel.SetActive(true); gotoSchoolBTN.interactable=false;
        LoadPlayerDatafromJson();
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
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        status.color = Color.red;
        status.text = "연결실패 재접속해주세요";
    }
    public void gotoMagicSchoolBTN() //마법학교로 버튼
    { 
        if(PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby();
            myList.Clear();
        }
    }

    public void QuitGameBTN() //나가기 버튼
    {
        closeAllPannel();
        GameEndPannel.SetActive(true);
    }

    public void SettingBTN() //설정 버튼
    {
        closeAllPannel();
        SettingPannel.SetActive(true);
    }

    public void productionStaffBTN() //제작진 버튼
    {
        printScreenString("미개발상태");
    }

    #endregion
   
    #region 게임종료패널
    public void EndPannelYesBTN()
    {
        Application.Quit();
    }

    public void EndPannelNoBTN()
    {
        closeAllPannel();
        StartPannel.SetActive(true);
    }

    #endregion
    
    #region 로비 화면

    public void toStartPannelBTN()
    {
        closeAllPannel();
        StartPannel.SetActive(true);
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
    void closeAllPannel() //모두 닫기
    {
        SettingPannel.SetActive(false);
        StartPannel.SetActive(false);
        GameEndPannel.SetActive(false);
        LobbyPannel.SetActive(false);
        GamePannel.SetActive(false);
        pausePannel.SetActive(false);
    }
    
    [Header("택스트 경고")]
    public GameObject WarningText;

    public void printScreenString(string str)
    {
        StartCoroutine(printString(str));
    }

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

        void OnPreCull() => GL.Clear(true, true, Color.black);
    }

    // pannel에 따라 뒤로 가기 버튼 처리 (게임 버튼에 붙어있는 이벤트 함수 호출)
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

    public void PauseBTN()
    {
        pausePannel.SetActive(true);
    }

    
    public void ClosePausePannel()
    {
        pausePannel.SetActive(false);
    }

    public void PauseToSetting()
    {
        pausePannel.SetActive(false);
        SettingPannel.SetActive(true);
        returntoPause = true;
    }
    bool returntoPause = false;

    public void Surrender()
    {
        pausePannel.SetActive(false);
        GameManager.instance.LoseGame();
    }

    public void EndGame()
    {
        closeAllPannel();
        LobbyPannel.SetActive(true);
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

[System.Serializable]
public class PlayerData
{
    public string name;
    public bool playeraHasPlayedTuitorial = false;
    public float mastervol;
    public float sfxvol;
    public float bgmvol;
}
