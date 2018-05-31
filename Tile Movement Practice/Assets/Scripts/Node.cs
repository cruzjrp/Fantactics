using System.Collections.Generic;

public class Node {

    public List<Node> neighbors;
    public int x;
    public int y;

    public Node()
    {
        neighbors = new List<Node>();
    }

}
