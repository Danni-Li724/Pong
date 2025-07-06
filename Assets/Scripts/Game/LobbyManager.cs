using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using System.Linq;
using Unity.Networking.Transport;
/// <summary>
/// This class is not in use.
/// Old manager with methods where I checked for player readiness and spawed ready buttons as soon as a player connects/is spawned
/// Now, to reinforce a better order when initializing the game, I only spawn ready button and check for readiness when all players have connected
/// </summary>
public class LobbyManager : NetworkBehaviour
{
   //private List<PlayerLobbyState> connectedPlayers = new List<PlayerLobbyState>();

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
      // StartCoroutine(WaitForPlayersReady(clientId));
   }
   private void OnClientDisconnected(ulong clientId)
   {
   }
   
   // private IEnumerator WaitForPlayersReady(ulong clientId)
   // {
   //    yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null);
   //    PlayerLobbyState player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerLobbyState>();
   //    connectedPlayers.Add(player);
   //    player.isReady.OnValueChanged += CheckAllPlayersReady;
   // }
   //
   // private void CheckAllPlayersReady(bool previous, bool current)
   // {
   //    if (connectedPlayers.All(p => p.isReady.Value))
   //    {
   //       StartGame();
   //    }
   // }
   
   private void StartGame()
   {
      Debug.Log("starting game for everyone");
      //StartGameClientRpc();
   }
   
   // [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
   // private void StartGameClientRpc()
   // {
   //    
   // }
}
