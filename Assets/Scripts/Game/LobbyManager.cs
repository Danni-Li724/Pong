using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using System.Linq;
using Unity.Networking.Transport;

public class LobbyManager : NetworkBehaviour
{
   private List<PlayerLobbyState> connectedPlayers = new List<PlayerLobbyState>();

   public override void OnNetworkSpawn()
   {
      if (IsServer)
      {
         NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
         NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
      }
   }
   
   public override void OnNetworkDespawn()
   {
      if (IsServer)
      {
         NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
         NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
      }
   }

   private void OnClientConnected(ulong clientId)
   {
      
   }
   private void OnClientDisconnected(ulong clientId)
   {
   }

   private IEnumerator WaitForPlayersReady(ulong clientId)
   {
      yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null);
      PlayerLobbyState player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerLobbyState>();
      connectedPlayers.Add(player);
      player.isReady.OnValueChanged += CheckAllPlayersReady;
   }

   private void CheckAllPlayersReady(bool previous, bool current)
   {
      if (connectedPlayers.All(p => p.isReady.Value))
      {
         StartGame();
      }
   }

   private void StartGame()
   {
      Debug.Log("starting game for everyone");
      StartGameClientRpc();
   }

   [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
   private void StartGameClientRpc()
   {
      // start game logic (sync ui, ball etc)
   }
}
