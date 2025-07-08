using Unity.VisualScripting;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    public Transform target;
    public Transform camTf;
    Vector3 dif;

    void Start()
    {
        if(camTf != null) camTf = GetComponent<Transform>();
        dif = target.position - camTf.position;
    }

    void LateUpdate()
    {
        camTf.position = target.position - dif;
    }
}
