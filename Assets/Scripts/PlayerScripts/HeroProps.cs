using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using MEC;

public class HeroProps : MonoBehaviour
{
    public bool startReady;

    [FoldoutGroup("Weapons")]
    public bool notUsingWeaponAttachment;
    [FoldoutGroup("Weapons")]
    public bool showHideWeapon;
    [FoldoutGroup("Weapons")]
    public Transform[] weaponObject;
    [FoldoutGroup("Weapons")]
    public Transform[] sheathePos, readyPos;


    [FoldoutGroup("Weapons"), LabelText("Change aura position")]
    public bool aura;
    [FoldoutGroup("Weapons"), ShowIf("aura")]
    public Transform[] weaponAura, sheatheAura, readyAura;

    [FoldoutGroup("Effects"), ListDrawerSettings(ShowIndexLabels = true)]
    public ParticleSystem[] vfx;
    [FoldoutGroup("Effects")]
    public ParticleSystem switchEffect;

    [FoldoutGroup("Audio")]
    public AudioSource sfxSrc;
    [FoldoutGroup("Audio"), ListDrawerSettings(ShowIndexLabels = true)]
    public AudioClip[] sfx, sfx2;
    [FoldoutGroup("Audio")]
    public AudioSource voiceSrc;
    [FoldoutGroup("Audio")]
    public AudioClip[] voice1, voice2, voice3, hitClip;
    [FoldoutGroup("Audio")]
    public AudioClip[] dieClip;
    [FoldoutGroup("Audio")]
    public AudioClip[] switchClip;
    [FoldoutGroup("Audio")]
    public AudioClip[] perfectEvasionClip;
    [FoldoutGroup("Audio")]
    public AudioClip[] addedToPartyClip;
    [FoldoutGroup("Audio")]
    public List<HeroTargetClip> addedToPartySpecificClip;
    [FoldoutGroup("Audio")]
    public AudioClip[] lowHealthClip;
    [FoldoutGroup("Audio")]
    public AudioClip[] treasureOpenClip;
    [FoldoutGroup("Audio")]
    public AudioClip[] treasureSpottedClip;
    [FoldoutGroup("Audio")]
    public AudioClip startRage;
    [FoldoutGroup("Audio")]
    public AudioClip endRage;

    [FoldoutGroup("Misc")]
    public HeroFallChecker fallCheck;
    [FoldoutGroup("Misc")]
    public GameObject[] lamp;
    [FoldoutGroup("Misc")]
    public GameObject[] visibleMeshes;
    private Animator _animator;
    private HeroStatus _status;
    private CombatCamera _cam;
    private GameManager gm;

    bool inited;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        if (inited) return;

        _animator = GetComponent<Animator>();
        _status = GetComponent<HeroStatus>();
        _cam = CombatCamera.instance;
        gm = GameManager.instance;

        if (startReady) AttachWeaponToHand();
        else AttachWeaponToSheath();
        SetAnimatorLayer();

