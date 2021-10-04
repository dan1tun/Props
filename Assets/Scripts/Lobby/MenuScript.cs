using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MenuScript : MonoBehaviour
{
    [SerializeField] private GameObject mainLobbyPanel, gameUIPanel, gameMenuPanel;
    [HideInInspector] public bool menuOpened;

    private CustomNetworkManager networkManager;


    #region Online behaviours

    public void Host()
    {
        networkManager.StartHost();
        SwitchPanels(lobbyPanelActive: false); //escondemos los menús
    }

    public void SaveIP(string ip) => networkManager.networkAddress = ip;

    public void Join()
    {
        networkManager.StartClient();
        SwitchPanels(lobbyPanelActive: false); //escondemos los menús
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
    #endregion

    void Start()
    {
        networkManager = this.GetComponent<CustomNetworkManager>();
        menuOpened = false;

        // inicializamos la vista
        SwitchPanels();
    }

    void Update()
    {
        //si estamos en pausa, mostramos el menú
        gameUIPanel.SetActive(menuOpened);
    }

    /// <summary>
    /// Activa o desactiva los paneles de la interfaz
    /// </summary>
    /// <param name="lobbyActive">Activar o desactivar Panel_MainLobby</param>
    /// <param name="gameUIActive">Activar o desactivar Panel_GameUI</param>
    /// <param name="gameMenuActive">Activar o desactivar Panel_GameMenu</param>
    private void SwitchPanels(bool lobbyPanelActive = true, bool gameUIPanelActive = false, bool gameMenuPanelActive = false)
    {
        mainLobbyPanel.SetActive(lobbyPanelActive);
        gameUIPanel.SetActive(gameUIPanelActive);
        gameMenuPanel.SetActive(gameUIPanelActive);
    }
}
