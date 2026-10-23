using UnityEngine;
namespace Snowglow {
 public sealed class SynthAudio : MonoBehaviour {
  public AudioSource effects,motor,ambience;public AudioClip[] notes;public AudioClip chime;
  float next;public void SetMuted(bool muted){effects.mute=motor.mute=ambience.mute=muted;}
  public void Engine(float amount){motor.volume=Mathf.Clamp01(amount)*.12f;motor.pitch=.65f+amount;}
  public void Clear(int multiplier){if(Time.time<next)return;next=Time.time+.12f;effects.PlayOneShot(notes[Mathf.Clamp(multiplier-1,0,notes.Length-1)],.3f);}
  public void Chime(){effects.PlayOneShot(chime,.5f);}
 }
}
