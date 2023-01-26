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
        setResolution();  // �ػ� ����
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
            if(time > 0.3f)
            {
                GoNext();
                print(time);time = 0;
            }
        }   
    }

    void disableAllPannels()
    {
        for (int i = 0; i < 14; i++)
            image_list[i].SetActive(false);
    }


    // ��� : ���� �̹����� ���� ���� ������ �Ѿ�� ��
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


    // ��� : �÷��̾��� �����͸� json ���Ͽ� ������
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

    // ��� : json ���Ϸκ��� �÷��̾��� �����͸� ������
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

    // ��� : �ػ󵵸� �׻� 1920*1080 (16:9)�� ������
    // ���� : NetWorkManager.Start()
    public void setResolution()  // �ػ� 16:9 ����
    {
        int setWidth = 1080; // ����� ���� �ʺ�
        int setHeight = 1920; // ����� ���� ����

        int deviceWidth = Screen.width; // ��� �ʺ� ����
        int deviceHeight = Screen.height; // ��� ���� ����

        Screen.SetResolution(setWidth, (int)(((float)deviceHeight / deviceWidth) * setWidth), true); // SetResolution �Լ� ����� ����ϱ�

        if ((float)setWidth / setHeight < (float)deviceWidth / deviceHeight) // ����� �ػ� �� �� ū ���
        {
            float newWidth = ((float)setWidth / setHeight) / ((float)deviceWidth / deviceHeight); // ���ο� �ʺ�
            Camera.main.rect = new Rect((1f - newWidth) / 2f, 0f, newWidth, 1f); // ���ο� Rect ����

        }
        else // ������ �ػ� �� �� ū ���
        {
            float newHeight = ((float)deviceWidth / deviceHeight) / ((float)setWidth / setHeight); // ���ο� ����
            Camera.main.rect = new Rect(0f, (1f - newHeight) / 2f, 1f, newHeight); // ���ο� Rect ����
        }

        void OnPreCull() => GL.Clear(true, true, Color.black);  // ���� ������ ��� ���������� ä��
    }
}