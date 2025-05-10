using UnityEngine;

public class TrainingManager : MonoBehaviour
{
    public enum PositionMode{
        Local,
        World
    }

    [Header("Gameobject Reference")]
    [SerializeField] public GameObject agentGameobject;
    [SerializeField] public GameObject playerGameobject;
    [SerializeField] private Transform[] agentSpawnPoints;
    [SerializeField] private Transform[] playerSpawnPoints;

    [Header("Position Mode for Training")]
    [SerializeField] private PositionMode positionMode = PositionMode.Local;

    public Vector3 GetPlayerPosition(){
        return positionMode == PositionMode.Local ? playerGameobject.transform.localPosition : playerGameobject.transform.position;
    }

    public Vector3 GetAgentPosition(){
        return positionMode == PositionMode.Local ? agentGameobject.transform.localPosition : agentGameobject.transform.position;
    }

    public Vector3 GetPosition(Transform obj){
        return positionMode == PositionMode.Local ? obj.transform.localPosition : obj.transform.position;
    }

    public void SetPlayerPosition(Vector3 pos){
        if(positionMode == PositionMode.Local){
            playerGameobject.transform.localPosition = pos;
        }
        else if(positionMode == PositionMode.World){
            playerGameobject.transform.position = pos;
        }
    }

    public void SetAgentPosition(Vector3 pos){
        if(positionMode == PositionMode.Local){
            agentGameobject.transform.localPosition = pos;
        }
        else if(positionMode == PositionMode.World){
            agentGameobject.transform.position = pos;
        }
    }

    public void SetPosition(Transform obj, Vector3 pos){
        if(positionMode == PositionMode.Local){
            obj.transform.localPosition = pos;
        }
        else if(positionMode == PositionMode.World){
            obj.transform.position = pos;
        }
    }

    public void SetMode(PositionMode mode){
        positionMode = mode;
    }

    public PositionMode GetMode(){
        return positionMode;
    }

    public void ResetTrainingPosition(){
        if(agentSpawnPoints == null || playerSpawnPoints == null){
            return;
        }

        Transform agentPos = agentGameobject.transform;
        Transform playerPos = playerGameobject.transform;

        int index = Random.Range(0, agentSpawnPoints.Length);
        Vector3 newAgentPos = GetPosition(agentSpawnPoints[index].transform);

        index = Random.Range(0, playerSpawnPoints.Length);
        Vector3 newPlayerPos = GetPosition(playerSpawnPoints[index].transform);

        SetPosition(agentPos, newAgentPos);
        SetPosition(playerPos, newPlayerPos);
    }
}
