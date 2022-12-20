using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;


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
        Screen.SetResolution(540, 960, false);
        PhotonNetwork.ConnectUsingSettings();
        status.color = Color.magenta; status.text = "연결중";
        closeAllPannel(); StartPannel.SetActive(true); gotoSchoolBTN.interactable=false;
        LoadPlayerDatafromJson();
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
        else PhotonNetwork.JoinRoom(myList[multiple+num].Name);
        GameManager.instance.Start();
        MyListRenewal();
    }

    public void MyListRenewal()
    {
        previousPage.interactable = (currentPage == 2);
        nextPage.interactable = (currentPage == 1);

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
    
    [ContextMenu("To Json Data")]public void SavePlayerDataToJson()
    {
        string jsonData = JsonUtility.ToJson(playerData);
        string path = Path.Combine(Application.dataPath,"playerData.json");
        File.WriteAllText(path, jsonData);
    }

    public void LoadPlayerDatafromJson()
    {
        string path = Path.Combine(Application.dataPath,"playerData.json");
        string jsonData = File.ReadAllText(path);
        playerData = JsonUtility.FromJson<PlayerData>(jsonData);
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
}
