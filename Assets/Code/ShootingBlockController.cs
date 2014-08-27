using UnityEngine;
using System.Collections;

public class ShootingBlockController : MonoBehaviour
{
  public float maxTimeForShot = 15.0f;

  public float _scale = 1;
  public float scale
  {
    get { return _scale; }
    set
    {
      _scale = value;
      ScaleMass();
    }
  }

  public ShootingBlockState shootingBlockState { get; private set; }
  public float finishedTime;
  private float startTime = 0;
  private float initMass = 0;
  private float initDrag = 0;

  void Start()
  {
    shootingBlockState = ShootingBlockState.NotStarted;
  }

  public void FinishedIsHandled()
  {
    Destroy(this);
  }

  void FixedUpdate()
  {
    if (shootingBlockState == ShootingBlockState.NotStarted)
    {
      // Increase Block Mass while shooting

      ScaleMass();
      startTime = Time.time;
      shootingBlockState = ShootingBlockState.Shooting;

      Debug.Log("Shooting Block - shooting");
    }

    if (shootingBlockState == ShootingBlockState.Shooting)
    {
      if ((rigidbody.velocity.sqrMagnitude < 0.01 && Time.time - startTime > 1) ||
        Time.time - startTime > maxTimeForShot)
      {
        rigidbody.mass = initMass;
        rigidbody.drag = initDrag;
        shootingBlockState = ShootingBlockState.Finished;
        finishedTime = Time.time;

        Debug.Log("Shooting Block - finished");
      }
    }
  }

  void ScaleMass()
  {
    if (initMass == 0)
    {
      initMass = rigidbody.mass;
      initDrag = rigidbody.drag;
    }

    rigidbody.mass = initMass * _scale;
    rigidbody.drag = 0;
  }
}

public enum ShootingBlockState
{
  NotStarted,
  Shooting,
  Finished
}
