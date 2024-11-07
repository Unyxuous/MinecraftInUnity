using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    public GameObject mainMenuObject;
    public GameObject settingsMenuObject;

    [Header("Main Menu UI Elements")]
    public TextMeshProUGUI seedField;

    [Header("Settings Menu UI Elements")]
    public Slider viewDistSlider;
    public TextMeshProUGUI viewDistText;
    public Slider mouseSlider;
    public TextMeshProUGUI mouseText;
    public Toggle threadingToggle;
    public TMP_Dropdown clouds;

    Settings settings;

    public void Awake()
    {
        if (!File.Exists(Application.dataPath + "/settings.cfg"))
        {
            Debug.Log("Could not load settings file, creating new one.");

            settings = new Settings();
            string jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
        }
        else
        {
            string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
            settings = JsonUtility.FromJson<Settings>(jsonImport);
        }
    }

    public void StartGame() 
    {
        TerrainGeneration.heightMapSeed = Mathf.Abs(seedField.text.GetHashCode()) / VoxelData.WORLD_SIZE_IN_VOXELS;
        SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    public void EnterSettings()
    {
        viewDistSlider.value = settings.viewDistance;
        UpdateViewDistSlider();
        mouseSlider.value = settings.mouseSensitivity;
        UpdateMouseSlider();
        threadingToggle.isOn = settings.enableThreading;
        clouds.value = (int)settings.clouds;

        mainMenuObject.SetActive(false);
        settingsMenuObject.SetActive(true);
    }

    public void LeaveSettings()
    {
        settings.viewDistance = (int)viewDistSlider.value;
        settings.mouseSensitivity = mouseSlider.value;
        settings.enableThreading = threadingToggle.isOn;
        settings.clouds = (CloudStyle)clouds.value;

        string jsonExport = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);

        mainMenuObject.SetActive(true);
        settingsMenuObject.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void UpdateViewDistSlider()
    {
        viewDistText.text = "View Distance: " + viewDistSlider.value;
    }

    public void UpdateMouseSlider()
    {
        mouseText.text = "Sensitivity: " + mouseSlider.value.ToString("F1");
    }
}
