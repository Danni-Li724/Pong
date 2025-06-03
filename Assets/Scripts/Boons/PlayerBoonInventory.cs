using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBoonInventory : NetworkBehaviour
{
     public List<string> activeBoonNames = new(); // IDs
   
       public void AddBoon(string boonName)
       {
           if (!IsServer) return;
           activeBoonNames.Add(boonName);
       }
}
