using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

/// <summary>
/// Handles boon selection, syncing, and applying effects across all players.
/// This class is owned by the server, referenced by BoonButton and PlayerInventorySlot.
/// It manages both server-side and client-side inventories and UI buttons.
/// </summary>
public class BoonManager : NetworkBehaviour
{
   [Header("Boon Setup")]
    [SerializeField] private List<BoonEffect> availableBoons;
    [SerializeField] private Transform boonButtonContainer;
    [SerializeField] private GameObject boonButtonPrefab;
    private List<ulong> expectedPlayerIds = new List<ulong>();
    // a reference of active boon buttons on screen
    private Dictionary<BoonType, GameObject> activeBoonButtons = new Dictionary<BoonType, GameObject>();
    // NetVar to track how many players in boon selection to fix early start bug..
    private NetworkVariable<int> activePlayersInBoonSelection = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [Header("Player Inventories")]
    [SerializeField] private Transform[] playerInventorySlots = new Transform[4];
    [SerializeField] private GameObject nominationButtonPrefab;

    public static BoonManager Instance { get; private set; }
    
    // Server-side data - fixed to using boonTypes instead of strings because netcode has issues sending string[] in rpc :(...
    // This is a server side inventory that tracks which client owns which boons
    private Dictionary<ulong, List<BoonType>> playerInventories = new Dictionary<ulong, List<BoonType>>();
    // Client-side data synced from server. It's a client-side cache of inventories which are used for displaying UIs/visuals. 
    private Dictionary<ulong, List<BoonType>> clientPlayerInventories = new Dictionary<ulong, List<BoonType>>();
    
