using System;
using UnityEngine;

public class AI_Cell : MonoBehaviour
{
    public int x, y;
    public Action<int, int> cellClicked;
    public SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();    
    }

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y; 
    }

    private void OnMouseDown()
    {
        cellClicked?.Invoke(x, y); 
    }

    public void ChangeColor(int player)
    {
        Color color = new Color();
        color.a = 1f; 

        if(player == 1)
        {
            color = Color.black;
            sr.color = color; 
        }

        else
        {
            color = Color.white;
            sr.color = color; 
        }
    }
}
