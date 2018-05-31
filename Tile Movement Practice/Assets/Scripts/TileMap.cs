using System.Collections.Generic;
using UnityEngine;

public class TileMap : MonoBehaviour
{

    public Unit selectedUnit;
    public bool unitSelected = false;
    public int mapSizeX = 10;
    public int mapSizeY = 10;
    public TurnManager turnManager;
    public TileType[] tileTypes; // Array of all the different tile types on the map

    public int[,] tiles; // Array of tiles where the int is the tileType
    Node[,] graph; // Array of nodes for the graph
    GameObject[,] tileVisuals; // Array of the instantiated tiles, used mostly for changing material color

    // Use this for initialization
    void Start()
    {
        //selectedUnit.tileX = (int)selectedUnit.transform.position.x;
        //selectedUnit.tileY = (int)selectedUnit.transform.position.y;
        //selectedUnit.map = this;

        GenerateMapData();
        GeneratePathfindingGraph();
        GenerateMapVisual();
    }

    // Update is called once per frame
    void Update()
    {

    }

    /* Generates the tile types for each tile on the map */
    void GenerateMapData()
    {
        // Creates our map tiles
        tiles = new int[mapSizeX, mapSizeY];

        // Initialize our map tiles to be grass
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                tiles[x, y] = 0;
            }
        }

        // Make a big swamp area
        for (int x = 3; x <= 5; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                tiles[x, y] = 1;
            }
        }

        // Let's make a u-shaped mountain range
        tiles[4, 4] = 2;
        tiles[5, 4] = 2;
        tiles[6, 4] = 2;
        tiles[7, 4] = 2;
        tiles[8, 4] = 2;

        tiles[4, 5] = 2;
        tiles[4, 6] = 2;
        tiles[8, 5] = 2;
        tiles[8, 6] = 2;
    }

    /* Generates the tile visuals on the screen */
    void GenerateMapVisual()
    {
        // Array of copies of the instantiated tiles
        tileVisuals = new GameObject[mapSizeX, mapSizeY];

        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                TileType currentTileType = tileTypes[tiles[x, y]];
                GameObject go = (GameObject)Instantiate(currentTileType.tileVisualPrefab, new Vector3(x, y, 0), Quaternion.identity);
                go.layer = 9;
                tileVisuals[x, y] = go;
            }
        }
    }

    /* Returns if the unit can enter the tile or not */
    public bool UnitCanEnterTile(int x, int y)
    {
        /* If there is a unit already on top of the tile, 
         * then the unit cannot enter the tile */
        //if (turnManager.CheckOccupiedTile(x, y))
        //{
        //    return false;
        //}

        /* We could test the unit's walk/hover/fly type against various
         * terrain flags here to see if they are allowed to enter the tile. */
        return tileTypes[tiles[x, y]].isWalkable;
    }

    /* Returns the cost to enter the tile. If the unit can't enter the tile, the cost is infinity */
    public float CostToEnterTile(int targetX, int targetY)
    {
        if (!UnitCanEnterTile(targetX, targetY))
        {
            return Mathf.Infinity;
        }
        return tileTypes[tiles[targetX, targetY]].movementCost;
    }

    /* Creates the graph for pathfinding, and gets neighbors of all nodes on the graph */
    void GeneratePathfindingGraph()
    {
        // Initialize the array of nodes the size of the map
        graph = new Node[mapSizeX, mapSizeY];

        // Initialize each node on the map
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                graph[x, y] = new Node();
                graph[x, y].x = x;
                graph[x, y].y = y;
            }
        }

        // Now that all the nodes exist, calculate their neighbors
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                // This is the 4-way connection version
                if (x > 0)
                {
                    // Gets the left neighbor, only if the current node is not on the left edge of the map
                    graph[x, y].neighbors.Add(graph[x - 1, y]);
                }
                if (x < mapSizeX - 1)
                {
                    // Gets the right neighbor, only if the current node is not on the right edge of the map
                    graph[x, y].neighbors.Add(graph[x + 1, y]);
                }
                if (y > 0)
                {
                    // Gets the top neighbor, only if the current node is not on the top edge of the map
                    graph[x, y].neighbors.Add(graph[x, y - 1]);
                }
                if (y < mapSizeY - 1)
                {
                    // Gets the bottom neighbor, only if the current node is not on the bottom edge of the map
                    graph[x, y].neighbors.Add(graph[x, y + 1]);
                }
            }
        }
    }

    /* No idea */
    public Vector3 TileCoordToWorldCoord(int x, int y)
    {
        return new Vector3(x, y, 0);
    }

    /* Generates the path from source to target using Djikstra's Algorithm */
    public void GeneratePathTo(int targetX, int targetY)
    {
        // Clear out our unit's old path
        selectedUnit.currentPath = null;

        if (UnitCanEnterTile(targetX, targetY))
        {
            // Dictionary of the distance of all nodes from the source
            Dictionary<Node, float> distance = new Dictionary<Node, float>();
            Dictionary<Node, Node> previousNode = new Dictionary<Node, Node>();

            // Setup the "Q" -- the list of nodes we haven't checked yet
            List<Node> unvisitedNodes = new List<Node>();

            Node source = graph[selectedUnit.tileX, selectedUnit.tileY];
            Node target = graph[targetX, targetY];

            distance[source] = 0;
            previousNode[source] = null; // Previous node in optimal path from source, basically a dictionary of the path from the source to every other node

            /* Initialize everything to have INFINITY distance since
             * we don't know any better right now. Also, it's possible
             * that some nodes CAN'T be reached from the source, which
             * would make INFINITY a reasonable value */
            foreach (Node node in graph)
            {
                if (node != source)
                {
                    distance[node] = Mathf.Infinity;
                    previousNode[node] = null;
                }
                // Set every node to be unvisited
                unvisitedNodes.Add(node);
            }

            // Goes through all unvisited nodes
            while (unvisitedNodes.Count > 0)
            {
                // "u" is going to be the unvisited node with the smallest distance.
                Node u = null;

                foreach (Node possibleU in unvisitedNodes)
                {
                    /* Gets node with the shortest distance. In the first pass, u is the source
                     * because the source has distance 0 while every other node has a distance of infinity */
                    if (u == null || distance[possibleU] < distance[u])
                    {
                        u = possibleU;
                    }
                }
                // If we've found the target, exit the while loop
                if (u == target)
                {
                    break;
                }

                unvisitedNodes.Remove(u);

                // Sets the shortest distance of all the neighbors of the node with the shortest distance
                foreach (Node v in u.neighbors)
                {
                    /* Calculate the cost of the node with the shortest distance to its neighbors
                     * In the case of the source node, alt = 0 + cost = cost; */
                    float alt = distance[u] + CostToEnterTile(v.x, v.y);
                    if (alt < distance[v])
                    {
                        distance[v] = alt; // Set the distance of the neighbor
                        previousNode[v] = u; // Sets u to be the previous node of the neighbor
                    }
                }
            }
            // At this point we have either found the shortest route or there is no route at all to our target

            // No route between our target and the source
            if (previousNode[target] == null)
            {
                return;
            }

            List<Node> currentPath = new List<Node>();
            Node curr = target;

            // Step through the "previousNode" chain and add it to our path, going from the target to the source
            while (curr != null)
            {
                currentPath.Add(curr);
                curr = previousNode[curr];
            }

            /* Right now, currentPath describes a route from our target to our source,
             * so we need to invert it! */
            currentPath.Reverse();

            selectedUnit.currentPath = currentPath;
        }
    }

    /* Generates the path from source to target using Djikstra's Algorithm */
    public void GenerateSelectableTiles()
    {
        // Clear out our unit's old selectableTiles
        selectedUnit.selectableTiles = null;

        // Dictionary of the distance of all nodes from the source
        Dictionary<Node, float> distance = new Dictionary<Node, float>();

        // Setup the "Q" -- the list of nodes we haven't checked yet
        List<Node> unvisitedNodes = new List<Node>();

        Node source = graph[selectedUnit.tileX, selectedUnit.tileY];

        distance[source] = 0;

        /* Initialize everything to have INFINITY distance since
         * we don't know any better right now. Also, it's possible
         * that some nodes CAN'T be reached from the source, which
         * would make INFINITY a reasonable value */
        foreach (Node node in graph)
        {
            if (node != source)
            {
                distance[node] = Mathf.Infinity;
            }
            // Set every node besides the source to be unvisited
            unvisitedNodes.Add(node);
        }

        // Goes through all unvisited nodes
        while (unvisitedNodes.Count > 0)
        {
            // "u" is going to be the unvisited node with the smallest distance.
            Node u = null;

            foreach (Node possibleU in unvisitedNodes)
            {
                /* Gets node with the shortest distance. In the first pass, u is the source
                 * because the source has distance 0 while every other node has a distance of infinity */
                if (u == null || distance[possibleU] < distance[u])
                {
                    u = possibleU;
                }
            }

            unvisitedNodes.Remove(u);

            // Sets the shortest distance of all the neighbors of the node with the shortest distance
            foreach (Node v in u.neighbors)
            {
                /* Calculate the cost of the node with the shortest distance to its neighbors
                 * In the case of the source node, alt = 0 + cost = cost; */
                float alt = distance[u] + CostToEnterTile(v.x, v.y);
                if (alt < distance[v])
                {
                    distance[v] = alt; // Set the distance of the neighbor
                }
            }
        }

        List<Node> selectableTiles = new List<Node>();

        // Remove tile highlights from the previously selected character.
        RemoveHighlightedTiles();

        foreach (Node tile in graph)
        {
            if (distance[tile] >= 0 && distance[tile] <= selectedUnit.movementSpeed)
            {
                tileVisuals[tile.x, tile.y].GetComponent<Renderer>().material.color = Color.blue;
                selectableTiles.Add(tile);
                //Debug.Log(tile.x + ", " + tile.y);
            }
        }

        selectedUnit.selectableTiles = selectableTiles;
    }

    /* Generates the path from source to target using Djikstra's Algorithm */
    public void GenerateSelectableTiles(Unit unit)
    {
        // Clear out our unit's old selectableTiles
        unit.selectableTiles = null;

        // Dictionary of the distance of all nodes from the source
        Dictionary<Node, float> distance = new Dictionary<Node, float>();

        // Setup the "Q" -- the list of nodes we haven't checked yet
        List<Node> unvisitedNodes = new List<Node>();

        Node source = graph[unit.tileX, unit.tileY];

        distance[source] = 0;

        /* Initialize everything to have INFINITY distance since
         * we don't know any better right now. Also, it's possible
         * that some nodes CAN'T be reached from the source, which
         * would make INFINITY a reasonable value */
        foreach (Node node in graph)
        {
            if (node != source)
            {
                distance[node] = Mathf.Infinity;
            }
            // Set every node besides the source to be unvisited
            unvisitedNodes.Add(node);
        }

        // Goes through all unvisited nodes
        while (unvisitedNodes.Count > 0)
        {
            // "u" is going to be the unvisited node with the smallest distance.
            Node u = null;

            foreach (Node possibleU in unvisitedNodes)
            {
                /* Gets node with the shortest distance. In the first pass, u is the source
                 * because the source has distance 0 while every other node has a distance of infinity */
                if (u == null || distance[possibleU] < distance[u])
                {
                    u = possibleU;
                }
            }

            unvisitedNodes.Remove(u);

            // Sets the shortest distance of all the neighbors of the node with the shortest distance
            foreach (Node v in u.neighbors)
            {
                /* Calculate the cost of the node with the shortest distance to its neighbors
                 * In the case of the source node, alt = 0 + cost = cost; */
                float alt = distance[u] + CostToEnterTile(v.x, v.y);
                if (alt < distance[v])
                {
                    distance[v] = alt; // Set the distance of the neighbor
                }
            }
        }

        List<Node> selectableTiles = new List<Node>();

        // Remove tile highlights from the previously selected character.
        RemoveHighlightedTiles();

        foreach (Node tile in graph)
        {
            if (distance[tile] >= 0 && distance[tile] <= unit.movementSpeed)
            {
                tileVisuals[tile.x, tile.y].GetComponent<Renderer>().material.color = Color.blue;
                selectableTiles.Add(tile);
                //Debug.Log(tile.x + ", " + tile.y);
            }
        }


    }

    /* Allows the player to manually create their own path for the selected unit, while fixing user errors.   
     * It will "cut off" the current path at a certain tile in the path if the player backtracks to a tile that is already in the path.  
     * It will use pathfinding for the following: 
     * - If the player goes outside of the movement range of the unit and points their mouse back inside the movement range.
     * - If the player's path goes through more tiles (or has a greater movement cost) than the unit's movement speed, but the next chosen tile is still inside the movement range. */
    public void ManualSelectedUnitPath(int x, int y)
    {
        // If the current path is null, add the source to the path
        if (selectedUnit.currentPath == null)
        {
            selectedUnit.currentPath = new List<Node>();
            selectedUnit.currentPath.Add(graph[selectedUnit.tileX, selectedUnit.tileY]);
        }

        Node nextTile = graph[x, y];
        Node latestNodeInCurrentPath = selectedUnit.currentPath[selectedUnit.currentPath.Count - 1];

        // Check if the tile is within the unit's selectable tiles, is a neighbor of the last node in the current path, is walkable, and the current path is less than or equal to the movement speed of the unit
        if (selectedUnit.selectableTiles.Contains(nextTile) && tileTypes[tiles[x, y]].isWalkable && selectedUnit.currentPath.Count - 1 <= selectedUnit.movementSpeed)
        {
            // Check if the next tile is not a neighbor of the latest node in the path, and the next tile is not the latest node itself.  
            // This is for when the user moves their cursor outside of the selectable tiles and puts their mouse back in. 
            if (!latestNodeInCurrentPath.neighbors.Contains(nextTile) && latestNodeInCurrentPath != nextTile)
            {
                // Use pathfinding 
                Debug.Log("yaes");
                GeneratePathTo(x, y);
            }
            // Check if the next tile is a neighbor of the latest node in the path
            else if (latestNodeInCurrentPath.neighbors.Contains(nextTile))
            {
                bool foundDuplicateTile = false;
                int duplicateIndex = 0;

                // Check if the next tile is already in the current path
                for (int i = 0; i < selectedUnit.currentPath.Count; i++)
                {
                    if (nextTile == selectedUnit.currentPath[i])
                    {
                        foundDuplicateTile = true;
                        duplicateIndex = i;
                    }
                }

                // If it's in the current path, remove everything in the current path after the the original given tile.
                // Ex. Path = 1, 2, 3, 4. Next tile = 2. Now path = 1. 2 will be re-added after the removal. 
                if (foundDuplicateTile)
                {
                    selectedUnit.currentPath.RemoveRange(duplicateIndex, selectedUnit.currentPath.Count - duplicateIndex);
                }

                // If the current path is longer than the movement speed of the unit, but is still inside the movement range, use pathfinding to create the new path
                // Uses pathfinding also if the current path PLUS the next tile has a greater movement cost than the movement speed of the unit, but is still inside its movement range
                if (selectedUnit.currentPath.Count - 1 > selectedUnit.movementSpeed || CostOfCurrentPath() + CostToEnterTile(nextTile.x, nextTile.y) > selectedUnit.movementSpeed)
                {
                    Debug.Log("ya" + (CostOfCurrentPath() + CostToEnterTile(nextTile.x, nextTile.y)));
                    GeneratePathTo(x, y);
                }

                // Add the next tile to the current path if the cost is still less than or equal to the movement speed of the unit
                if (CostOfCurrentPath() + CostToEnterTile(nextTile.x, nextTile.y) <= selectedUnit.movementSpeed)
                {
                    selectedUnit.currentPath.Add(nextTile);
                }
            }
        }
    }

    public void RemoveHighlightedTiles()
    {
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                tileVisuals[x, y].GetComponent<Renderer>().material.color = tileTypes[tiles[x, y]].tileVisualPrefab.GetComponent<Renderer>().sharedMaterial.color;
            }
        }
    }

    public Node GetNode(int x, int y)
    {
        return graph[x, y];
    }

    public float CostOfCurrentPath()
    {
        float currentCost = 0;

        // Start at index 1 because index 0 is the source and the source costs nothing. 
        // If the current path is only of length 1, the for loop does not run, and the currentCost = 0
        for (int i = 1; i < selectedUnit.currentPath.Count; i++)
        {
            currentCost += tileTypes[tiles[selectedUnit.currentPath[i].x, selectedUnit.currentPath[i].y]].movementCost;
        }
        return currentCost;
    }

}
