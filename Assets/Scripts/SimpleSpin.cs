using UnityEngine;

public class SimpleSpin : MonoBehaviour
{
    [SerializeField] private float spinSpeed = 1200f;

    void Update()
    {
        if(transform.rotation.x == -1)
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        else
            transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
    }
}