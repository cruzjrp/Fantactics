using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour {

    public Unit[] allyUnits;
    public Unit[] enemyUnits;
    public Camera mainCamera;
    public TileMap map;
    public GameObject tileHighlighter;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {

        if (CheckTurnEnded(allyUnits))
        {
            StartTurn();
        }

        int tileLayer = 1 << 9;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit tileHit;

        if (Physics.Raycast(ray, out tileHit, 100f, tileLayer))
        {
            // Get the int versions of the X and Y values of the raycast
            int tileHitX = (int)tileHit.transform.position.x;
            int tileHitY = (int)tileHit.transform.position.y;

            // Changes the position of the tile highlighter to where the mouse is 
            tileHighlighter.transform.position = new Vector3(tileHit.transform.position.x, tileHit.transform.position.y, -0.75f);

            // Changes the color of the tile highlighter depending on the tile type. If the tile is walkable, it is blue and white.
            // If the tile is not walkable, it is red
            if (!map.tileTypes[map.tiles[tileHitX, tileHitY]].isWalkable)
            {
                tileHighlighter.GetComponentInChildren<Renderer>().material.color = Color.red;
            }
            else
            {
                tileHighlighter.GetComponentInChildren<Renderer>().material.color = Color.white;
            }

            // Generates the movement range for a unit on mouse hover when a unit is not already selected
            if (!map.unitSelected)
            {
                //if (CheckOccupiedTile(tileHitX, tileHitY) && GetUnitOnMouse(tileHitX, tileHitY) != map.selectedUnit)
                if (CheckOccupiedTile(tileHitX, tileHitY))
                {
                    Unit unitOnTile = GetUnitOnMouse(tileHitX, tileHitY);

                    if (!unitOnTile.turnTaken)
                    {
                        map.GenerateSelectableTiles(unitOnTile);
                        //map.GenerateAttackableTiles(unitOnTile);
                    }
                }
                else if (!CheckOccupiedTile(tileHitX, tileHitY))
                {
                    map.RemoveHighlightedTiles();
                }
            }
            else if (map.unitSelected)
            {
                // Manage the path of the selected unit
                map.ManualSelectedUnitPath(tileHitX, tileHitY);
            }

            // Selects a unit if possible if a unit is not already selected. If a unit is already selected, it moves the unit instead.
            if (Input.GetMouseButtonUp(0))
            {
                if (!map.unitSelected && CheckOccupiedTile(tileHitX,tileHitY) && !GetUnitOnMouse(tileHitX, tileHitY).turnTaken)
                {
                    SetSelectedUnit(tileHitX, tileHitY);
                    if (map.unitSelected)
                    {
                        map.GenerateSelectableTiles();
                    }
                }

                else if (map.unitSelected)
                {
                    if (!map.selectedUnit.hasMoved)
                    {
                        // Check if the clicked tile you want to move to is within the selectable tiles of the selected unit and has no unit already on it
                        if (map.selectedUnit.selectableTiles.Contains(map.GetNode(tileHitX, tileHitY)) && GetUnitOnMouse(tileHitX, tileHitY) == null)
                        //&& CheckOccupiedTile(map.selectedUnit.currentPath[map.selectedUnit.currentPath.Count - 1].x, map.selectedUnit.currentPath[map.selectedUnit.currentPath.Count - 1].x))
                        {
                            // Move the unit
                            MoveNextTile(map.selectedUnit);
                            map.selectedUnit.hasMoved = true;
                            map.RemoveHighlightedTiles();
                            map.GenerateAttackableTiles();
                            //EndUnitTurn();
                            //map.GenerateSelectableTiles();
                        }
                    }

                    else
                    {
                        if (map.selectedUnit.attackableTiles.Contains(map.GetNode(tileHitX, tileHitY)) && !GetUnitOnMouse(tileHitX, tileHitY).ally)
                        {
                            Attack(map.selectedUnit, GetUnitOnMouse(tileHitX, tileHitY));
                            EndUnitTurn();
                        }
                    }

                }
            }

            if (Input.GetKeyUp("space"))
            {
                if (map.unitSelected)
                {
                    EndUnitTurn();
                }
            }
        }

        // When right click is pressed, it unselects the unit and removes the selectable tiles
        if (Input.GetMouseButtonUp(1))
        {
            if (map.unitSelected && !map.selectedUnit.hasMoved)
            {
                map.unitSelected = false;
                map.selectedUnit.currentPath = null;
                map.RemoveHighlightedTiles();
            }
        }

        if (map.unitSelected && !map.selectedUnit.hasMoved)
        {
            if (map.selectedUnit.currentPath != null)
            {
                for (int currNode = 0; currNode < map.selectedUnit.currentPath.Count - 1; currNode++)
                {
                    Vector3 start = map.TileCoordToWorldCoord(map.selectedUnit.currentPath[currNode].x, map.selectedUnit.currentPath[currNode].y) + new Vector3(0, 0, -1f);
                    Vector3 end = map.TileCoordToWorldCoord(map.selectedUnit.currentPath[currNode + 1].x, map.selectedUnit.currentPath[currNode + 1].y) + new Vector3(0, 0, -1f);

                    //Debug.DrawLine(start, end, Color.cyan);
                    DrawLine(start, end, Color.cyan);
                }
            }
        }

        if (CheckTurnEnded(allyUnits))
        {
            // Start the enemy's turn.
        }

	}

    public void SetSelectedUnit(int x, int y)
    {
        // Reset the unit selected just in case the tile clicked is not a tile with a unit on it
        map.unitSelected = false;
        map.selectedUnit = null;
        map.RemoveHighlightedTiles();

        Unit unitOnMouse = GetUnitOnMouse(x, y);

        if (unitOnMouse != null)
        {
            map.selectedUnit = unitOnMouse;
            map.unitSelected = true;
            Debug.Log(unitOnMouse.transform.name + " Selected! HP = " + unitOnMouse.hp + ".");
        }
    }

    public Unit GetUnitOnMouse(int x, int y)
    {
        for (int i = 0; i < allyUnits.Length; i++)
        {
            if (allyUnits[i].tileX == x && allyUnits[i].tileY == y)
            {
                return allyUnits[i];
            }
        }

        for (int i = 0; i < enemyUnits.Length; i++)
        {
            if (enemyUnits[i].tileX == x && enemyUnits[i].tileY == y)
            {
                return enemyUnits[i];
            }
        }
        return null;
    }

    public bool CheckOccupiedTile(int x, int y)
    {
        for (int i = 0; i < allyUnits.Length; i++)
        {
            if (allyUnits[i].tileX == x && allyUnits[i].tileY == y)
            {
                return true;
            }
        }
        return false;
    }

    /* Moves the unit along the path as far as its movement speed takes it */
    public void MoveNextTile(Unit unit)
    {
        float remainingMovement = unit.movementSpeed;

        while (remainingMovement > 0)
        {
            if (unit.currentPath == null)
            {
                return;
            }

            // Get cost from current tile to next tile
            remainingMovement -= unit.map.CostToEnterTile(unit.currentPath[1].x, unit.currentPath[1].y);

            /* Check after subtracting the cost of the next tile to your movement if your movement speed is below 0
             * If it is below 0, the unit cannot move to that tile */
             if (remainingMovement >= 0)
            {
                // Move us to the next tile in the sequence
                unit.tileX = unit.currentPath[1].x;
                unit.tileY = unit.currentPath[1].y;

                // Update our unity world position
                //unit.transform.position = unit.map.TileCoordToWorldCoord(unit.tileX, unit.tileY);
                unit.transform.position = Vector3.MoveTowards(unit.transform.position, unit.map.TileCoordToWorldCoord(unit.tileX, unit.tileY), 1000f * Time.deltaTime);

                // Remove the old "current" tile
                unit.currentPath.RemoveAt(0);

                if (unit.currentPath.Count == 1)
                {
                    /* We only have one tile left in the path, and that tile MUST be our ultimate
                     * destination -- and we are standing on it! 
                     * So let's just clear our pathfinding info. */
                    unit.currentPath = null;
                }
            }
        }
    }

    public void StartTurn()
    {
        for (int i = 0; i < allyUnits.Length; i++)
        {
            allyUnits[i].turnTaken = false;
            allyUnits[i].hasMoved = false;
            allyUnits[i].GetComponentInChildren<Renderer>().material.color = Color.white;
        }

        //map.selectedUnit = allyUnits[0];
        //map.unitSelected = true;

    }

    public void EndTurn()
    {

    }

    public bool CheckTurnEnded(Unit[] units)
    {
        bool turnEnded = true;

        for (int i = 0; i < units.Length; i++)
        {
            if (!units[i].turnTaken)
            {
                turnEnded = false;
            }
        }
        return turnEnded;
    }

    public void EndUnitTurn()
    {
        map.selectedUnit.turnTaken = true;
        map.selectedUnit.GetComponentInChildren<Renderer>().material.color = Color.gray;
        map.selectedUnit = null;
        map.unitSelected = false;
        map.RemoveHighlightedTiles();
    }

    void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject myline = new GameObject();
        myline.transform.position = start;
        myline.AddComponent<LineRenderer>();
        LineRenderer lr = myline.GetComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        GameObject.Destroy(myline, 0.1f);
    }

    void Attack(Unit attacker, Unit defender)
    {
        float defense = 1.0f - defender.defense / 100.0f;
        int damage = Mathf.RoundToInt(attacker.strength * defense);
        defender.hp -= damage;
        Debug.Log(defender.name + " remaining HP: " + defender.hp);

        if (defender.hp <= 0)
        {
            UnitDie(defender);
        }
    }

    void UnitDie(Unit unit)
    {
        if (unit.ally)
        {
            for (int i = 0; i < allyUnits.Length; i++)
            {
                if (unit == allyUnits[i])
                {
                    allyUnits[i] = null;
                }
            }
        }
        else
        {
            for (int i = 0; i < enemyUnits.Length; i++)
            {
                if (unit == enemyUnits[i])
                {
                    enemyUnits[i] = null;
                }
            }
        }
    }
}
