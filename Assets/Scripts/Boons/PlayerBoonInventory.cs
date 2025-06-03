using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerBoonInventory : NetworkBehaviour
{
     //public NetworkList<string> activeBoonNames = new(); 
     public List<string> activeBoonNames = new();
     public static PlayerBoonInventory Instance { get; private set; }
     private Dictionary<ulong, List<string> >playerBoons = new();
       public void AddBoonToPlayer(ulong clientId, string boonId)
       {
           //if (!IsServer) return
               if(!playerBoons.ContainsKey(clientId))
               playerBoons[clientId] = new List<string>();
               playerBoons[clientId].Add(boonId);
           //activeBoonNames.Add(boonName);
       }
       public List<string> GetBoonsForPlayer(ulong clientId)
       {
           return playerBoons.TryGetValue(clientId, out var list) ? list : new List<string>();
       }
}
