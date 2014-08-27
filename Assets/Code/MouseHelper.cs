using UnityEngine;
using System.Collections;

public class MouseHelper : MonoBehaviour
{
  public bool showDebug = false;

  private GameObject mouseHeightPlane;
  private Camera gameCamera;

  private Vector3? cache;
  private float? cacheHeight;

  void Start()
  {
    mouseHeightPlane = GameObject.Find("MouseHeightPlane");
    gameCamera = GameObject.Find("GameCamera").GetComponent<Camera>();
  }

  void Update()
  {
    cache = null;
    cacheHeight = null;
  }

  public Vector3 GetMousePosition(float height)
  {
    if (cache != null && cacheHeight == height)
    {
      return cache.Value;
    }

    mouseHeightPlane.transform.position = new Vector3(0, height, 0);

    var mPos = Input.mousePosition;
    Vector3 mPosWorld = new Vector3(0, 0, 0);

    Ray ray = gameCamera.ScreenPointToRay(mPos);
    RaycastHit hit;

    // Get the bit for that layer
    var layerMask = 1 << Consts.mouseHeightPlaneLayerNumber;

    if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
    {
      mPosWorld = hit.point;

      // Debug
      if (showDebug)
      {
        var centerScreenPoint = gameCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 5));
        Debug.DrawLine(centerScreenPoint, hit.point, Color.white, 3);
      }
    }
    else if (showDebug)
    {
      Debug.Log("Mouse Plane was not hit!");
    }

    cache = mPosWorld;
    cacheHeight = height;
    return cache.Value;
  }

  // TODO: Move this to CameraHelper or change name to MouseAndCameraHelper
  public void MoveCameraToLookAtGroundPoint(float groundX, float groundZ)
  {
    // This depends on a fixed 60 degree angle
    var height = gameCamera.transform.position.y;
    var backForHeight = -height / 2;

    gameCamera.transform.position = new Vector3(groundX, height, groundZ + backForHeight);
  }
}
