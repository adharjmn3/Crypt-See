using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// scriptable object for items
[CreateAssetMenu(fileName = "Item", menuName = "Item")]

public class Item : ScriptableObject{
    public string name;
    public string description;
    public Sprite icon;
    public Sprite sprite;
}
