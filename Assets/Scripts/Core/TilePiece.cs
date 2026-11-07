using UnityEngine;

public class TilePiece : MonoBehaviour
{
    [SerializeField] public bool canBeOccupied;
    [SerializeField] public bool isOccupied;
    [SerializeField] public GameObject root;
    [SerializeField] public SpriteRenderer indicator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetCanBeOccupied(canBeOccupied);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetCanBeOccupied(bool val)
    {
        canBeOccupied = val;
        indicator.enabled = val;

        Color designatedColor = !isOccupied ? Color.white : Color.red;
        designatedColor.a = 0.2f;
        indicator.color = designatedColor;
    }

    public GameObject GetOccupator()
    {
        return root.transform.childCount > 0 ? root.transform.GetChild(0).gameObject : null;
    }

    public void OccupyTile(GameObject occupator)
    {
        if(occupator == null)
        {
            isOccupied = false;
            indicator.enabled = true;
            SetCanBeOccupied(true);
           
            return;
        }

        occupator.transform.parent = root.transform;
        occupator.transform.position = root.transform.position;

        SetCanBeOccupied(false);
        isOccupied = true;
        indicator.enabled = false;
    }

    public GameObject RemoveOccupator()
    {
        GameObject occupator = null;
        if (root.transform.childCount > 0) {
            occupator = root.transform.GetChild(0).gameObject;
        }

        OccupyTile(null);

        return occupator;
    }

    private void OnMouseEnter()
    {
        
    }

    private void OnMouseExit()
    {
        
    }
}
