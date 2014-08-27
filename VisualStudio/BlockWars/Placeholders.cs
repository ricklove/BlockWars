using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class MirrorObject
{

}

namespace UnityEngine
{
    public class MonoBehaviour
    {
        public T GetComponent<T>(string name) { return default(T); }

        public void Destroy(GameObject gameObject) { throw new NotImplementedException(); }
        public GameObject Instantiate(GameObject gameObject, Vector3 position, Quaternion rotation) { throw new NotImplementedException(); }
        public PlayerPrefs PlayerPrefs { get; set; }
        public Resources Resources;
    }

    public class TextAsset
    {
        public string text { get; set; }
    }


    public class Rigidbody
    {
        public RigidbodyConstraints constraints { get; set; }
        public float drag { get; set; }
        public bool isKinematic { get; set; }
    }

    public enum RigidbodyConstraints
    {
        None
    }

    public class Resources
    {
        public object Load(string name)
        {
            throw new NotImplementedException();
        }
    }

    public class PlayerPrefs
    {
        public void DeleteKey(string name)
        {
            throw new NotImplementedException();
        }

        public string GetString(string name)
        {
            throw new NotImplementedException();
        }

        public void SetString(string name, string value)
        {
            throw new NotImplementedException();
        }
    }

    public class Camera
    {
    }

    public interface Transform : IEnumerable
    {
        Transform parent { get; set; }

        GameObject gameObject { get; set; }
        Vector3 localPosition { get; set; }
        Quaternion localRotation { get; set; }
    }

    public class GameObject
    {
        public string name;
        public string tag;
        public Transform transform;

        public T AddComponent<T>()
        {
            throw new NotImplementedException();
        }

        public T GetComponent<T>()
        {
            throw new NotImplementedException();
        }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public float magnitude;

        public Vector3(float x, float y, float z)
        {
            throw new NotImplementedException();
        }
    }

    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Quaternion(float x, float y, float z, float w)
        {
            throw new NotImplementedException();
        }
    }
}
