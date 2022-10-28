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
    bool isStart = false;
    

    [Header("Lobby")]
    public GameObject LobbyScreen;
    public TMP_InputField roomnameField;
    public TextMeshProUGUI welcome;
    public TextMeshProUGUI clientNumber;
    bool isLobby = false;
    

    void Start()
    {
        Screen.SetResolution(960, 540, false);
        PhotonNetwork.ConnectUsingSettings();
        isStart = true;
    }

    public override void OnConnectedToMaster()
    {
        status.color= Color.green;
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
        
    }
   
    public override void OnJoinedLobby()
    {
        isLobby = true;
        LobbyScreen.SetActive(true);
    }

    public void CreateRoom() => 
        PhotonNetwork.CreateRoom(roomnameField.text, new RoomOptions{MaxPlayers=2}, null);

    
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
}
