//AviatorArsenal code is copyright 2015, All Rights Reserved, Michael Ferrara, aka ferram4
using System;
using UnityEngine;
using KSP;

namespace AviatorArsenal
{
    /// <summary>
    /// A PartModule that will play a repeating sound effect when the local dynamic pressure and velocity are above a certain amount
    /// </summary>
    public class JerichoTrumpet : PartModule
    {
        //cutoff velocity in m/s
        [KSPField(isPersistant = false, guiActive = false)]
        public float cutoffVelocity = 110;

        //cutoff dyn pres in KPa
        [KSPField(isPersistant = false, guiActive = false)]
        public float cutoffDynPresKPA = 7.5f;

        [KSPField(isPersistant = false, guiActive = false)]
        public float maxVolume = 1;

        [KSPField(isPersistant = false, guiActive = false)]
        public string audioClipName = "";

        AudioSource trumpetSource;
        AudioClip trumpetClip;

        bool ready = false;

        #region Init Methods
        void InitAudio()
        {
            if (audioClipName != String.Empty)
            {
                trumpetClip = GameDatabase.Instance.GetAudioClip(audioClipName);

                trumpetSource = gameObject.AddComponent<AudioSource>();
                trumpetSource.dopplerLevel = 1;
                trumpetSource.priority = 5;
                trumpetSource.bypassListenerEffects = true;
                trumpetSource.minDistance = 0.1f;
                trumpetSource.maxDistance = 2000;
                trumpetSource.volume = 0;
                trumpetSource.loop = true;

                trumpetSource.clip = trumpetClip;
            }
            else
            {
                this.enabled = false;
                Debug.LogError("AviatorArsenal JerichoTrumpet module does not have a sound specified");
            }
        }
        #endregion

        #region Runtime Methods
        void UpdateSound()
        {
            float curVolume = CalculateVolume();
            if(curVolume > 0)
            {
                trumpetSource.volume = curVolume;
                if(!trumpetSource.isPlaying)
                {
                    trumpetSource.Play();
                }
            }
            else
            {
                if (trumpetSource.isPlaying)
                {
                    trumpetSource.volume = 0;
                    trumpetSource.Stop();
                }
            }
        }

        float CalculateVolume()
        {
            float dynPresFactor, velFactor;

            dynPresFactor = (float)vessel.dynamicPressurekPa;

            dynPresFactor -= cutoffDynPresKPA;
            dynPresFactor *= 0.1f;

            if (dynPresFactor <= 0)
                return 0;

            velFactor = (float)vessel.srfSpeed;

            velFactor -= cutoffVelocity;
            velFactor *= 10;

            if (velFactor <= 0)
                return 0;

            float volumeFactor = dynPresFactor * velFactor;

            if (volumeFactor > 1)
                volumeFactor = 1;

            return maxVolume * volumeFactor;
        }

        #endregion
        #region Unity Methods
        void Start()
        {
            InitAudio();
            ready = true;
        }

        void Update()
        {
            if(HighLogic.LoadedSceneIsFlight && ready)
                UpdateSound();
        }
        #endregion
    }
}
