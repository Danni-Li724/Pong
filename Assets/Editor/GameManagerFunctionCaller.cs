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
