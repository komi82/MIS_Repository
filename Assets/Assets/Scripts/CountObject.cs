using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CountObject : MonoBehaviour
{
    public GameObject[] objects;
    [SerializeField] private GameObject image;
    // public TextMeshProUGUI text;
    int count;

    void Update()
    {
        objects = GameObject.FindGameObjectsWithTag("Button");
        count = objects.Length;
        Debug.Log(count);
        if (count == 0)
        {
            //  text.text = "Success!!";
                image.SetActive(false);
            
        }
    }
}