        inited = true;
    }

    public void CheckStance()
    {
        if (gm = null) gm = GameManager.instance;

        if (gm)
            SetStance(!gm.activeAreaNode.nonCombatArea, true);
    }

    public void SetStance(bool isCombat)
    {
        SetStance(isCombat, false);
    }
    public void SetStance(bool isCombat, bool force)
    {
        ProceedChangeStance(isCombat, force);
    }

    public void ProceedChangeStance(bool isCombat, bool force)
    {
        Init();
        Timing.RunCoroutine(WaitUsingSkill(isCombat, force).CancelWith(gameObject));
    }

    IEnumerator<float> WaitUsingSkill(bool isCombat, bool force)
    {
        while (!_status.initialized)
        {
            yield return Timing.WaitForOneFrame;
        }

        while (_status.control.IsUsingSkill())
        {
            yield return Timing.WaitForOneFrame;
        }

        if (isCombat) Ready(force);
        else Sheathe(force);
    }
    public void Sheathe()
    {
        Sheathe(false);
    }
    public void Ready()
    {
        Ready(false);
    }
    public void Sheathe(bool force)
    {
        if (_status.combatStat == CombatStatus.OffCombat && !force) return;

        //Debug.Log($"{_status.heroReference.hero} Combat : OFF");

        _status.combatStat = CombatStatus.OffCombat;

        AttachWeaponToSheath();
        SetAnimatorLayer();

        GetComponent<HeroControl>().GoToSafeArea();
    }

    public void Ready(bool force)
    {
        if (_status.combatStat == CombatStatus.OnCombat && !force) return;

        //Debug.Log($"{_status.heroReference.hero} Combat : ON");

        _status.combatStat = CombatStatus.OnCombat;

        AttachWeaponToHand();
        SetAnimatorLayer();
        GetComponent<HeroControl>().GoToCombatArea();
    }

    public void HideWeapon()
    {
        if (showHideWeapon)
        {
            for (int i = 0; i < sheathePos.Length; i++)
                sheathePos[i].gameObject.SetActive(false);
            for (int i = 0; i < readyPos.Length; i++)
                readyPos[i].gameObject.SetActive(false);
        }
        else
        {
            for (int i = 0; i < weaponObject.Length; i++)
                weaponObject[i].gameObject.SetActive(false);
            if (aura)
                for (int i = 0; i < weaponAura.Length; i++)
                    weaponAura[i].gameObject.SetActive(false);
        }
    }
    public void ShowWeapon()
    {
        for (int i = 0; i < weaponObject.Length; i++)
            weaponObject[i].gameObject.SetActive(true);
        if (aura)
            for (int i = 0; i < weaponAura.Length; i++)
                weaponAura[i].gameObject.SetActive(true);
    }

    bool toCombat;
    public void AttachWeaponToSheath()
    {
        toCombat = false;

        if (notUsingWeaponAttachment) return;

        if (showHideWeapon)
        {
            for (int i = 0; i < sheathePos.Length; i++)
            {
                sheathePos[i].gameObject.SetActive(true);
            }
            for (int i = 0; i < readyPos.Length; i++)
            {
                readyPos[i].gameObject.SetActive(false);
            }
        }
        else
        {
            for (int i = 0; i < weaponObject.Length; i++)
            {
                weaponObject[i].parent = sheathePos[i];
                weaponObject[i].localPosition = Vector3.zero;
                weaponObject[i].localRotation = Quaternion.identity;
                weaponObject[i].localScale = Vector3.one;
            }
        }

        //AURA
        if (aura)
        {
            for (int i = 0; i < weaponAura.Length; i++)
            {
                weaponAura[i].parent = sheatheAura[i];
                weaponAura[i].localPosition = Vector3.zero;
                weaponAura[i].localRotation = Quaternion.identity;
                weaponAura[i].localScale = Vector3.one;
            }
        }
    }
    public void AttachWeaponToHand()
    {
        toCombat = true;

        if (notUsingWeaponAttachment) return;

        if (showHideWeapon)
        {
            for (int i = 0; i < sheathePos.Length; i++)
            {
                sheathePos[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < readyPos.Length; i++)
            {
                readyPos[i].gameObject.SetActive(true);
            }
        }
        else
        {
            for (int i = 0; i < weaponObject.Length; i++)
            {
                weaponObject[i].parent = readyPos[i];
                weaponObject[i].localPosition = Vector3.zero;
                weaponObject[i].localRotation = Quaternion.identity;
                weaponObject[i].localScale = Vector3.one;
            }
        }

        //AURA
        if (aura)
        {
            for (int i = 0; i < weaponAura.Length; i++)
            {
                weaponAura[i].parent = readyAura[i];
                weaponAura[i].localPosition = Vector3.zero;
                weaponAura[i].localRotation = Quaternion.identity;
                weaponAura[i].localScale = Vector3.one;
            }
        }
    }
    #region listener
    void SetAnimatorLayer()
    {
        if (toCombat) SetAnimator_CombatMode();
        else SetAnimator_SafeMode();
    }
    void SetAnimator_CombatMode()
    {
        _animator.SetLayerWeight(1, 0);
        _animator.SetLayerWeight(2, 1);
        _status.combatStat = CombatStatus.OnCombat;
    }
    void SetAnimator_SafeMode()
    {
        _animator.SetLayerWeight(1, 1);
        _animator.SetLayerWeight(2, 0);
        _status.combatStat = CombatStatus.OffCombat;
    }

    public void PlayStartRageSFX()
    {
        if (startRage)
        {
            if (sfxSrc) sfxSrc.PlayOneShot(startRage);
        }
    }
    public void PlayEndRageSFX()
    {
        if (endRage)
        {
            if (sfxSrc) sfxSrc.PlayOneShot(endRage);
        }
    }

    public void PlayVFX(int idx)
    {
        vfx[idx].Play();
    }
    public void PlaySFX(int idx)
    {
        if (sfxSrc) sfxSrc.PlayOneShot(sfx[idx]);
    }
    public void PlayRandomSfx()
    {
        if (sfxSrc) sfxSrc.PlayOneShot(sfx[Random.Range(0, sfx.Length)]);
    }
    public void PlaySFX2(int idx)
    {
        if (sfxSrc) sfxSrc.PlayOneShot(sfx2[idx]);
    }
    public void PlayRandomSfx2()
    {
        if (sfxSrc) sfxSrc.PlayOneShot(sfx2[Random.Range(0, sfx2.Length)]);
    }
    public void PlayVoice1(int idx)
    {
        if (voiceSrc)
        {
            if (voice1[idx] != null)
                voiceSrc.PlayOneShot(voice1[idx]);
        }
    }
    public void PlayRandomVoice1()
    {
        if (voiceSrc) voiceSrc.PlayOneShot(voice1[Random.Range(0, voice1.Length)]);
    }
    public void PlayRandomVoice1WithChance()
    {
        if (voiceSrc)
        {
            if (Random.Range(0, 100) < 60)
                voiceSrc.PlayOneShot(voice1[Random.Range(0, voice1.Length)]);
        }
    }
    public void PlayVoice2(int idx)
    {
        if (voiceSrc) voiceSrc.PlayOneShot(voice2[idx]);
    }

    int v2lastIndex;
    public void PlayRandomVoice2()
    {
        if (voiceSrc)
        {
            int random = -1;

            do
            {
                random = Random.Range(0, voice2.Length);
            } while (random == v2lastIndex);

            v2lastIndex = random;
            voiceSrc.PlayOneShot(voice2[v2lastIndex]);
        }
    }

    int v3lastIndex;
    public void PlayRandomVoice3()
    {
        if (voiceSrc)
        {
            int random = -1;

            do
            {
                random = Random.Range(0, voice3.Length);
            } while (random == v3lastIndex);

            v3lastIndex = random;
            voiceSrc.PlayOneShot(voice3[v3lastIndex]);
        }
    }
    public void PlayVoice(AudioClip voice)
    {
        if (voice == null) return;
        if (voiceSrc) voiceSrc.PlayOneShot(voice);
    }
    public void PlayVoice(AudioClip voice, float chance)
    {
        if (Random.Range(0, 100) > chance) return;
        PlayVoice(voice);
    }
    public void PlayVoice(AudioClip[] voice)
    {
        if (voice == null) return;
        if (voice.Length == 0) return;
        if (voiceSrc)
        {
            AudioClip clip = voice[Random.Range(0, voice.Length)];
            if (clip) voiceSrc.PlayOneShot(clip);
        }
    }
    public void PlayVoice(AudioClip[] voice, float chance)
    {
        if (Random.Range(0, 100) > chance) return;
        PlayVoice(voice);
    }

    public void PlayHit()
    {
        if (hitClip.Length == 0) return;

        if (voiceSrc)
        {
            //if (Random.Range(0, 100) < 60)
            voiceSrc.PlayOneShot(hitClip[Random.Range(0, hitClip.Length)]);
        }
    }
    public void StopVFX(int idx)
    {
        vfx[idx].Stop();
    }
    public void PlayDie()
    {
        if (!voiceSrc) return;
        if (dieClip.Length == 0) return;

        voiceSrc.PlayOneShot(dieClip[Random.Range(0, dieClip.Length)]);
    }

    public void PlaySwitch()
    {
        if (!voiceSrc) return;
        if (switchClip.Length == 0) return;
        voiceSrc.PlayOneShot(switchClip[Random.Range(0, switchClip.Length)]);
    }

    float delayPFEvaVoice = 3f;
    bool disablePFEvaVoice;
    public void PlayPerfectEvasion()
    {
        if (!voiceSrc) return;
        if (perfectEvasionClip.Length == 0) return;
        if (disablePFEvaVoice) return;

        if (Random.Range(0, 100) < 35)
        {
            disablePFEvaVoice = true;
            Timing.RunCoroutine(DelayAndDo(delayPFEvaVoice, () => { disablePFEvaVoice = false; }));
            voiceSrc.PlayOneShot(perfectEvasionClip[Random.Range(0, perfectEvasionClip.Length)]);
        }
    }

    public void ForceStopVoiceSource()
    {
        if (!voiceSrc) return;
        voiceSrc.Stop();
    }
    public bool PlayLowHealth()
    {
        if (!voiceSrc) return false;
        if (lowHealthClip.Length == 0) return false;

        voiceSrc.PlayOneShot(lowHealthClip[Random.Range(0, lowHealthClip.Length)]);
        return true;
    }

    float delayTreasureOpenVoice = 15f;
    bool disableTreasureOpenVoice;
    public void PlayTreasureOpen()
    {
        if (!voiceSrc) return;
        if (treasureOpenClip.Length == 0) return;
        if (disableTreasureOpenVoice) return;
        if (voiceSrc.isPlaying) return;

        disableTreasureOpenVoice = true;
        Timing.RunCoroutine(DelayAndDo(delayTreasureOpenVoice, () => { disableTreasureOpenVoice = false; }));
        voiceSrc.PlayOneShot(treasureOpenClip[Random.Range(0, treasureOpenClip.Length)]);
    }

    float delayTreasureSpottedVoice = 25f;
    bool disableTreasureSpottedVoice;
    public void PlayTreasureSpotted()
    {
        if (!voiceSrc) return;
        if (treasureSpottedClip.Length == 0) return;
        if (disableTreasureSpottedVoice) return;
        if (voiceSrc.isPlaying) return;

        disableTreasureSpottedVoice = true;
        Timing.RunCoroutine(DelayAndDo(delayTreasureSpottedVoice, () => { disableTreasureSpottedVoice = false; }));
        voiceSrc.PlayOneShot(treasureSpottedClip[Random.Range(0, treasureSpottedClip.Length)]);
    }

    public IEnumerator<float> DelayAndDo(float delay, System.Action afterDelay)
    {
        yield return Timing.WaitForSeconds(delay);
        afterDelay?.Invoke();
    }

    #endregion

    //Hero Camera Shake
    //beberapa dipanggil dari animator. jgn di delete.
    void ShakeTiny()
    {
        _cam.TinyShake();
    }
    void ShakeLight()
    {
        _cam.LightShake();
    }
    void ShakeLightWithTimeBreak()
    {
        gm.TimeBreak();
        _cam.MediumShake();
    }
    void MediumShake()
    {
        CombatCamera.instance.MediumShake();
    }

    public void SwitchEffect()
    {
        if (switchEffect != null)
            switchEffect.Play();
    }

    public void CheckLamp(bool value)
    {
        for (int i = 0; i < lamp.Length; i++)
            lamp[i].SetActive(value);
    }
    public void CheckVisibleMeshes(bool value)
    {
        for (int i = 0; i < visibleMeshes.Length; i++)
        {
            visibleMeshes[i].SetActive(value);
        }
        sfxSrc.mute = !value;
        voiceSrc.mute = !value;
        try
        {
            GetComponent<HeroStep>().stepCaster.GetComponent<AudioSource>().mute = !value;
        }
        catch { }
    }

    public AudioClip GetJoinToPartyClip(Hero target)
    {
        if(target == Hero.None)
        {
            if (addedToPartyClip.Length == 0) return null;
            else return addedToPartyClip[Random.Range(0, addedToPartyClip.Length)];
        }
        else
        {
            var getTarget = addedToPartySpecificClip.Find(x => x.target == target);
            AudioClip targetClip = null;
            if(getTarget != null)
            {
                targetClip = getTarget.clips.Count > 0 ?
                    getTarget.clips[Random.Range(0, getTarget.clips.Count)] : null;
            }

            var normalClip = addedToPartyClip.Length > 0 ?
                addedToPartyClip[Random.Range(0, addedToPartyClip.Length)] : null;
            var useTargetClip = Random.Range(0, 2) == 0;

            if(useTargetClip)
            {
                if (targetClip) return targetClip;
                else if (normalClip) return normalClip;
            }
            else
            {
                if (normalClip) return normalClip;
                else if (targetClip) return targetClip;
            }

            return null;
        }
    }
}

[System.Serializable]
public class HeroTargetClip
{
    public Hero target;
    public List<AudioClip> clips;
}