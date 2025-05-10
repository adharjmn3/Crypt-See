using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionHandler : MonoBehaviour
{
    public enum PositionMode{
        Local,
        World
    }

    [SerializeField] private PositionMode positionMode = PositionMode.Local;

    public Vector3 GetPosition(){
        return positionMode == PositionMode.Local ? transform.localPosition : transform.position;
    }

    public void SetPosition(Vector3 pos){
        if(positionMode == PositionMode.Local){
            transform.localPosition = pos;
        }
        else if(positionMode == PositionMode.World){
            transform.position = pos;
        }
    }

    public void SetMode(PositionMode mode){
        positionMode = mode;
    }

    public PositionMode GetMode(){
        return positionMode;
    }
}
