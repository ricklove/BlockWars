using UnityEngine;
using System.Collections.Generic;

public class SimpleJoint : MonoBehaviour
{
  public static bool globalDisable = false;

  // InverseSring = The closer the stronger
  public static bool usesInverseSpring = true;
  public static float sleepDistance = 0.1f;// 0.02f;
  public static float physicsSleepVelocityToForceModifier = 1.0f;

  private float targetDistance = 0.0f;
  private float springXZ = 200;//500;
  private float breakDistance = 0.45f;
  private Vector3 anchor = Vector3.zero;


  private bool separateYAxis = true;
  private float springY = 1;

  private Rigidbody thisRigidbody;
  private List<Rigidbody> connectedBodies;

  public void AddConnectedBody(Rigidbody rigidbody)
  {
    if (!connectedBodies.Contains(rigidbody))
    {
      connectedBodies.Add(rigidbody);
    }
  }

  public void RemoveConnectedBody(Rigidbody rigidbody)
  {
    if (connectedBodies.Contains(rigidbody))
    {
      connectedBodies.Remove(rigidbody);
    }
  }

  public bool IsConnected(Rigidbody rigidbody)
  {
    return connectedBodies.Contains(rigidbody);
  }

  public void Start()
  {
    if (connectedBodies == null)
    {
      connectedBodies = new List<Rigidbody>();
      thisRigidbody = this.rigidbody;
    }
  }

  public void FixedUpdate()
  {
    if (globalDisable)
    {
      return;
    }

    if (connectedBodies == null)
    {
      connectedBodies = new List<Rigidbody>();
      thisRigidbody = this.rigidbody;
    }

    var broken = new List<Rigidbody>();

    foreach (var connectedBody in connectedBodies)
    {
      // This is a possible quirk because a destroyed rigidbody == null
      if (connectedBody == null)
      {
        broken.Add(connectedBody);
        continue;
      }

      Debug.DrawLine(this.transform.position, connectedBody.position, Color.cyan, 0.1f);

      // Apply force along this line
      var diff = this.transform.position - connectedBody.position;
      var distSqr = diff.sqrMagnitude;

      if (distSqr > breakDistance * breakDistance)
      {
        broken.Add(connectedBody);
        continue;
      }

      if (distSqr < (targetDistance + sleepDistance) * (targetDistance + sleepDistance))
      {
        continue;
      }

      var spring = springXZ;

      if (separateYAxis)
      {
        var xzOnly = new Vector3(diff.x, 0, diff.z);

        if (xzOnly.sqrMagnitude > diff.y * diff.y)
        {
          // Use xz
          diff = xzOnly;
          spring = springXZ;
        }
        // Use y
        else
        {
          diff = new Vector3(0, diff.y, 0);
          spring = springY;
        }

      }

      var distance = diff.magnitude;
      var distanceFromBreaking = breakDistance - distance;

      var scale = spring * (!usesInverseSpring ? distance : distanceFromBreaking);

      // Don't disturb the rigidbodies for small changes
      if (scale < Physics.sleepVelocity * physicsSleepVelocityToForceModifier)
      {
        continue;
      }

      var force = diff.normalized;
      force.Scale(new Vector3(scale, scale, scale));

      thisRigidbody.AddForce(Vector3.zero - force);
      connectedBody.AddForce(force);
    }

    foreach (var b in broken)
    {
      RemoveConnectedBody(b);
    }
  }
}
