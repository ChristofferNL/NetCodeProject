using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestLobby : MonoBehaviour
{
    const float HEART_BEAT_INTERVAL_SECONDS = 15;
    const float POLL_UPDATE_INTERVAL_SECONDS = 1.1f;

    Lobby hostLobby;
    Lobby joinedLobby;
    float heartBeatTimer = 0;
    float pollUpdateBeatTimer = 0;
    [SerializeField] TMP_InputField inputField;
    string playerName;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed in with PlayerID:{AuthenticationService.Instance.PlayerId}");
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        playerName = $"TestSubject {Random.Range(10, 1000)}";
        Debug.Log(playerName);
    }

	private void Update()
	{
        LobbyHeartbeatTimer();
        LobbyPollForUpdates();
    }

	public async void CreateLobby()
    {
        try
        {
            string lobbyName = "testLobby";
            int maxPlayers = 2;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false, // creates a 6 digit code used to join the lobby if true
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "DefaultGameMode") }
                }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;
            joinedLobby = hostLobby;

            Debug.Log($"Created lobby! ID:{lobby.Id} Name:{lobby.Name} Players:{lobby.MaxPlayers} LobbyCode:{lobby.LobbyCode}");
            PrintPlayers(hostLobby);
        } 
        catch (LobbyServiceException e) 
        { 
            Debug.LogError(e); 
        }
    }

    public async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions 
            {
                Count = 10,
                Filters = new List<QueryFilter> 
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            Debug.Log($"Lobbies found: {queryResponse.Results.Count}");

            foreach (var lobby in queryResponse.Results)
            {
                Debug.Log($"{lobby.Name} {lobby.MaxPlayers} {lobby.Data["GameMode"].Value}");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            joinedLobby = lobby;
            Debug.Log($"Joined Lobby With Code: {lobbyCode}");
            PrintPlayers(lobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions quickJoinLobbyOptions = new QuickJoinLobbyOptions
            {
                Player = GetPlayer()
            };
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(quickJoinLobbyOptions);
            joinedLobby = lobby;
            Debug.Log($"Player joined by QuickJoin");
            PrintPlayers(lobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public void PrintPlayers()
    {
        PrintPlayers(joinedLobby);
    }

    void PrintPlayers(Lobby lobby)
    {
        Debug.Log($"Players in Lobby {lobby.Name} {lobby.Data["GameMode"].Value}");
        foreach (var player in lobby.Players)
        {
            Debug.Log($"{player.Id} {player.Data["PlayerName"].Value}");
        }
    }

    Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
                }
        };
    }

    async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode) } 
                }
            });

            joinedLobby = hostLobby;

            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            playerName = newPlayerName;
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
                }
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void KickPlayer()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[1].Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    async void LobbyPollForUpdates()
    {
        if (joinedLobby == null) return;
        pollUpdateBeatTimer += Time.deltaTime;
        if (pollUpdateBeatTimer > POLL_UPDATE_INTERVAL_SECONDS)
        {
            pollUpdateBeatTimer = 0;
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            joinedLobby = lobby;
        }
    }

    async void LobbyHeartbeatTimer() // keeps the lobby active
    {
        if (hostLobby == null) return;
        heartBeatTimer += Time.deltaTime;
        if (heartBeatTimer > HEART_BEAT_INTERVAL_SECONDS)
        {
            heartBeatTimer = 0;
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        }
    }
}
