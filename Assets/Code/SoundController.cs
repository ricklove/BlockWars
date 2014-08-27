using UnityEngine;
using System.Collections;

public class SoundController : MonoBehaviour
{
  public float threshold = 0;
  public float loudness = 0.0003f;

  void OnCollisionEnter(Collision collision)
  {
    if (collision.gameObject.tag == "Block" || collision.gameObject.name == "Ground" || collision.gameObject.name == "Wall")
    {
      if (audio && !audio.isPlaying)
      {
        var volume = collision.relativeVelocity.sqrMagnitude * loudness;

        if (volume > 0.01f)
        {
          //audio.volume = volume;
          //audio.Play();
          audio.PlayOneShot(audio.clip, volume);
          //Debug.Log("SOUND! " + volume);
        }
      }
    }
  }
}
