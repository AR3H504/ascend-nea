using UnityEngine;

public static class GameAudio
{
    public const float ItemPickupVolume = 1.75f;

    private static AudioClip jumpClip;
    private static AudioClip landingClip;
    private static AudioClip slideClip;
    private static AudioClip footstepClip;
    private static AudioClip itemPickupClip;
    private static AudioClip checkpointClip;

    public static AudioClip JumpClip => jumpClip ??= Resources.Load<AudioClip>("Audio/Jump Sound Effect");
    public static AudioClip LandingClip => landingClip ??= Resources.Load<AudioClip>("Audio/Landing Effect");
    public static AudioClip SlideClip => slideClip ??= Resources.Load<AudioClip>("Audio/Slide Sound Effect");
    public static AudioClip FootstepClip => footstepClip ??= Resources.Load<AudioClip>("Audio/Concrete Footsteps sound effect SFX");
    public static AudioClip ItemPickupClip => itemPickupClip ??= Resources.Load<AudioClip>("Audio/Item Pick up");
    public static AudioClip CheckpointClip => checkpointClip ??= Resources.Load<AudioClip>("Audio/Checkpoint Sound Effect");

    public static void PlayItemPickup(Vector3 worldPosition)
    {
        if (ItemPickupClip == null)
        {
            return;
        }

        GameObject audioObject = new GameObject("ItemPickupOneShot");
        audioObject.transform.position = worldPosition;

        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        audioSource.PlayOneShot(ItemPickupClip, ItemPickupVolume);
        Object.Destroy(audioObject, ItemPickupClip.length + 0.1f);
    }
}
