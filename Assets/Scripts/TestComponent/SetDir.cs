using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetDir : MonoBehaviour
{
    // Start is called before the first frame update
    public MoveComponent moveComponent;

    public Vector2 vector2 = Vector2.zero;
    void Start()
    {
        moveComponent.SetDirection(vector2);
    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
