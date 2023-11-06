using System;
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using UnityEngine;
using UnityEngine.UI;



[ExecuteInEditMode]
public class NodeRenderer : MonoBehaviour,IButtonListenable
{

    
    public GameObject oState;
    public GameObject xState;


    private void Awake()
    {
        if (oState == null)
            oState = transform.Find("O")?.gameObject;
        
        if (xState == null)
            xState = transform.Find("X")?.gameObject;
    }

    public void SetRenderState(NodeState state)
    {
        switch (state)
        {
            case NodeState.Empty:
                oState?.SetActive(false);
                xState?.SetActive(false);
                break;
            case NodeState.O:
                oState?.SetActive(true);
                xState?.SetActive(false);
                break;
            case NodeState.X:
                oState?.SetActive(false);
                xState?.SetActive(true);
                break;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<Action> OnClick { get; set; }
    public Button GetButton()
    {
        var button = GetComponent<Button>();

        return button;
    }
}
