using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;
using UnityEngine.UI;

public class MenuScript : NetworkBehaviour
{
    [Header("Canvas panels")]
    [SerializeField] private GameObject mainLobbyPanel;
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject gameMenuPanel;
    [SerializeField] private GameObject deadPanel;
    [SerializeField] private GameObject adminPanel;

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


    #region Online behaviours

    public void Host()
    {
        networkManager.StartHost();
        SwitchPanels(lobbyPanelActive: false, gameUIPanelActive: true); //escondemos los menús, mostramos la UI 
    }

    public void SaveIP(string ip) => networkManager.networkAddress = ip;

    public void Join()
    {
        networkManager.StartClient();
        SwitchPanels(lobbyPanelActive: false, gameUIPanelActive: true); //escondemos los menús, mostramos la UI 
    }

    public void Stop()
    {
        switch (networkManager.mode)
        {
            case NetworkManagerMode.Host:
                networkManager.StopHost();
                break;
            case NetworkManagerMode.ClientOnly:
                networkManager.StopClient();
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

    #endregion

    void Start()
    {
        networkManager = this.GetComponent<CustomNetworkManager>();
        menuOpened = false;
        countdownInfoText = countdownInfoObject.GetComponent<Text>();

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
    private void SwitchPanels(bool lobbyPanelActive = true, bool gameUIPanelActive = false, bool gameMenuPanelActive = false, bool deadPanelActive = false)
    {
        mainLobbyPanel.SetActive(lobbyPanelActive);
        gameUIPanel.SetActive(gameUIPanelActive);
        gameMenuPanel.SetActive(gameUIPanelActive);
        deadPanel.SetActive(deadPanelActive);
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