    // Server-side tracking of available boons
    private List<BoonType> availableBoonTypes = new List<BoonType>();
    private bool boonSelectionActive = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // initializing client inventory tracking
        if (IsClient)
        {
            clientPlayerInventories.Clear();
        }
    }
    
    // This is server-side: called by NetworkGameManager when all players are ready
    public void StartBoonSelection()
    {
        if (!IsServer) return;
        
        // Set the list of expected players once
        expectedPlayerIds = NetworkGameManager.Instance.GetAllPlayers()
            .Where(p => p.isConnected)
            .Select(p => p.clientId)
            .ToList();
        
        activePlayersInBoonSelection.Value = expectedPlayerIds.Count; // setting up total count
        Debug.Log($"Starting boon selection with {activePlayersInBoonSelection.Value} active players");

        boonSelectionActive = true;
        playerInventories.Clear();
        
        // Set up available boons on server
        // availableBoonTypes = availableBoons
        //     .OrderBy(x => Random.value) // randomly choose 4 bools from the pool, so that it's different every game
        //     .Take(6) // select available (I added more now:D)
        //     .Select(b => b.type) // project each boonEffect to its boonType enum
        //     .ToList(); // keep as list for later manipulation
        
        // Select all boon types (I made all 6 now!) from the list of available BoonEffects
        availableBoonTypes = availableBoons
            .Select(b => b.type) // get boon type from each boon effect
            .Distinct() // ensuring each type is only included once - all unique boons will be shown
            .OrderBy(x => Random.value) // still randomly shuffle order
            .ToList();
        
        // Sending a clear inventory state to all clients
        SyncAllInventoriesClientRpc(new ulong[0], new BoonType[0], new int[0]);
        
        // send these boons to clients so they display the right buttons
        SpawnBoonButtonsClientRpc(availableBoonTypes.ToArray());
    }
    
    // Client-Rpc: sent by the server to all clients to show buttons for the boons to all clients
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void SpawnBoonButtonsClientRpc(BoonType[] selectedBoons)
    {
        foreach (Transform child in boonButtonContainer)
            Destroy(child.gameObject);
        activeBoonButtons.Clear();

        foreach (BoonType boonType in selectedBoons)
        {
            var boonEffect = availableBoons.Find(b => b.type == boonType);
            if (boonEffect != null)
            {
                GameObject buttonObj = Instantiate(boonButtonPrefab, boonButtonContainer);
                var button = buttonObj.GetComponent<BoonButton>();
                button.Initialize(boonEffect, this);
                // Store using BoonType directly
                activeBoonButtons[boonType] = buttonObj;
            }
        }
    }
    
    // called by BoonButton on client when player clicks the boons
    public void TrySelectBoon(BoonType boonType)
    {
        if (!IsClient) return;
        
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        RequestBoonServerRpc(clientId, boonType);
    }
    
    // Server-Rpc (requesting) sent from client to server to request for a boon when they click on it
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void RequestBoonServerRpc(ulong clientId, BoonType boonType)
    {
        Debug.Log($"Server received boon request from client {clientId} for boon {boonType}");
        
        if (!boonSelectionActive) 
        {
            Debug.Log("Boon selection not active");
            return;
        }
        
        // check if boon is still available in server list
        if (!availableBoonTypes.Contains(boonType)) 
        {
            Debug.Log($"Boon {boonType} not available");
            return;
        }
        
        if (playerInventories.ContainsKey(clientId) && playerInventories[clientId].Count > 0) 
        {
            Debug.Log($"Player {clientId} already has a boon");
            return;
        }
        
        // add boon to player inventory
        if (!playerInventories.ContainsKey(clientId))
            playerInventories[clientId] = new List<BoonType>();
        
        playerInventories[clientId].Add(boonType);
        Debug.Log($"Added boon {boonType} to player {clientId} inventory");
        
        // remove boon from available list
        availableBoonTypes.Remove(boonType);
        
        // remove boon from selection and sync all inventories
        RemoveBoonButtonClientRpc(boonType);
        
        // Add a small delay before syncing to ensure button removal is processed
        StartCoroutine(DelayedSync());
        
        CheckAllPlayersSelected();
    }
    
    private System.Collections.IEnumerator DelayedSync()
    {
        yield return new WaitForSeconds(0.1f); // small delay to ensure proper order; needed to cuz issues
        SyncAllInventoriesToClients();
    }
    
    // This is server only function - syncs full (everyone's) inventories to all clients
    private void SyncAllInventoriesToClients() // syncing all player inventories to all clients
    {
        if (!IsServer) return;
        
        List<ulong> clientIds = new List<ulong>();
        List<BoonType> boonTypes = new List<BoonType>();
        List<int> playerIds = new List<int>();
        
        foreach (var kvp in playerInventories)
        {
            ulong clientId = kvp.Key;
            var playerInfo = NetworkGameManager.Instance.GetPlayerInfo(clientId);
            
            if (playerInfo != null)
            {
                foreach (var boonType in kvp.Value)
                {
                    clientIds.Add(clientId);
                    boonTypes.Add(boonType);
                    playerIds.Add(playerInfo.playerId);
                }
            }
        }
        
        SyncAllInventoriesClientRpc(clientIds.ToArray(), boonTypes.ToArray(), playerIds.ToArray());
    }
    
    // Client Rpc - sent from server to all clients to update visuals on all clients
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void SyncAllInventoriesClientRpc(ulong[] clientIds, BoonType[] boonTypes, int[] playerIds)
    {
        Debug.Log($"Syncing inventories - received {clientIds.Length} boons");
        
        // Clear all inventory slots first
        foreach (var slot in playerInventorySlots)
        {
            if (slot != null)
            {
                var inventorySlot = slot.GetComponent<PlayerInventorySlot>();
                if (inventorySlot != null)
                {
                    inventorySlot.ClearBoon();
                }
            }
        }
        
        // clear client inventory
        clientPlayerInventories.Clear();
        
        // rebuild inventories from synced data x_x
        for (int i = 0; i < clientIds.Length; i++)
        {
            ulong clientId = clientIds[i];
            BoonType boonType = boonTypes[i];
            int playerId = playerIds[i];
            
            // Track client inventory
            if (!clientPlayerInventories.ContainsKey(clientId))
                clientPlayerInventories[clientId] = new List<BoonType>();
            clientPlayerInventories[clientId].Add(boonType);
            
            // Update visual slot
            int slotIndex = playerId - 1;
            if (slotIndex < playerInventorySlots.Length && slotIndex >= 0)
            {
                var inventorySlot = playerInventorySlots[slotIndex].GetComponent<PlayerInventorySlot>();
                if (inventorySlot != null)
                {
                    var boonEffect = availableBoons.Find(b => b.type == boonType);
                    if (boonEffect != null)
                    {
                        inventorySlot.SetBoon(boonEffect, clientId);
                        Debug.Log($"Synced boon {boonEffect.effectName} for player {playerId} (client {clientId})");
                    }
                }
            }
        }
    }
    
    // When a boon has been chosen, remove it from the selection for all players
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void RemoveBoonButtonClientRpc(BoonType boonType)
    {
        Debug.Log($"Removing boon button for {boonType}");
        if (activeBoonButtons.ContainsKey(boonType))
        {
            Destroy(activeBoonButtons[boonType]);
            activeBoonButtons.Remove(boonType);
        }
    }

    // Checks if everyone has selected a boon (server only)
    private void CheckAllPlayersSelected()
    {
        if (!IsServer) return;
        
        // Get the count of players who have selected boons
        int playersWithBoons = playerInventories.Count(kvp => kvp.Value.Count > 0);
        Debug.Log($"Checking all players selected: {playersWithBoons}/{activePlayersInBoonSelection.Value} players have selected boons");
        
        // Check if all active players (not all connected ones now) have selected a boon
        if (playersWithBoons >= activePlayersInBoonSelection.Value)
        {
            Debug.Log("All players have selected boons!");
            boonSelectionActive = false;
            ClearRemainingButtonsClientRpc();
            var gameManager = NetworkGameManager.Instance;
            if (gameManager != null)
            {
                gameManager.OnAllBoonsSelected();
            }
        }
    }
    
    // ensuring that all clients can reliably destroy any leftover boon selection buttons. 
    // Doesn't have to be reliable because even if buttons remain, each player cannot select more than one anyway
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
    private void ClearRemainingButtonsClientRpc()
    {
        foreach (var buttonObj in activeBoonButtons.Values)
        {
            Destroy(buttonObj);
        }
        activeBoonButtons.Clear();
    }
    
    // Called on the client when a boon button is clicked
    public void UseBoon(ulong clientId, BoonType boonType)
    {
        if (!IsClient) return;
        
        // Only allow the owner to use their boon
        if (NetworkManager.Singleton.LocalClientId != clientId) return;
        
        RequestUseBoonServerRpc(clientId, boonType);
    }
    
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void RequestUseBoonServerRpc(ulong clientId, BoonType boonType)
    {
        Debug.Log($"Server received use boon request from client {clientId} for boon {boonType}");

        if (!playerInventories.ContainsKey(clientId) || !playerInventories[clientId].Contains(boonType))
        {
            Debug.Log($"Player {clientId} doesn't have boon {boonType}");
            return;
        }

        var boonEffect = availableBoons.Find(b => b.type == boonType);
        if (boonEffect == null) return;

        // Run game logic effects on server ONLY
        switch (boonEffect.type)
        {
            case BoonType.SpaceshipMode:
                EnableSpaceshipMode(boonEffect.duration);
                break;
            case BoonType.DoubleBall:
                SpawnDoubleBall(boonEffect.duration);
                break;
            case BoonType.BallSpeedBoost:
                ModifyBallSpeed(2f, boonEffect.duration);
                break;
            case BoonType.MusicChange:
                CycleMusicTrack(); // server-side logic
                break;
            case BoonType.PaddleTilt:
                EnablePaddleTilt(clientId, boonEffect.duration);
                break;

        }

        // Remove non-reusable boons
        if (!boonEffect.isReusable)
        {
            playerInventories[clientId].Remove(boonType);
            SyncAllInventoriesToClients();
        }

        var playerInfo = NetworkGameManager.Instance.GetPlayerInfo(clientId);
        if (playerInfo != null)
        {
            ApplyClientSideEffectClientRpc(boonType, clientId, playerInfo.playerId);
        }
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ApplyClientSideEffectClientRpc(BoonType boonType, ulong boonUserId, int boonPlayerId)
    {
        if (NetworkManager.Singleton.LocalClientId != boonUserId) return;

        Debug.Log($"[CLIENT] ApplyClientSideEffect for {boonType}");

        var boonEffect = availableBoons.Find(b => b.type == boonType);
        if (boonEffect == null) return;

        if (boonEffect.type == BoonType.ScoreThief)
        {
            PrepareNomination(boonUserId, boonPlayerId); // pass ID directly
        }
        // NOT applying gameplay logic here like CycleMusic/SpawnBall to avoid doubling up
    }
    
    #region BOON EFFECTS // below are functions for each boon effects. 
    
    // Updated this method to use the boon user's client ID for correct nomination assignment
private void PrepareNomination(ulong boonUserId, int boonPlayerId) 
{
    Debug.Log($"Preparing nomination for local client {NetworkManager.Singleton.LocalClientId}, boon user = {boonUserId}");
    
    if (NetworkManager.Singleton.LocalClientId != boonUserId)
    {
        Debug.Log("Not boon owner, skipping nomination UI");
        return;
    }
    
    int slotIndex = boonPlayerId - 1;
    if (slotIndex < 0 || slotIndex >= playerInventorySlots.Length)
    {
        Debug.LogError($"Invalid slot index: {slotIndex}");
        return;
    }
    
    var slot = playerInventorySlots[slotIndex];
    var nominationParent = slot.Find("Nomination");
    if (nominationParent == null)
    {
        Debug.LogError("Nomination parent not found!");
        return;
    }
    
    // Clear old buttons
    foreach (Transform child in nominationParent)
        Destroy(child.gameObject);
    
    // Find all paddle controllers in the scene (these exist on all clients)
    var allPaddles = FindObjectsOfType<PaddleController>();
    var otherPaddles = allPaddles.Where(p => p.GetPlayerId() != boonPlayerId).ToList();
    
    Debug.Log($"Found {otherPaddles.Count} other paddles for nomination");
    
    // Create nomination buttons for each other player's paddle
    foreach (var paddle in otherPaddles)
    {
        int targetPlayerId = paddle.GetPlayerId();
        ulong targetClientId = paddle.GetComponent<NetworkObject>().OwnerClientId;
        
        GameObject buttonObj = Instantiate(nominationButtonPrefab, nominationParent);
        var text = buttonObj.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = $"Player {targetPlayerId}";
        }
        
        var button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            // Capture both player ID and client ID for this player
            int capturedPlayerId = targetPlayerId;
            ulong capturedClientId = targetClientId;
            
            button.onClick.AddListener(() =>
            {
                Debug.Log($"Nominating player {capturedPlayerId} (clientId: {capturedClientId})");
                TryNominatePlayerForScoreThief(capturedClientId, capturedPlayerId);
                foreach (Transform c in nominationParent) Destroy(c.gameObject);
            });
        }
    }
}

