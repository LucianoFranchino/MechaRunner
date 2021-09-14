using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class OpcionesMenu : MonoBehaviour
{

    public AudioMixer audioMixer;
    public void menuOpciones (float volume){
        audioMixer.SetFloat("Volume", volume);
    }
}
