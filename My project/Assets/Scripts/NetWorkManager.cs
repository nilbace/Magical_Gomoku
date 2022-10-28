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
    public TextMeshProUGUI[] RoomNames;
    public Button previousPage; public Button nextPage;
    List<RoomInfo> myList = new List<RoomInfo>();
    public int currentPage = 1, maxPage = 1, multiple = 5;

    [Header("Room")]
    public GameObject GamePannel;
    bool isGaming = false;

    void test()
    {
        
    }

    void Start()
    {
        Screen.SetResolution(960, 540, false);
        PhotonNetwork.ConnectUsingSettings();
        StartScreen.SetActive(true);
        LobbyScreen.SetActive(false);
        isStart = true;
        ConnectBtn.interactable = false;
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
        isStart = false;
        PhotonNetwork.LocalPlayer.NickName = nameField.text;
        PhotonNetwork.JoinLobby();
        StartScreen.SetActive(false);
        myList.Clear();
    }
   
    public override void OnJoinedLobby()
    {
        isLobby = true;
        LobbyScreen.SetActive(true);
    }

    public void CreateRoom() 
    {
        if(roomnameField.text == "") 
        {
            print("Please Enter room Name");
            return;
        }
        PhotonNetwork.CreateRoom(roomnameField.text, new RoomOptions{MaxPlayers=2}, null);
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        print("another Room name required");
    }

    #region 방목록갱신
    public void MyListClick(int num)
    {
        if(num == -2) --currentPage;
        else if(num == -1) ++currentPage;
        else PhotonNetwork.JoinRoom(myList[multiple+num].Name);
    }

    public void MyListRenewal()
    {
        previousPage.interactable = (currentPage == 2);
        nextPage.interactable = (currentPage == 1);

        multiple = (currentPage-1)*5;
        for(int i =0;i<5;i++)
        {
            PNrooms[i].interactable = (multiple+i<myList.Count)?true:false;
            RoomNames[i].text = (multiple + i < myList.Count) ? myList[multiple+i].Name : "";
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
            for(int i = 0;i<myList.Count;i++)
            {
                RoomNames[i].text = myList[i].Name;
            }
        }
    }
}
