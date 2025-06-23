using UnityEngine;

public class GoalController : MonoBehaviour
{
    [SerializeField]
    private int goalForPlayerId;

    public int GetGoalForPlayerId()
    {
        return goalForPlayerId;
    }
    
    public void SetGoalForPlayerId(int id)
    {
        goalForPlayerId = id;
    }
}
