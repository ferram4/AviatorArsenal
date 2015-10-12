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

        [KSPField(isPersistant = false, guiActive = false)]
        public float meshSwitchRPM = 300;

        [KSPField(isPersistant = false, guiActive = false)]
        public float cutoffRPM = 1000;

        [KSPField(isPersistant = false, guiActive = false)]
        public float maxRPM = 2000;

        [KSPField(isPersistant = false, guiActive = false)]
        public float diskRPMFactor = 0.01f;

        //controls rate of change of RPM as a function of dyn pres
        [KSPField(isPersistant = false, guiActive = false)]
        public float dynPresScalingFactor = 0.0001f;

        [KSPField(isPersistant = false, guiActive = false)]
        public float frictionDecayFactor = 0.01f;

        [KSPField(isPersistant = false, guiActive = false)]
        public float maxVolume = 1;

        [KSPField(isPersistant = false, guiActive = false)]
        public string audioClipName = "";

        [KSPField(isPersistant = false, guiActive = false)]
        public string propellerDiscreteTransformName = "";

        [KSPField(isPersistant = false, guiActive = false)]
        public string propellerDiskTransformName = "";

        [KSPField(isPersistant = false, guiActive = false)]
        public string propAxisTransformName = "";

        [KSPField(isPersistant = false, guiActive = false)]
        public Vector3 partLocalOrientationVector = Vector3.up;

        Transform propellerDiscreteTransform;
        Transform propellerDiskTransform;
        Transform propAxisTransform;

        AudioSource trumpetSource;
        AudioClip trumpetClip;

        float RPMPerVelocity;
        float currentRPM;
        bool propDiskVisible = false;

        bool ready = false;

        #region Init Methods
        void InitTransforms()
        {
            if (propellerDiscreteTransformName != String.Empty)
            {
                propellerDiscreteTransform = part.FindModelTransform(propellerDiscreteTransformName);
                if(propellerDiscreteTransform == null)
                    Debug.LogError("AviatorArsenal JerichoTrumpet module could not find the specified discrete propeller transform");

            }
            else
            {
                this.enabled = false;
                Debug.LogError("AviatorArsenal JerichoTrumpet module does not have a discrete propeller transform specified");
            }

            if (propellerDiskTransformName != String.Empty)
            {
                propellerDiskTransform = part.FindModelTransform(propellerDiskTransformName);
                if (propellerDiscreteTransform == null)
                    Debug.LogError("AviatorArsenal JerichoTrumpet module could not find the specified disk transform");

            }
            else
            {
                this.enabled = false;
                Debug.LogError("AviatorArsenal JerichoTrumpet module does not have a disk transform specified");
            }

            if (propAxisTransformName != String.Empty)
            {
                propAxisTransform = part.FindModelTransform(propAxisTransformName);
                if (propellerDiscreteTransform == null)
                    Debug.LogError("AviatorArsenal JerichoTrumpet module could not find the specified prop axis transform");

            }
            else
            {
                this.enabled = false;
                Debug.LogError("AviatorArsenal JerichoTrumpet module does not have a prop axis transform specified");
            }

            propellerDiskTransform.gameObject.SetActive(false);
        }

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

        void CalculateRuntimeVariables()
        {
            RPMPerVelocity = cutoffRPM / cutoffVelocity;
        }
        #endregion

        #region Runtime Physics Methods

        //change in rotation rate due to 
        void FixedUpdateVelocityRPM()
        {
            float velocity = Vector3.Dot(vessel.srf_velocity, propAxisTransform.forward);
            float targetRPM = RPMPerVelocity * velocity;

            //calculates exponential decay towards target speed
            float errorRPM = targetRPM - currentRPM;
            float recip_timeconstant = (float)vessel.dynamicPressurekPa * dynPresScalingFactor;     //as dynPres increases, rate of change increases
            float tmp1 = errorRPM * recip_timeconstant;
            float tmp2 = Math.Abs(0.6f * errorRPM);     //limits to prevent overshoot due to large timesteps

            if (tmp1 < -tmp2)
                tmp1 = -tmp2;
            if (tmp1 > tmp2)
                tmp1 = tmp2;

            currentRPM += TimeWarp.fixedDeltaTime * tmp1;
        }

        void FixedUpdateFrictionRPM()
        {
            float errorRPM = -currentRPM;
            float tmp1 = errorRPM * frictionDecayFactor;
            float tmp2 = Math.Abs(0.6f * errorRPM);     //limits to prevent overshoot due to large timesteps

            if (tmp1 < -tmp2)
                tmp1 = -tmp2;
            if (tmp1 > tmp2)
                tmp1 = tmp2;

            currentRPM += TimeWarp.fixedDeltaTime * tmp1;
        }
        
        void FixedUpdateVelocityClamp()
        {
            if (currentRPM > maxRPM)
                currentRPM = maxRPM;
            if (currentRPM < -maxRPM)
                currentRPM = -maxRPM;
        }

        #endregion

        #region Runtime Visual and Sound Methods
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
            float rpmFactor;

            rpmFactor = currentRPM - cutoffRPM;

            rpmFactor *= 0.001f;

            if (rpmFactor <= 0)
                return 0;

            if (rpmFactor > 1)
                rpmFactor = 1;

            return maxVolume * rpmFactor;
        }

        void UpdateAnimation()
        {
            SetVisibleTransform();
            if (propDiskVisible)
            {
                Quaternion rotation = Quaternion.AngleAxis(currentRPM * diskRPMFactor, Vector3.forward);
                propellerDiskTransform.rotation *= rotation;
            }
            else
            {
                Quaternion rotation = Quaternion.AngleAxis(currentRPM, Vector3.forward);
                propellerDiscreteTransform.rotation *= rotation;
            }

        }

        void SetVisibleTransform()
        {
            if (propDiskVisible)
            {
                if (currentRPM < meshSwitchRPM)
                {
                    propellerDiscreteTransform.gameObject.SetActive(true);
                    propellerDiskTransform.gameObject.SetActive(false);
                    propDiskVisible = false;
                }
            }
            else
            {
                if (currentRPM > meshSwitchRPM)
                {
                    propellerDiscreteTransform.gameObject.SetActive(false);
                    propellerDiskTransform.gameObject.SetActive(true);
                    propDiskVisible = true;
                }
            }
        }

        #endregion
        #region Unity Methods
        void Start()
        {
            InitAudio();
            InitTransforms();
            CalculateRuntimeVariables();
            ready = true;
        }

        void Update()
        {
            if (HighLogic.LoadedSceneIsFlight && ready)
            {
                UpdateAnimation();
                UpdateSound();
            }
        }

        void FixedUpdate()
        {
            if(HighLogic.LoadedSceneIsFlight && ready)
            {
                FixedUpdateFrictionRPM();
                FixedUpdateVelocityRPM();
                FixedUpdateVelocityClamp();
            }
        }
        #endregion
    }
}
