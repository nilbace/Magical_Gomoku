using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using UnityEngine.UI;
using TMPro;

public class Prologue : MonoBehaviour
{
    // 싱글턴
    public static Prologue instance;
    private void Awake()
    {
        instance = this;
    }

    public GameObject[] image_list = new GameObject[14];
    int num;
    float time = 0f;
    public PlayerData playerData;

    public GameObject CharacterExplain;
    public GameObject GameExplain;
    bool isCharacterExplain = false;
    bool truckWait = false;
    bool isNotFirst;

    // Start is called before the first frame update
    void Start()
    {
        setResolution(); 
        LoadPlayerDatafromJson();
        if (playerData.playeraHasPlayedTuitorial)
            SceneManager.LoadScene("Start");
        else
            num = 0;

        isNotFirst = playerData.isNotFirst;

        disableAllPannels();
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            if (truckWait)
            {
                if (time > 0.2f)
                {
                    GoNext();
                    time = 0;
                    truckWait = false;
                }
            }
            else
            {
                if (time > 0.07f)
                {
                    GoNext();
                    print(time); time = 0;
                }
            }
            this.gameObject.GetComponent<AudioSource>().Play();
        }

        if (!isNotFirst)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (GameExplain.activeSelf)
                {
                    playerData.isNotFirst = true;
                    SavePlayerDataToJson();

                    SceneManager.LoadScene("Start");
                }
                else if (CharacterExplain.activeSelf && !isCharacterExplain)
                {
                    isCharacterExplain= true;
                }
                else if (CharacterExplain.activeSelf && isCharacterExplain)
                {
                    CharacterExplain.SetActive(false);
                    GameExplain.SetActive(true);
                }
            }
        }
    }

    void disableAllPannels()
    {
        for (int i = 0; i < 14; i++)
            image_list[i].SetActive(false);

        CharacterExplain.SetActive(false);
        GameExplain.SetActive(false);
    }


    // 기능 : 다음 이미지로 넘어감
    void GoNext()
    {
        if (num < 14)
        {
            image_list[num].SetActive(true);

            if (num == 3)
            {
                truckWait = true;
            }
            else if (num == 4)
            {
                for (int i = 0; i < 4; i++)
                    image_list[i].SetActive(false);
            }
            else if (num == 5)
            {
                image_list[4].SetActive(false);
            }
            else if (num == 6)
            {
                image_list[5].SetActive(false);
            }
            else if (num == 9)
            {
                for (int i = 6; i < 9; i++)
                    image_list[i].SetActive(false);
            }
            else if (num == 12)
            {
                for (int i = 9; i < 12; i++)
                    image_list[i].SetActive(false);
            }

            num++;
        }
        else
        {
            for (int i = 12; i < 14; i++)
                image_list[i].SetActive(false);

            if (!isNotFirst)
            {
                CharacterExplain.SetActive(true);

                playerData.name = "test";
                playerData.playeraHasPlayedTuitorial = true;
                playerData.isNotFirst = true;
                playerData.mastervol = 1f;
                playerData.sfxvol = 1f;
                playerData.bgmvol = 1f;

                SavePlayerDataToJson();
            }
            else
            {
                playerData.playeraHasPlayedTuitorial = true;
                SavePlayerDataToJson();

                SceneManager.LoadScene("Start");
            }
        }
    }


    [ContextMenu("To Json Data")]
    public void SavePlayerDataToJson()
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

        FileStream fileStream = new FileStream(path, FileMode.Create);
        byte[] data = Encoding.UTF8.GetBytes(jsonData);
        fileStream.Write(data, 0, data.Length);
        fileStream.Close();
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

        FileStream fileStream = new FileStream(path, FileMode.Open);
        byte[] data = new byte[fileStream.Length];
        fileStream.Read(data, 0, data.Length);
        fileStream.Close();
        string jsonData = Encoding.UTF8.GetString(data);

        playerData = JsonUtility.FromJson<PlayerData>(jsonData);
    }


    public void setResolution() 
    {
        int setWidth = 1080; 
        int setHeight = 1920; 

        int deviceWidth = Screen.width;
        int deviceHeight = Screen.height; 

        Screen.SetResolution(setWidth, (int)(((float)deviceHeight / deviceWidth) * setWidth), true); 

        if ((float)setWidth / setHeight < (float)deviceWidth / deviceHeight)
        {
            float newWidth = ((float)setWidth / setHeight) / ((float)deviceWidth / deviceHeight);
            Camera.main.rect = new Rect((1f - newWidth) / 2f, 0f, newWidth, 1f);

        }
        else 
        {
            float newHeight = ((float)deviceWidth / deviceHeight) / ((float)setWidth / setHeight); 
            Camera.main.rect = new Rect(0f, (1f - newHeight) / 2f, 1f, newHeight); 
        }

        void OnPreCull() => GL.Clear(true, true, Color.black);  
    }
}