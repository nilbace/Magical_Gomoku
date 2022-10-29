using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetWorkManager : MonoBehaviourPunCallbacks
{
    public PhotonView PV;
    public static NetWorkManager instance = null; 

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
    
    [Header("Start")]
    public GameObject StartScreen;
    public TextMeshProUGUI status; 
    public TMP_InputField nameField;
    public Button ConnectBtn;
    bool isStarted = false;
    

    [Header("Lobby")]
    public GameObject LobbyScreen;
    public TMP_InputField roomnameField;
    public TextMeshProUGUI welcome;
    public TextMeshProUGUI clientNumber;
    bool isLobby = false;
    [Header("RoomList")]
    public Button[] PNrooms;
    public Button previousPage; public Button nextPage;
    List<RoomInfo> myList = new List<RoomInfo>();
    int currentPage = 1, maxPage = 1, multiple = 5;

    [Header("Room")]
    public GameObject GamePannel;
    bool isGaming = false;

    enum State
    {
        StartEnum, LobbyEnum, GameEnum
    };
    void SetState(State nowstate)
    {
        if (nowstate==State.StartEnum)
        {
            StartScreen.SetActive(true);
            LobbyScreen.SetActive(false);
            GamePannel.SetActive(false);
            isLobby = false;
            isGaming = false;
        }
        else if(nowstate == State.LobbyEnum)
        {
            StartScreen.SetActive(false);
            LobbyScreen.SetActive(true);
            isLobby = true;
            isGaming = false;
            welcome.text = PhotonNetwork.LocalPlayer.NickName + "  Welcome";
        }
        else if(nowstate == State.GameEnum)
        {
            GamePannel.SetActive(true);
            isLobby = false;
            isGaming = true;
        }
    }
    
    void Update()
    {   if(SceneManager.GetActiveScene().buildIndex==0)
        status.text = PhotonNetwork.NetworkingClient.State.ToString();
        if(isLobby)
        clientNumber.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) +
            "Lobby / " + PhotonNetwork.CountOfPlayers + "Connected";
    }

    #region 스타트 화면
    void Start()
    {
        Screen.SetResolution(500, 800, false);
        PhotonNetwork.ConnectUsingSettings();
        SetState(State.StartEnum);
    }

    public override void OnConnectedToMaster()
    {
        status.color= Color.green;
        ConnectBtn.interactable = true;
        if(isStarted)
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        status.color = Color.red;
    }
    public void connectToLobby() //커넥트 버튼
    { 
        PhotonNetwork.LocalPlayer.NickName = nameField.text;
        PhotonNetwork.JoinLobby();
        myList.Clear();
        isStarted = true;
    }
    #endregion
   
    #region 로비 화면
    public override void OnJoinedLobby()
    {
        SetState(State.LobbyEnum);
        
        MyListRenewal();
        myList.Clear();
    }

    public void CreateRoom() 
    {
        if(roomnameField.text == "") 
        {
            print("Please Enter room Name");
            return;
        }
        PhotonNetwork.CreateRoom(roomnameField.text, new RoomOptions{MaxPlayers=2}, null);
        SetState(State.GameEnum);
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
        SetState(State.GameEnum);
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        PV.RPC("EnteredRoom", RpcTarget.All, newPlayer);
    }
    [PunRPC] void EnteredRoom(Player otherPlayer)
    {
        print(otherPlayer.NickName+"님이 입장하셨습니다.");
    }
    
    public void ExitGame()
    {
        PhotonNetwork.LeaveRoom();
        GamePannel.SetActive(false);
    }

    #endregion
}
