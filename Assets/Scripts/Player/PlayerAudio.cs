using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    public AudioClip footstepSound;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip dashSound;
    public AudioClip slideSound;
    public AudioClip grappleSound;
    public AudioClip healSound;

    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.spatialBlend = 0f; // first-person — these are "your" sounds
    }

    public void PlayFootstep() => PlayOne(footstepSound);
    public void PlayJump()     => PlayOne(jumpSound);
    public void PlayLand()     => PlayOne(landSound);
    public void PlayDash()     => PlayOne(dashSound);
    public void PlaySlide()    => PlayOne(slideSound);
    public void PlayGrapple()  => PlayOne(grappleSound);
    public void PlayHeal()     => PlayOne(healSound);

    void PlayOne(AudioClip clip)
    {
        if (clip != null) _audioSource.PlayOneShot(clip);
    }
}
