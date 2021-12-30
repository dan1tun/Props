using System;
using System.Collections.Generic;
using UnityEngine;

//hack para tener el diccionario en el inspector
[Serializable]
public struct DictAudio
{
    public Enums.SoundType type;
    public AudioClip audio;
}
public class AudioManager : MonoBehaviour
{
    [SerializeField] private DictAudio[] sounds;
    private Dictionary<Enums.SoundType, AudioClip> audioDictionary = new  Dictionary<Enums.SoundType, AudioClip>();
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        //inicializamos audioSource
        audioSource = GetComponent<AudioSource>();

        //pasamos lo del inspector al diccionario
        foreach (DictAudio audio in sounds)
            audioDictionary.Add(audio.type, audio.audio);
    }

    public void PlayAudio(Enums.SoundType type)
    {
        audioSource.PlayOneShot(audioDictionary[type]);
    }
}
