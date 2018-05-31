using UnityEngine;

public class ClickableTile : MonoBehaviour {

    public int tileX;
    public int tileY;
    public TileMap map;
    public GameObject tm;

    void OnMouseDown()
    {
        //GameObject tm = GameObject.Find("TurnManager");
        //TurnManager tee = tm.GetComponent<TurnManager>();

        //tee.ct = this;
    }
}
