using System.Collections.Generic;
using UnityEngine;

// [System.Serializable]
public class Unit : MonoBehaviour {

    public int tileX;
    public int tileY;
    public TileMap map;
    public bool turnTaken = false;
    public List<Node> currentPath = null;
    public List<Node> selectableTiles = null;
    public List<Node> attackableTiles = null;

    public int hp = 24;
    public int strength = 12;
    public int defense = 11;
    public int movementSpeed = 3;
    public int attackRange = 1;
    public bool ally = true;
}
