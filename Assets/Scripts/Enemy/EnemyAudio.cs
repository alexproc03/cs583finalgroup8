using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyAudio : MonoBehaviour
{
    public AudioClip footstepSound;
    public AudioClip attackSound;

    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.minDistance = 2f;
        _audioSource.maxDistance = 25f;
    }

    public void PlayFootstep()
    {
        if (footstepSound != null) _audioSource.PlayOneShot(footstepSound);
    }

    public void PlayAttack()
    {
        if (attackSound != null) _audioSource.PlayOneShot(attackSound);
    }
}
