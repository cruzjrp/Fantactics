using System.Collections.Generic;
using UnityEngine;

// [System.Serializable]
public class Unit : MonoBehaviour {

    public int tileX;
    public int tileY;
    public TileMap map;
    public int movementSpeed = 2;
    public bool turnTaken = false;
    public List<Node> currentPath = null;
    public List<Node> selectableTiles = null;
}
