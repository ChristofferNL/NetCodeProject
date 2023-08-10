using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor;
using UnityEngine;

public class TestLobby : MonoBehaviour
{
    const float HEART_BEAT_INTERVAL_SECONDS = 15;
    Lobby hostLobby;
    float heartBeatTimer = 0;
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed in with PlayerID:{AuthenticationService.Instance.PlayerId}");
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

	private void Update()
	{
        LobbyHeartbeatTimer();
	}

	public async void CreateLobby()
    {
        try
        {
            string lobbyName = "testLobby";
            int maxPlayers = 2;
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);

            hostLobby = lobby;

            Debug.Log($"Created lobby! ID:{lobby.Id} Name:{lobby.Name} Players:{lobby.MaxPlayers}");

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
                Debug.Log($"{lobby.Name} {lobby.MaxPlayers}");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    async void LobbyHeartbeatTimer()
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
