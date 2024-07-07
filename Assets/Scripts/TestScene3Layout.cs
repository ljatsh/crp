using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScene3Layout : MonoBehaviour
{
    void Awake()
    {
    }
    // Start is called before the first frame update
    void Start()
    {
        Scene scene = SceneManager.GetActiveScene();
        var objects = scene.GetRootGameObjects();
        Random.InitState(100);
        foreach(GameObject go in objects)
        {
            if (go.name.StartsWith("Sphere"))
            {
                go.transform.position = Random.insideUnitSphere * 5;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log("Update...");
    }
}