// Updated nomination method to pass both client ID and player ID
private void TryNominatePlayerForScoreThief(ulong targetClientId, int targetPlayerId)
{
    ulong localId = NetworkManager.Singleton.LocalClientId;
    ScoreThiefNominationServerRpc(localId, targetClientId, targetPlayerId);
}

// Updated ServerRpc to accept player ID directly (more reliable than looking it up)
[Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
private void ScoreThiefNominationServerRpc(ulong thiefClientId, ulong victimClientId, int victimPlayerId)
{
    Debug.Log($"Score Thief: Player {thiefClientId} is stealing from player {victimPlayerId} (client {victimClientId})");
    
    ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
    if (scoreManager == null)
    {
        Debug.LogError("ScoreManager not found!");
        return;
    }
    
    var canDeduct = scoreManager.TryDeductPointsByPlayerId(victimPlayerId, 3);
    if (canDeduct)
    {
        Debug.Log($"Successfully deducted 3 points from player {victimPlayerId}");
    
        // Remove ScoreThief from thief's inventory
        if (playerInventories.TryGetValue(thiefClientId, out var boons))
        {
            boons.Remove(BoonType.ScoreThief);
            SyncAllInventoriesToClients();
        }
        
        // Notify all clients about the score theft
        NotifyScoreTheftClientRpc(thiefClientId, victimClientId, victimPlayerId);
    }
    else
    {
        Debug.LogWarning($"Score Thief failed - not enough points for player {victimPlayerId}");
        // notify thief that the theft failed
        NotifyScoreTheftFailedClientRpc(thiefClientId);
    }
}

// Notify clients about successful score theft for visual feedback
[Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
private void NotifyScoreTheftClientRpc(ulong thiefClientId, ulong victimClientId, int victimPlayerId)
{
    Debug.Log($"Score theft notification: Thief {thiefClientId} stole from player {victimPlayerId}");
    // Add visual/audio feedback here if desired
}

// Notify thief if the theft failed
[Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
private void NotifyScoreTheftFailedClientRpc(ulong thiefClientId)
{
    if (NetworkManager.Singleton.LocalClientId == thiefClientId)
    {
        Debug.Log("Score theft failed - target doesn't have enough points");
        // Show UI feedback to the player
    }
}
    
    private void CycleMusicTrack()
    {
        if (!IsServer) return;
        var audioManager = AudioManager.instance;
        if (audioManager != null)
        {
            audioManager.CycleMusicServerRpc();
        }
    }
    
    private void EnableSpaceshipMode(float duration)
    {
        // var paddles = FindObjectsOfType<PaddleController>();
        // foreach (var paddle in paddles)
        // {
        //     paddle.EnableSpaceshipMode(duration);
        // }
        NetworkGameManager.Instance.StartSpaceshipMode();
    }
    
    private void SpawnDoubleBall(float duration)
    {
        var gameManager = FindObjectOfType<NetworkGameManager>();
        if (gameManager != null)
        {
            gameManager.SpawnAdditionalBall(duration);
        }
    }
    
    private void EnablePaddleTilt(ulong clientId, float duration)
    {
        if (!IsServer) return;
    
        Debug.Log($"[BoonManager] Enabling paddle tilt for client {clientId} with duration {duration}");
    
        // enable tilt for the specific player's paddle
        EnablePaddleTiltClientRpc(clientId, duration);
    }
    
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void EnablePaddleTiltClientRpc(ulong targetClientId, float duration)
    {
        Debug.Log($"[BoonManager] EnablePaddleTiltClientRpc called for client {targetClientId}");
    
        // Find the paddle controller for the target client
        var paddles = FindObjectsOfType<PaddleController>();
        foreach (var paddle in paddles)
        {
            var networkObject = paddle.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.OwnerClientId == targetClientId)
            {
                paddle.ActivateTilt(duration);
                Debug.Log($"[BoonManager] Activated tilt for paddle owned by client {targetClientId}");
                break;
            }
        }
    }
    
    private void ModifyBallSpeed(float multiplier, float duration)
    {
        var balls = FindObjectsOfType<BallPhysics>();
        foreach (var ball in balls)
        {
            ball.ModifySpeed(multiplier, duration);
        }
    }
    public List<BoonType> GetPlayerBoons(ulong clientId) // debug/helper function
    {
        if (IsServer)
        {
            return playerInventories.ContainsKey(clientId) ? playerInventories[clientId] : new List<BoonType>();
        }
        else
        {
            return clientPlayerInventories.ContainsKey(clientId) ? clientPlayerInventories[clientId] : new List<BoonType>();
        }
    }
    #endregion
}
