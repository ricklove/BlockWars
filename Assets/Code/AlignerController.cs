using UnityEngine;
using System.Collections;

public class AlignerController : MonoBehaviour
{
  public bool hasBeenInitialized = false;

  void InitializeJoints()
  {
    if (!hasBeenInitialized)
    {
      // Attach the aligner to its game object
      var tFixedJoint = this.GetComponent<FixedJoint>();
      tFixedJoint.connectedBody = this.transform.parent.gameObject.GetComponent<Rigidbody>();

      Debug.DrawLine(this.transform.position, new Vector3(this.transform.position.x + 10, this.transform.position.y + 10, this.transform.position.z + 10), Color.yellow, 0.1f);

      hasBeenInitialized = true;
    }
  }

  void OnTriggerEnter(Collider other)
  {
    InitializeJoints();

    // Create spring
    var tRigidbody = this.GetComponent<Rigidbody>();
    var oRigidbody = other.GetComponent<Rigidbody>();

    if (tRigidbody != null && oRigidbody != null &&
      tRigidbody.tag == "Aligner" && oRigidbody.tag == "Aligner")
    {
      Debug.DrawLine(this.transform.position, other.transform.position, Color.black, 3);

      // Only create if there is not already a spring between the two 
      var tJoint = tRigidbody.GetComponent<SimpleJoint>();
      var oJoint = oRigidbody.GetComponent<SimpleJoint>();

      if (!tJoint.IsConnected(oRigidbody) && !oJoint.IsConnected(tRigidbody))
      {
        // Simple Joint
        tJoint.AddConnectedBody(oRigidbody);
        Debug.DrawLine(this.transform.position, other.transform.position, Color.green, 3);
      }
    }

  }
}
