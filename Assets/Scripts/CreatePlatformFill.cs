using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class CreatePlatformFill : MonoBehaviour
{
   [SerializeField] private const float MAX_LENGTH = 10f;
   [SerializeField] private LayerMask targetLayer;
   private SpriteShapeController ssc;
   
   private List<Vector2> pointList;

    void Start()
    {
        ssc = GetComponent<SpriteShapeController>();
        CreatePointList();
        for (int i = 0; i < pointList.Count ; i++)
        {
            FireRaycast(pointList[i]);
        }
    }

    void CreatePointList()
    {
        Spline spline = ssc.spline;

        for (int i = 0; i < spline.GetPointCount(); i++)
        {
            Vector3 worldPos = transform.TransformPoint(spline.GetPosition(i));
            pointList.Add(worldPos);
        }
    }

    void FireRaycast(Vector2 origin)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin,Vector2.down,MAX_LENGTH,targetLayer);
    } 
}
