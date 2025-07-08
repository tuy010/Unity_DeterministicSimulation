using UnityEngine;

public class ReactiveCube : MonoBehaviour
{
    [SerializeField] Material white;
    [SerializeField] Material red;
    [SerializeField] MeshRenderer mesh;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            mesh.material = red;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            mesh.material = white;
        }
    }
}
