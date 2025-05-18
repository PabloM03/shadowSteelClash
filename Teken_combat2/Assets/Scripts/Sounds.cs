using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sounds : MonoBehaviour
{
    //public float min = 0f;
    //public float max = 100f;
    private AudioSource fireSound;
    private AudioSource explosionSound;
    private AudioSource shieldSound;
    private AudioSource roarSound;

    // Lista pública para los sonidos de "hurt" (se rellenará en el Inspector)
    public List<AudioSource> hurtSounds;

    // AudioSources para sonidos específicos
    public AudioSource attackSound; // Nuevo AudioSource para sonido de ataque
    private AudioSource dieSound;
    private AudioSource kickSound;

    void Start()
    {
        // Intenta encontrar "fireSound" solo si existe en este objeto
        Transform fireTransform = transform.Find("fireSound");
        if (fireTransform != null)
        {
            fireSound = fireTransform.GetComponent<AudioSource>();
            if (fireSound == null)
                Debug.LogError("El AudioSource para fireSound no se encontró.");
        }

        // Intenta encontrar "explosionSound" solo si existe en este objeto
        Transform explosionTransform = transform.Find("explosionSound");
        if (explosionTransform != null)
        {
            explosionSound = explosionTransform.GetComponent<AudioSource>();
            if (explosionSound == null)
                Debug.LogError("El AudioSource para explosionSound no se encontró.");
        }

        // Intenta encontrar "shieldSound" solo si existe en este objeto
        Transform shieldTransform = transform.Find("shieldSound");
        if (shieldTransform != null)
        {
            shieldSound = shieldTransform.GetComponent<AudioSource>();
            if (shieldSound == null)
                Debug.LogError("El AudioSource para shieldSound no se encontró.");
        }

        // Intenta encontrar "roarSound" solo si existe en este objeto
        Transform roarTransform = transform.Find("roarSound");
        if (roarTransform != null)
        {
            roarSound = roarTransform.GetComponent<AudioSource>();
            if (roarSound == null)
                Debug.LogError("El AudioSource para roarSound no se encontró.");
        }

        // Intenta encontrar "dieSound" solo si existe en este objeto
        Transform dieTransform = transform.Find("dieSound");
        if (dieTransform != null)
        {
            dieSound = dieTransform.GetComponent<AudioSource>();
            if (dieSound == null)
                Debug.LogError("El AudioSource para dieSound no se encontró.");
        }

        // Intenta encontrar "kickSound" solo si existe en este objeto
        Transform kickTransform = transform.Find("kickSound");
        if (kickTransform != null)
        {
            kickSound = kickTransform.GetComponent<AudioSource>();
            if (kickSound == null)
                Debug.LogError("El AudioSource para kickSound no se encontró.");
        }
    }

    public void FireSound()
    {
        if (fireSound != null)
        {
            fireSound.Stop();
            PlayAudio(fireSound, 0.4f, 3f);
        }
        else
        {
            Debug.LogWarning("fireSound no está asignado en este objeto.");
        }
    }

    public void ExplosionSound()
    {
        if (explosionSound != null)
        {
            explosionSound.Stop();
            PlayAudio(explosionSound, 0f, 2.5f);
        }
        else
        {
            Debug.LogWarning("explosionSound no está asignado en este objeto.");
        }
    }

    public void ShieldSound()
    {
        if (shieldSound != null)
        {
            shieldSound.Stop();
            PlayAudio(shieldSound, 0f, 2f);
        }
        else
        {
            Debug.LogWarning("shieldSound no está asignado en este objeto.");
        }
    }

    public void RoarSound(int active)
    {
        roarSound.Stop();
        if (active != 0)
        {
            if (roarSound != null)
            {
                PlayAudio(roarSound, 0f, 4f);
            }
            else
            {
                Debug.LogWarning("roarSound no está asignado en este objeto.");
            }
        }
    }

    public void HurtSound()
    {
        if (hurtSounds != null && hurtSounds.Count > 0)
        {
            // Selecciona un AudioSource aleatorio de la lista hurtSounds
            int randomIndex = Random.Range(0, hurtSounds.Count);
            AudioSource selectedHurtSound = hurtSounds[randomIndex];

            // Reproduce el sonido seleccionado
            if (selectedHurtSound != null)
            {
                selectedHurtSound.Stop();
                PlayAudio(selectedHurtSound, 0f, 1.5f);
            }
            else
            {
                Debug.LogWarning("Uno de los hurtSounds no está asignado.");
            }
        }
        else
        {
            Debug.LogWarning("No hay sonidos de hurt asignados.");
        }
    }

    public void AttackSound(int type)
    {
        switch (type)
        {
            case 1: // Reproduce attackSound con pitch aleatorio
                if (attackSound != null)
                {
                    attackSound.pitch += Random.Range(-0.5f, 0.5f);
                    PlayAudio(attackSound, 0f, 2f);
                }
                else
                {
                    Debug.LogWarning("attackSound no está asignado en este objeto.");
                }
                break;
            case 2: // Sonido específico 2
          	
		    KickSound();
                break;

            default:
                Debug.LogWarning("Tipo de ataque no válido.");
                break;
        }
    }

    public void DieSound()
    {
        if (dieSound != null)
        {
            dieSound.Stop();
            PlayAudio(dieSound, 0f, 2f);
        }
        else
        {
            Debug.LogWarning("dieSound no está asignado en este objeto.");
        }
    }

    public void KickSound()
    {
        if (kickSound != null)
        {
            kickSound.Stop();
            PlayAudio(kickSound, 0f, 2f);
        }
        else
        {
            Debug.LogWarning("kickSound no está asignado en este objeto.");
        }
    }

    // Método para reproducir el audio desde un punto de inicio y por una duración específica
    private void PlayAudio(AudioSource audioSource, float startTime, float duration)
    {
        audioSource.time = startTime;
        audioSource.Play();
        StartCoroutine(StopAudioAfterDuration(audioSource, duration));
    }

    // Corutina para detener el audio después de la duración especificada
    private IEnumerator StopAudioAfterDuration(AudioSource audioSource, float duration)
    {
        yield return new WaitForSeconds(duration);
        audioSource.Stop();
    }
}
