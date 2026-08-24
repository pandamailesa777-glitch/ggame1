using System;
using UnityEngine;

namespace Nightfall.UnityMvp
{
    public sealed class GameAudioController : MonoBehaviour
    {
        private const int SampleRate=22050;
        private readonly AudioClip[] abilityClips=new AudioClip[3];
        private AudioSource sfxSource,ambientSource;

        private void Awake()
        {
            sfxSource=gameObject.AddComponent<AudioSource>();sfxSource.playOnAwake=false;sfxSource.spatialBlend=0;sfxSource.volume=.34f;
            ambientSource=gameObject.AddComponent<AudioSource>();ambientSource.playOnAwake=false;ambientSource.loop=true;ambientSource.spatialBlend=0;ambientSource.volume=.16f;
            abilityClips[0]=CreateAbilityClip("LightAbility",0,.48f);
            abilityClips[1]=CreateAbilityClip("DeathAbility",1,.58f);
            abilityClips[2]=CreateAbilityClip("LightningAbility",2,.38f);
            ambientSource.clip=Resources.Load<AudioClip>("Audio/Music/nightfall_ambient_suno_v1")??CreateAmbient();ambientSource.Play();
        }

        public void PlayAbility(HeroKind kind,int slot)
        {
            sfxSource.pitch=1+slot*.035f;sfxSource.PlayOneShot(abilityClips[(int)kind],slot>=3?1f:.82f);
        }

        private static AudioClip CreateAbilityClip(string name,int style,float duration)
        {
            int length=Mathf.CeilToInt(SampleRate*duration);var data=new float[length];var random=new System.Random(4100+style);
            for(int i=0;i<length;i++)
            {
                float t=i/(float)SampleRate,n=t/duration,envelope=Mathf.Sin(Mathf.Clamp01(n)*Mathf.PI)*Mathf.Pow(1-n,.55f),sample;
                if(style==0)sample=Mathf.Sin(2*Mathf.PI*(620+420*n)*t)*.55f+Mathf.Sin(2*Mathf.PI*930*t)*.22f;
                else if(style==1)sample=Mathf.Sin(2*Mathf.PI*(105-34*n)*t)*.58f+((float)random.NextDouble()*2-1)*.28f*(1-n);
                else{float gate=Mathf.Sin(t*118)>0?.9f:.18f;sample=((float)random.NextDouble()*2-1)*gate*.58f+Mathf.Sin(2*Mathf.PI*(280+760*n)*t)*.32f;}
                data[i]=Mathf.Clamp(sample*envelope,-1,1);
            }
            var clip=AudioClip.Create(name,length,1,SampleRate,false);clip.SetData(data,0);return clip;
        }

        private static AudioClip CreateAmbient()
        {
            const float duration=24;int length=Mathf.CeilToInt(SampleRate*duration);var data=new float[length];var random=new System.Random(7331);float noise=0;
            for(int i=0;i<length;i++)
            {
                float t=i/(float)SampleRate;noise=Mathf.Lerp(noise,(float)random.NextDouble()*2-1,.0022f);
                float breath=.64f+.36f*Mathf.Sin(2*Mathf.PI*t/duration*2);
                data[i]=(Mathf.Sin(2*Mathf.PI*55*t)*.20f+Mathf.Sin(2*Mathf.PI*82.5f*t)*.11f+noise*.15f)*breath;
            }
            var clip=AudioClip.Create("NightfallTemporaryAmbient",length,1,SampleRate,false);clip.SetData(data,0);return clip;
        }
    }
}
