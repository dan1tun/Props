using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Audio;
using System;
using System.Net;
using UnityEngine.Networking;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class MenuScript : MonoBehaviour
{
    [Header("API")]
    public string apiUrl = "https://addresshelper.azurewebsites.net";
    private string key;
    private static readonly HttpClient client = new HttpClient();

    [Header("Canvas panels")]
    [SerializeField] private GameObject mainLobbyPanel;
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject gameMenuPanel;
    [SerializeField] private GameObject deadPanel;
    [SerializeField] private GameObject adminPanel;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Configuration items")]
    public AudioMixer audioMixer;
    public Dropdown resolutionDropdown;
    public Dropdown qualityDropdown;
    public Dropdown textureDropdown;
    public Dropdown aaDropdown;
    public Slider volumeSlider;
    float currentVolume;
    Resolution[] resolutions;

    [Header("Game UI")]
    [SerializeField] private GameObject cooldownsPanel;
    [SerializeField] private GameObject countdownInfoObject;
    private Text countdownInfoText;
    private float countdown;
    private Enums.RoundType currentRoundType;

    [Header("Cooldowns")]
    [SerializeField] private GameObject cooldownTemplate;

    [Header("Cooldowns Items")]
    [SerializeField] private Sprite blankSprite;
    [SerializeField] private Sprite afkSprite, meleeSprite, transformSprite;

    [HideInInspector] public bool menuOpened;
    [HideInInspector] public PlayerController playerController;

    private CustomNetworkManager networkManager;
    private Dictionary<Enums.CooldownType, GameObject> cooldowns = new Dictionary<Enums.CooldownType, GameObject>();

    #region api calls
    string ApiGet(string extra = "", string url = "")
    {
        if (String.IsNullOrEmpty(url))
            url = apiUrl;

        var response = client.GetAsync(url + extra).Result;
        return response.Content.ReadAsStringAsync().Result;
    }
    string ApiPost(string extra)
    {
        var response = client.PostAsync(apiUrl + extra, null).Result;
        return response.Content.ReadAsStringAsync().Result;
    }
    string ApiDelete(string extra)
    {
        var response = client.DeleteAsync(apiUrl + extra).Result;
        return response.Content.ReadAsStringAsync().Result;
    }
    private string getIp()
    {
        string ip = ApiGet(url: "http://checkip.dyndns.org");
        Debug.Log("IP: " + ip);
        ip = ip.Substring(ip.IndexOf(":") + 1);
        ip = ip.Substring(0, ip.IndexOf("<"));
        return ip.Trim();
    }
    #endregion
    #region Online behaviours

    public void Host()
    {
        networkManager.StartHost();
        SwitchPanels(lobbyPanelActive: false, gameUIPanelActive: true); //escondemos los menús, mostramos la UI
        //registramos en el AddressManager
        key = ApiPost($"?ipAddress={getIp()}");
        Debug.Log("Our key: " + key);
    }

    public void SaveIP(string input) => key = input;

    public void Join()
    {
        networkManager.networkAddress = ApiGet($"?key={key}");
        SwitchPanels(lobbyPanelActive: false, gameUIPanelActive: true); //escondemos los menús, mostramos la UI 
        networkManager.StartClient();
        Debug.Log("IP:" + key);
    }

    public void Stop()
    {
        Debug.Log("Stop called. Nº of players: " + networkManager.numPlayers);
        switch (networkManager.mode)
        {
            case NetworkManagerMode.Host:
                ApiDelete($"?key={key}");
                networkManager.StopHost();
                break;
            case NetworkManagerMode.ClientOnly:
                networkManager.StopClient();
                if (networkManager.numPlayers <= 1)
                    ApiDelete($"?key={key}");
                break;
        }
    }

    public void LeaveFromDead()
    {
        Stop();
        deadPanel.SetActive(false);
    }

    public void StartGame()
    {
        this.adminPanel.SetActive(false);
        playerController.StartGame();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    #endregion

    #region Options
    public void ShowOptions()
    {
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        resolutions = Screen.resolutions;
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " +
                     resolutions[i].height;
            options.Add(option);
            if (resolutions[i].width == Screen.currentResolution.width
                  && resolutions[i].height == Screen.currentResolution.height)
                currentResolutionIndex = i;
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.RefreshShownValue();
        LoadSettings(currentResolutionIndex);

        // Swap panels
        SwitchPanels(lobbyPanelActive: false, optionsPanelActive: true);
    }
    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("Volume", volume);
        currentVolume = volume;
    }
    public void SetScreenMode(int index)
    {
        switch (index)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
                break;
            case 3:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            default:
                break;
        }
    }
    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width,
                  resolution.height, Screen.fullScreen);
    }
    public void SetTextureQuality(int textureIndex)
    {
        QualitySettings.masterTextureLimit = textureIndex;
        qualityDropdown.value = 6;
    }
    public void SetAntiAliasing(int aaIndex)
    {
        QualitySettings.antiAliasing = aaIndex;
        qualityDropdown.value = 6;
    }
    public void SetQuality(int qualityIndex)
    {
        if (qualityIndex != 6) // if the user is not using 
                               //any of the presets
            QualitySettings.SetQualityLevel(qualityIndex);
        switch (qualityIndex)
        {
            case 0: // quality level - very low
                textureDropdown.value = 3;
                aaDropdown.value = 0;
                break;
            case 1: // quality level - low
                textureDropdown.value = 2;
                aaDropdown.value = 0;
                break;
            case 2: // quality level - medium
                textureDropdown.value = 1;
                aaDropdown.value = 0;
                break;
            case 3: // quality level - high
                textureDropdown.value = 0;
                aaDropdown.value = 0;
                break;
            case 4: // quality level - very high
                textureDropdown.value = 0;
                aaDropdown.value = 1;
                break;
            case 5: // quality level - ultra
                textureDropdown.value = 0;
                aaDropdown.value = 2;
                break;
        }

        qualityDropdown.value = qualityIndex;
    }
    public void SaveSettings()
    {
        PlayerPrefs.SetInt("QualitySettingPreference",
                   qualityDropdown.value);
        PlayerPrefs.SetInt("ResolutionPreference",
                   resolutionDropdown.value);
        PlayerPrefs.SetInt("TextureQualityPreference",
                   textureDropdown.value);
        PlayerPrefs.SetInt("AntiAliasingPreference",
                   aaDropdown.value);
        PlayerPrefs.SetInt("FullscreenPreference",
                   Convert.ToInt32(Screen.fullScreen));
        PlayerPrefs.SetFloat("VolumePreference",
                   currentVolume);

        // Swap panels
        SwitchPanels(lobbyPanelActive: true, optionsPanelActive: false);
    }
    public void LoadSettings(int currentResolutionIndex)
    {
        if (PlayerPrefs.HasKey("QualitySettingPreference"))
            qualityDropdown.value =
                         PlayerPrefs.GetInt("QualitySettingPreference");
        else
            qualityDropdown.value = 3;
        if (PlayerPrefs.HasKey("ResolutionPreference"))
            resolutionDropdown.value =
                         PlayerPrefs.GetInt("ResolutionPreference");
        else
            resolutionDropdown.value = currentResolutionIndex;
        if (PlayerPrefs.HasKey("TextureQualityPreference"))
            textureDropdown.value =
                         PlayerPrefs.GetInt("TextureQualityPreference");
        else
            textureDropdown.value = 0;
        if (PlayerPrefs.HasKey("AntiAliasingPreference"))
            aaDropdown.value =
                         PlayerPrefs.GetInt("AntiAliasingPreference");
        else
            aaDropdown.value = 1;
        if (PlayerPrefs.HasKey("FullscreenPreference"))
            Screen.fullScreen =
            Convert.ToBoolean(PlayerPrefs.GetInt("FullscreenPreference"));
        else
            Screen.fullScreen = true;
        if (PlayerPrefs.HasKey("VolumePreference"))
            volumeSlider.value =
                        PlayerPrefs.GetFloat("VolumePreference");
        else
            volumeSlider.value =
                        PlayerPrefs.GetFloat("VolumePreference");
    }
    #endregion

    void Start()
    {
        networkManager = this.GetComponent<CustomNetworkManager>();
        menuOpened = false;
        countdownInfoText = countdownInfoObject.GetComponent<Text>();
        client.DefaultRequestHeaders.ExpectContinue = false;


        // inicializamos la vista
        SwitchPanels();
    }

    void Update()
    {
        //si estamos en pausa, mostramos el menú
        gameMenuPanel.SetActive(menuOpened);


        //mostramos la información de la partida (tipo y tiempo, ej: preronda - 50s)
        if (countdown > 0)
        {
            string text = "";
            switch (currentRoundType)
            {
                case Enums.RoundType.Starting:
                    text = "Starting in... " + (int)countdown;
                    break;
                case Enums.RoundType.Preround:
                    text = "Hunt begins in... " + (int)countdown;
                    break;
                case Enums.RoundType.HideAndSeek:
                    text = "Survival ends in... " + (int)countdown;
                    break;
                case Enums.RoundType.Flight:
                    text = "Flight before... " + (int)countdown;
                    break;
                default:
                    text = "Game didn't start yet";
                    break;
            }
            this.countdownInfoText.text = text;
            countdown -= Time.deltaTime;

            if (countdown <= 0)
            {
                switch (currentRoundType)
                {
                    case Enums.RoundType.Starting:
                        playerController.StartPreround();
                        break;
                    case Enums.RoundType.Preround:
                        playerController.StartRound();
                        break;
                    case Enums.RoundType.HideAndSeek:
                        playerController.StartFlight();
                        break;
                    case Enums.RoundType.Flight:
                        playerController.EndGame();
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public void NewPhase(Enums.RoundType roundType, float time)
    {
        this.currentRoundType = roundType;
        this.countdown = time;
    }

    /// <summary>
    /// Activa o desactiva los paneles de la interfaz
    /// </summary>
    /// <param name="lobbyActive">Activar o desactivar Panel_MainLobby</param>
    /// <param name="gameUIActive">Activar o desactivar Panel_GameUI</param>
    /// <param name="gameMenuActive">Activar o desactivar Panel_GameMenu</param>
    public void SwitchPanels(
        bool lobbyPanelActive = true,
        bool gameUIPanelActive = false,
        bool gameMenuPanelActive = false,
        bool deadPanelActive = false,
        bool adminPanelActive = false,
        bool endPanelActive = false,
        bool optionsPanelActive = false)
    {
        mainLobbyPanel.SetActive(lobbyPanelActive);
        gameUIPanel.SetActive(gameUIPanelActive);
        gameMenuPanel.SetActive(gameMenuPanelActive);
        deadPanel.SetActive(deadPanelActive);
        adminPanel.SetActive(adminPanelActive);
        endPanel.SetActive(endPanelActive);
        optionsPanel.SetActive(optionsPanelActive);
    }

    /// <summary>
    /// Shows the dead screen (enables the panel)
    /// </summary>
    public void ShowDeadScreen()
    {
        this.deadPanel.SetActive(true);
    }

    /// <summary>
    /// Shows the admin screen (enables the panel)
    /// </summary>
    public void ShowAdminScreen()
    {
        this.adminPanel.SetActive(true);
    }

    /// <summary>
    /// Creates a new cooldown
    /// </summary>
    /// <param name="cooldownType">Type (afk, transform, melee attack...)</param>
    /// <param name="time">Cooldown time</param>
    public void NewCooldown(Enums.CooldownType cooldownType, float time)
    {
        // If it exists, then update it (update or insert)
        if (cooldowns.TryGetValue(cooldownType, out GameObject value))
        {
            UpdateCooldown(cooldownType, time);
            return;
        }

        // Getting the image for this cooldown
        Sprite image;
        switch (cooldownType)
        {
            case Enums.CooldownType.AFK:
                image = afkSprite;
                break;
            case Enums.CooldownType.Melee:
                image = meleeSprite;
                break;
            case Enums.CooldownType.Transform:
                image = transformSprite;
                break;
            default:
                image = blankSprite;
                Debug.LogError($"No sprite for this cooldown: {cooldownType.ToString()}");
                break;
        }

        // Getting the position
        int x = 125 + 225 * cooldowns.Count;
        Vector3 position = new Vector3(x, 100, 0);

        GameObject newCooldown = Instantiate(cooldownTemplate, cooldownsPanel.transform);
        newCooldown.GetComponent<CooldownItem>().Initialize(image, time, position, this, cooldownType);

        cooldowns.Add(cooldownType, newCooldown);
    }

    /// <summary>
    /// Updates the cooldown to the given type
    /// </summary>
    /// <param name="cooldownType">Type of cooldown to update</param>
    /// <param name="newCooldown">New timer</param>
    public void UpdateCooldown(Enums.CooldownType cooldownType, float newCooldown)
    {
        cooldowns[cooldownType].GetComponent<CooldownItem>().UpdateCooldown(newCooldown);
    }

    /// <summary>
    /// Removes the cooldown from the UI
    /// </summary>
    /// <param name="cooldownType">Cooldown type</param>
    public void RemoveCooldown(Enums.CooldownType cooldownType)
    {
        Destroy(cooldowns[cooldownType]);
        cooldowns.Remove(cooldownType);

        // updates the positions
        int i = 0;
        foreach (Enums.CooldownType key in cooldowns.Keys)
        {
            int x = 125 + 225 * i++;
            Vector3 position = new Vector3(x, 100, 0);

            cooldowns[key].GetComponent<CooldownItem>().UpdateCooldown(position);

        }
    }
}
