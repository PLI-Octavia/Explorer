using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clickable : MonoBehaviour
{
    public bool IsClicked { get; private set; }

    void OnMouseDown()
    {
        IsClicked = true;
    }

    public void ResetClicked()
    {
        IsClicked = false;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        IsClicked = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
