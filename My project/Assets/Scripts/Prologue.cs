using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class Prologue : MonoBehaviour
{
    //public GameObject[] pannel_list = new GameObject[6];
    public GameObject[] image_list = new GameObject[14];
    int num;
    float time = 0f;
    public PlayerData playerData;


    // Start is called before the first frame update
    void Start()
    {
        setResolution();  // 해상도 설정
        LoadPlayerDatafromJson();
        if (playerData.playeraHasPlayedTuitorial)
            SceneManager.LoadScene("Start");
        else
            num = 0;

        disableAllPannels();
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            time = 0f;
        }

        if (time >= 2f)
        {
            time = 0f;

            GoNext();
        }    
    }

    void disableAllPannels()
    {
        for (int i = 0; i < 14; i++)
            image_list[i].SetActive(false);
    }

    private void OnMouseDown()
    {
        GoNext();
    }

    // 기능 : 다음 이미지나 메인 게임 씬으로 넘어가게 함
    void GoNext()
    {
        if (num < 14)
        {
            image_list[num].SetActive(true);

            if (num == 4)
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
            /*playerData.playeraHasPlayedTuitorial = true;
            SavePlayerDataToJson();*/

            SceneManager.LoadScene("Start");
        }
    }


    // 기능 : 플레이어의 데이터를 json 파일에 저장함
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
        File.WriteAllText(path, jsonData);
    }

    // 기능 : json 파일로부터 플레이어의 데이터를 가져옴
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
}