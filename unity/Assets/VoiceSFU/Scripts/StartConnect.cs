using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartConnect : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var connect=GetComponent<Connect>();
        connect.StartConnect("192.168.1.7");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
