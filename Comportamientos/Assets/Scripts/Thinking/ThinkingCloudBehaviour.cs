using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThinkingCloudBehaviour : MonoBehaviour
{
    public List<Sprite> images;
    public Image image;

    Transform cam;

    // Start is called before the first frame update
    void Start()
    {
        image.sprite = images[0];
        cam = Camera.current.transform;
    }

    private void Update()
    {
        cam = Camera.current.transform;
        transform.LookAt(cam);
    }

    public void UpdateCloud(int index) 
    {
        image.sprite = images[index];
    }
}
