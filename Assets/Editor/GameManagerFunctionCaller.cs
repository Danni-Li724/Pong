using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NetworkGameManager))]
public class GameManagerFunctionCaller : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        NetworkGameManager networkGameManager = (NetworkGameManager)target;
        // another script, maybe visual events? need to attach on same GO
        VisualEventsManager visualEventsManager = networkGameManager.GetComponent<VisualEventsManager>();
        
        EditorGUILayout.Space(); // space between functions
        EditorGUILayout.LabelField("Custom Actions", EditorStyles.boldLabel);
        if (networkGameManager != null)
        {
            if (GUILayout.Button("Spaceship Mode"))
            {
                networkGameManager.ActivateSpaceshipModeFromEditor();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Game End Varieties", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Player 1 Wins"))
            {
                networkGameManager.HandleGameEnd(1);
            }
            if (GUILayout.Button("Player 2 Wins"))
            {
                networkGameManager.HandleGameEnd(2);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Player 3 Wins"))
            {
                networkGameManager.HandleGameEnd(3);
            }
            if (GUILayout.Button("Player 4 Wins"))
            {
                networkGameManager.HandleGameEnd(4);
            }
            EditorGUILayout.EndHorizontal();
        }
        if (visualEventsManager != null)
        {
            if (GUILayout.Button("function name"))
            {
                // visual events function
            }
        }

    }
    
}
