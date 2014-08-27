using UnityEngine;
using System.Collections;

public class MirrorObject : MonoBehaviour
{
  public GameObject master;
  public GameObject clone;

  public float gap = 0.1f;
  public float blockHeight = 0.4f;
  public bool shiftUpHeight = true;

  public static bool isEnabledGlobal = true;

  void Start()
  {
    //if (!isEnabled)
    //{
    //  return;
    //}

    if (master == null)
    {
      master = this.gameObject;
      clone = (GameObject)Instantiate(master, GetPosition(master), master.transform.rotation);
      clone.name = clone.name + "(Mirror)";

      Destroy(clone.GetComponent<Rigidbody>());
      Destroy(clone.GetComponent<MirrorObject>());
      Destroy(clone.GetComponent<SoundController>());

      foreach (Transform cTrans in clone.transform)
      {
        if (!cTrans.gameObject.name.StartsWith("Block_") || cTrans.gameObject.tag == "Aligner")
        {
          Destroy(cTrans.gameObject);
          continue;
        }

        var componentsToDestroy = new Component[] {
          cTrans.gameObject.GetComponent<MeshCollider>(),
          cTrans.gameObject.GetComponent<Rigidbody>(),
          cTrans.gameObject.GetComponent<SoundController>()
        };

        foreach (var c in componentsToDestroy)
        {
          if (c != null)
          {
            Destroy(c);
          }
        }
      }
    }
  }

  void OnDestroy()
  {
    Destroy(clone);
  }

  private Vector3 GetPosition(GameObject master)
  {
    return new Vector3(master.transform.position.x, master.transform.position.y + gap, master.transform.position.z);
  }

  private Vector3 GetMirrorPosition(GameObject master)
  {
    return new Vector3(master.transform.position.x, -(master.transform.position.y + gap), master.transform.position.z);
  }

  private Vector3 GetMirrorPositionOfCenter(GameObject master)
  {
    return new Vector3(master.transform.position.x, -(master.transform.position.y + blockHeight / 2 + gap), master.transform.position.z);
  }

  //private static Vector3 GetEulerAngles(GameObject master)
  //{
  //  return new Vector3(master.transform.eulerAngles.x, -(master.transform.eulerAngles.y), master.transform.eulerAngles.z);
  //}

  void FixedUpdate()
  {
    if (!isEnabledGlobal)
    {
      // Disable clone if needed
      if (clone.activeSelf)
      {
        // Hide the clone and make inactive
        clone.transform.position = new Vector3(0, -10000, 0);
        clone.SetActive(false);
      }

      return;
    } else
    {
      // Enable clone if needed
      if (!clone.activeSelf)
      {
        clone.SetActive(true);
      }
    }

    if (clone != null && master != null)
    {
      // If the master is below level, then hide this
      if (master.transform.position.y < -0.5f)
      {
        clone.transform.position = new Vector3(0, -10000, 0);
        return;
      }

      var transform = clone.transform;

      //// Attempt 1: This makes the rotation off center
      //// This works for anything with a symetric vertical axis (almost, but it flips weird at certain angles)
      // gap = 0.1f;
      //transform.eulerAngles = new Vector3(0, master.transform.eulerAngles.y, 0);
      //var relativeForward = transform.forward;
      //transform.rotation = master.transform.rotation;
      //transform.position = GetPosition(master);
      //transform.RotateAround(new Vector3(transform.position.x, 0, transform.position.z), relativeForward, 180);

      //// Attempt 2: This leaves a big gap when the piece is upside down 
      //gap = blockHeight * 2;
      //var ang = master.transform.localEulerAngles;
      //transform.localEulerAngles = -new Vector3(ang.x, -ang.y, ang.z);
      //transform.position = GetMirrorPosition(master);
      //transform.position = transform.position + (master.transform.up * blockHeight);


      // Attempt 3: Perfect!
      gap = 0.1f;
      var ang = master.transform.localEulerAngles;
      // Opposite side-side angle
      transform.localEulerAngles = -new Vector3(ang.x, -ang.y, ang.z);

      // mirror the center
      transform.position = GetMirrorPositionOfCenter(master);

      // Shift the block down (as the top block rot point is at the bottom of the block)
      transform.position = transform.position - (transform.up * blockHeight);
    }

  }
}
