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
    bool isStart = false;
    

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
    public int currentPage = 1, maxPage = 1, multiple = 5;

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
            isStart = true;
            isLobby = false;
            isGaming = false;
        }
        else if(nowstate == State.LobbyEnum)
        {
            StartScreen.SetActive(false);
            LobbyScreen.SetActive(true);
            GamePannel.SetActive(false);
            isStart = false;
            isLobby = true;
            isGaming = false;
        }
        else if(nowstate == State.GameEnum)
        {
            StartScreen.SetActive(false);
            LobbyScreen.SetActive(false);
            GamePannel.SetActive(true);
            isStart = false;
            isLobby = false;
            isGaming = true;
        }
    }
    
    #region 스타트 화면
    void Start()
    {
        Screen.SetResolution(960, 540, false);
        PhotonNetwork.ConnectUsingSettings();
        SetState(State.StartEnum);
    }

    public override void OnConnectedToMaster()
    {
        status.color= Color.green;
        ConnectBtn.interactable = true;
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
    }
    #endregion
   
    #region 로비 화면
    public override void OnJoinedLobby()
    {
        SetState(State.LobbyEnum);
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
            PNrooms[i].interactable = (multiple+i<myList.Count)?true:false;
            PNrooms[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (multiple + i < myList.Count) ? myList[multiple+i].Name : "";
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
    void Update()
    {   if(SceneManager.GetActiveScene().buildIndex==0)
        status.text = PhotonNetwork.NetworkingClient.State.ToString();

        if(isLobby)
        {
            clientNumber.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) +
            "Lobby / " + PhotonNetwork.CountOfPlayers + "Connected";
            welcome.text = PhotonNetwork.LocalPlayer.NickName + "  Welcome";
        }
    }
    #endregion
}
