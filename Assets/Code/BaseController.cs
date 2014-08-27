using UnityEngine;
using System.Collections.Generic;

public class BaseController : MonoBehaviour
{
    //public float explosionTimeOut = 5.0f;
    public float explosionCountDown = 2.0f;
    public float explosionRadius = 15.0f;
    public float explosionPower = 150000.0f;
    public float explosionUpwards = 3.0f;

    public BaseControllerState baseState { get; private set; }
    private bool isArmed = false;

    public bool isTouchingBlock { get; private set; }
    private float timeSinceNotTouching;

    private int touchingBlockCount;
    private bool touchingBlockCountChanged;

    public void MakeActive()
    {
        Debug.Log("Base enabled");
        baseState = BaseControllerState.Alive;
    }

    void OnTriggerEnter(Collider other)
    {
        UpdateIsTouchingBlock();
    }

    void OnTriggerExit(Collider other)
    {
        UpdateIsTouchingBlock();
    }

    void UpdateIsTouchingBlock()
    {
        var isNowTouchingBlock = false;

        Vector3 pos = transform.position;
        var radius = GetComponent<SphereCollider>().radius;
        Collider[] colliders = Physics.OverlapSphere(pos, radius);

        touchingBlockCount = 0;
        touchingBlockCountChanged = true;

        foreach (Collider hit in colliders)
        {
            var hitObj = hit.transform.parent.gameObject;

            if (hitObj.tag == "Block")
            {
                isNowTouchingBlock = true;
                //break;

                touchingBlockCount++;
            }
        }

        if (isTouchingBlock != isNowTouchingBlock)
        {
            if (isNowTouchingBlock)
            {
                Debug.Log("Base is Touching a Block - It is ARMED.");
                isTouchingBlock = true;
                isArmed = true;
            }
            else
            {
                Debug.Log("Base is NOT Touching Any Blocks!");
                isTouchingBlock = false;
                timeSinceNotTouching = Time.time;
            }
        }

    }

    void FixedUpdate()
    {
        // Critical
        if (touchingBlockCountChanged)
        {
            touchingBlockCountChanged = false;

            var isSafe = false;
            var isDanger = false;
            var isCritical = false;

            if (touchingBlockCount > 3)
            {
                isSafe = true;
            }
            else if (touchingBlockCount > 0)
            {
                isDanger = true;
            }
            else //if (touchingBlockCount > 0)
            {
                isCritical = true;
            }

            // Change the core color
            foreach (Transform cTransform in transform)
            {
                if (cTransform.name == "BaseMarker")
                {
                    foreach (Transform inner in cTransform.gameObject.transform)
                    {
                        if (inner.name == "Sphere")
                        {
                            inner.gameObject.SetActive(isSafe);
                        }
                        else if (inner.name == "Sphere_Danger")
                        {
                            inner.gameObject.SetActive(isDanger);
                        }
                        else if (inner.name == "Sphere_Critical")
                        {
                            inner.gameObject.SetActive(isCritical);
                        }
                    }
                }
            }
        }

        // Explode
        if (baseState == BaseControllerState.Alive &&
          isArmed && !isTouchingBlock &&
          Time.time - timeSinceNotTouching > explosionCountDown)
        {
            Vector3 explosionPos = transform.position;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
            foreach (Collider hit in colliders)
            {
                //Debug.Log("Hit! - " + hit.gameObject.name);

                var hitObj = hit.transform.parent.gameObject;

                if (hitObj.tag == "Block")
                {
                    hitObj.rigidbody.AddExplosionForce(explosionPower, explosionPos, explosionRadius, explosionUpwards);
                    //var force = hitObj.transform.position - explosionPos;
                    //force = (force.normalized + new Vector3(0, 1, 0)) * explosionPower;

                    //hitObj.rigidbody.AddForce(force);
                    //hitObj.rigidbody.MovePosition(hitObj.rigidbody.position + force * Time.deltaTime);
                    Debug.Log("BOOM! - " + hitObj.name + " - " + explosionPower);
                }
            }

            Debug.Log("BOOM! @ " + explosionPos);
            audio.Play();

            // Try just one hit for performance
            baseState = BaseControllerState.Dead;


            // Destroy the base marker
            foreach (Transform cTransform in transform)
            {
                if (cTransform.name == "BaseMarker")
                {
                    Destroy(cTransform.gameObject);
                }
            }

            //this.active = false;

            //if (Time.time - timeSinceNotTouching > explosionCountDown + explosionTimeOut)
            //{
            //  baseState = BaseControllerState.Dead;
            //}
        }
    }
}

public enum BaseControllerState
{
    NotActive,
    Alive,
    Dead
}
