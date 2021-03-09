using UnityEngine;
using System.Collections;
using PicaVoxel;

public class InvadersWave : MonoBehaviour
{
    public Object InvaderPrefab;

    public float Direction = 1f;
    public float Speed = 0.1f;
    public float Width = 10f;

	// Use this for initialization
	void Start () {
        // Setting the gravity manually because the demo games are shared in one project
        Physics.gravity = new Vector3(0f, -50f, 0f);

	    for (int y = 0; y < 6; y++)
	    {
	        for (int x = 0; x < 12; x++)
	        {
	            InvadersInvader newInvader = ((GameObject)Instantiate(InvaderPrefab, Vector3.zero, Quaternion.identity)).GetComponent<InvadersInvader>();
                Material m = new Material(newInvader.GetComponent<Volume>().Material);
                m.SetColor("_Tint", GetRowColor(y));
	            newInvader.name = "Invader";
                newInvader.GetComponent<Volume>().Material = m;
                newInvader.GetComponent<Volume>().CreateChunks();
	            newInvader.transform.parent = transform;
                newInvader.transform.position = transform.position + new Vector3(-8.2f + (x*1.5f),-3f+y);
	            newInvader.Wave = this;
                
	        }
	    }
	}

    Color GetRowColor(int row)
    {
        switch (row)
        {
            case 0: return new Color(1f, 0f, 0f);
            case 1: return new Color(0f, 1f, 0f);
            case 2: return new Color(0f, 0f, 1f);
            case 3: return new Color(1f, 1f, 0f);
            case 4: return new Color(1f, 0f, 1f);
            case 5: return new Color(0f, 1f, 1f);
            default: return Color.white;
        }
    }
	
	// Update is called once per frame
	void Update () {
	    
	}

    public void BoundaryReached(float pos)
    {
        if (pos > 0) Direction = -1f;
        else Direction = 1f;

        transform.Translate(0f,-0.5f,0f, Space.World);
    }
}
