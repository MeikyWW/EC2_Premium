using DG.Tweening;
using MEC;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeroStatusBar : MonoBehaviour
{
    GameManager gm;
    //=====================================//
    //========== HERO STATUS BAR ==========//
    //=====================================//
    public bool isMainStatusBar;

    [Header("Status Bar")]
    public UILabel nameLabel;
    public UITexture heroPicture;
    public UISlider mainHealthBar;
    public UISlider mainShieldBar;
    public UISlider manaBar, expBar;
    public UILabel levelLabel;
    public GameObject affinity;
    UILabel mainHealthText, manaText, gaugeText;

    [Header("Claris Special Gauge")]
    public UISlider clarisGauge;
    public ParticleSystem clarisRageFx, clarisRageReady;

    [Header("Chase Gauge")]
    public GaugeHUD chaseGauge;
    public ParticleSystem chaseOverdriveReady;
    public GameObject chaseOverdriveLabel;

    [Header("Amy Gauge")]
    public Amy_Gauge amyGauge;

    [Header("Alaster Gauge")]
    public Alaster_Gauge alasterGauge;

    [Header("Edna Gauge")]
    public Edna_Gauge ednaGauge;

    [Header("Louisa Gauge")]
    public Louisa_Gauge louisaGauge;

    [Header("Elze Gauge")]
    public Elze_Gauge elzeGauge;

    [Header("Zerav Gauge")]
    public Zerav_Gauge zeravGauge;

    [SerializeField, HideInEditorMode] private HeroStatus status;
    [SerializeField, HideInEditorMode] private HeroHealth health;
    bool isInited;


    #region SUBSCRIBTIONS 
    private void OnEnable()
    {
        gm = GameManager.instance;
        GameManager.OnGameStateChanged += RageFXOverState;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= RageFXOverState;
    }
    #endregion

    Texture PPDiamond(HeroStatus hero)
    {
        try
        {
            List<CostumeInstance> cos = hero.GetComponent<HeroSaveData>().data.costumes;
            string selectedPP = SelectedPPChanger(cos, hero.heroReference);

            //build dictionary pp changer
            Dictionary<string, HeroReferenceCustomEquip> customs = new Dictionary<string, HeroReferenceCustomEquip>();
            foreach (var item in hero.heroReference.customPP)
                customs.Add(item.equipment.id, item);

            if (!string.IsNullOrEmpty(selectedPP))
            {
                return customs[selectedPP].heroPPDiamond;
            }
            else
            {
                return hero.heroReference.heroPPDiamond;
            }
        }
        catch
        {
            return hero.heroReference.heroPPDiamond;
        }
    }
    Texture SidePortrait(HeroStatus hero)
    {
        try
        {
            List<CostumeInstance> cos = hero.GetComponent<HeroSaveData>().data.costumes;
            string selectedPP = SelectedPPChanger(cos, hero.heroReference);

            //build dictionary pp changer
            Dictionary<string, HeroReferenceCustomEquip> customs = new Dictionary<string, HeroReferenceCustomEquip>();
            foreach (var item in hero.heroReference.customPP)
                customs.Add(item.equipment.id, item);

            if (!string.IsNullOrEmpty(selectedPP))
            {
                return customs[selectedPP].heroSidePortrait;
            }
            else
            {
                return hero.heroReference.heroSidePortrait;
            }
        }
        catch
        {
            return hero.heroReference.heroSidePortrait;
        }
    }
    public string SelectedPPChanger(List<CostumeInstance> _costumes, HeroReference heroref)
    {
        //build all pp changer costume
        List<string> ids = new List<string>();
        foreach (HeroReferenceCustomEquip c in heroref.customPP)
            ids.Add(c.equipment.id);

        foreach (CostumeInstance c in _costumes)
        {
            if (ids.Contains(c.itemData.id))
            {
                return c.itemData.id;
            }
        }
        return null;
    }

    public void Init(HeroStatus heroStatus)
    {
        status = heroStatus;
        health = status.GetComponent<HeroHealth>();
        nameLabel.text = heroStatus.heroReference.hero.ToString();

        ChangeNotif();
        if (isMainStatusBar)
        {
            heroPicture.mainTexture = PPDiamond(heroStatus);// heroStatus.heroReference.heroPPDiamond;
            mainHealthText = mainHealthBar.transform.Find("Label").GetComponent<UILabel>();
            manaText = manaBar.transform.Find("Label").GetComponent<UILabel>();
            gaugeText = clarisGauge.transform.Find("Label").GetComponent<UILabel>();

            DisableAllHeroGauge();

            switch (heroStatus.heroReference.hero)
            {
                case Hero.Claris:
                    affinity.SetActive(true);
                    break;

                case Hero.Chase:
                    chaseGauge.GetComponent<TweenAlpha>().PlayForward();
                    break;

                case Hero.Amy:
                    amyGauge.GetComponent<TweenAlpha>().PlayForward();
                    break;

                case Hero.Alaster:
                    alasterGauge.gameObject.SetActive(true);
                    break;

                case Hero.Edna:
                    ednaGauge.gameObject.SetActive(true);
                    break;

                case Hero.Louisa:
                    louisaGauge.gameObject.SetActive(true);
                    break;

                case Hero.Elze:
                    elzeGauge.gameObject.SetActive(true);
                    break;

                case Hero.Zerav:
                    zeravGauge.gameObject.SetActive(true);
                    break;
            }
        }
        else
        {
            heroPicture.mainTexture = SidePortrait(heroStatus);// heroStatus.heroReference.heroSidePortrait;
            if (health.die)
                SetPictureDie();
            else
                heroPicture.color = Color.white;
        }

        //isInited = true;
    }
    private void DisableAllHeroGauge()
    {
        affinity.SetActive(false);

        chaseGauge.GetComponent<TweenAlpha>().PlayReverse();

        amyGauge.GetComponent<TweenAlpha>().PlayReverse();
        amyGauge.StopAll();

        alasterGauge.RageUIVFX(false);
        alasterGauge.gameObject.SetActive(false);

        ednaGauge.RageUIVFX(false);
        ednaGauge.gameObject.SetActive(false);

        louisaGauge.RageUIVFX(false);
        louisaGauge.gameObject.SetActive(false);

        //elzeGauge.RageUIVFX(false);
        elzeGauge.gameObject.SetActive(false);

        zeravGauge.gameObject.SetActive(false); ;
    }

    public void SetPictureDie()
    {
        if (isMainStatusBar) return;
        heroPicture.color = new Color(0.3301887f, 0.3301887f, 0.3301887f);
    }
    private void Update()
    {
        //Check if allowed using Affinity 
        if (!isMainStatusBar) return;
        if (status.heroReference.hero == Hero.Claris)
            affinity.SetActive(gm.AllowMasterySkill);
    }

    //Level EXP Bar
    float currentExp = 0;
    public void UpdateExpBar(float percent)
    {
        if (!isMainStatusBar) return;
        float x = currentExp;
        DOTween.To(UpdateValueExp, x, percent, 0.5f);
    }
    void UpdateValueExp(float x)
    {
        currentExp = x;
        expBar.value = x;
    }
    public void SetLevel(int level)
    {
        if (!isMainStatusBar) return;
        levelLabel.text = "Lv. " + level.ToString();
    }

    //Health Bar
    float currentHp = 1;
    public void SetHealthText(string s)
    {
        if (!isMainStatusBar) return;
        mainHealthText.text = s;
    }
    public void UpdateHpBar(float percent)
    {
        float x = currentHp;
        DOTween.To(UpdateValueHP, x, percent, 0.5f);
    }
    void UpdateValueHP(float x)
    {
        currentHp = x;
        mainHealthBar.value = x;
    }

    //Shield Bar
    // float currentShield = 0;
    public void UpdateShieldBar(float percent)
    {
        mainShieldBar.value = percent;
        // float x = currentShield;
        // DOTween.To(UpdateValueShield, x, percent, 0.5f);
    }
    void UpdateValueShield(float x)
    {
        // currentShield = x;
        // mainShieldBar.value = x;
    }

    //Mana Bar
    float currentMana = 0;
    public void SetManaText(string s)
    {
        if (!isMainStatusBar) return;
        manaText.text = s;
    }
    public void UpdateManaBar(float percent)
    {
        float x = currentMana;
        DOTween.To(UpdateValueMana, x, percent, 0.5f);
    }
    void UpdateValueMana(float x)
    {
        currentMana = x;
        manaBar.value = x;
    }

    //Hero Gauge
    float currentGaugePoint = 0;
    public void SetGPText(string s)
    {
        if (!isMainStatusBar) return;
        gaugeText.text = s;
    }
    public void UpdateGP(float percent)
    {
        if (!isMainStatusBar) return;
        float x = currentGaugePoint;
        DOTween.To(UpdateValueGP, x, percent, 0.5f);
    }
    void UpdateValueGP(float x)
    {
        currentGaugePoint = x;
        clarisGauge.value = x;
    }

    //Claris : Resonance Gauge
    private bool isRaging;
    public void RageFXOverState(GameState state)
    {
        if (state == GameState.PAUSE)
        {
            if (clarisRageFx)
            {
                clarisRageFx.Stop();
                clarisRageFx.Clear();
            }

        }
        else
        {
            if (isRaging)
            {
                if (clarisRageFx) clarisRageFx.Play();
            }
        }
    }
    public void ClarisRageVfx(bool on)
    {
        if (!isMainStatusBar) return;
        if (on)
        {
            isRaging = true;
            if (clarisRageFx) clarisRageFx.Play();
            if (clarisRageReady) clarisRageReady.Play();
        }
        else
        {
            isRaging = false;
            clarisRageFx.Stop();
            clarisRageFx.Clear();
        }
    }

    //Chase : Overdrive Gauge
    public void ChaseOverdriveVfx(bool on)
    {
        if (!isMainStatusBar) return;
        if (on)
        {
            isRaging = true;
            if (chaseOverdriveReady) chaseOverdriveReady.Play();
            if (chaseOverdriveLabel) chaseOverdriveLabel.SetActive(true);
        }
        else
        {
            isRaging = false;
            if (chaseOverdriveLabel) chaseOverdriveLabel.SetActive(false);
        }
    }

    //Amy : Willpower Gauge
    public void SetAmyGauge(int bpLevel, int fullGauge, float wp)
    {
        if (!isMainStatusBar) return;
        if (amyGauge) amyGauge.SetGauge(bpLevel, fullGauge, wp);
    }

    //Alaster : Valor Gauge
    public void SetAlasterGauge(float percent)
    {
        if (!isMainStatusBar) return;
        if (alasterGauge) alasterGauge.SetGauge(percent);
    }

    //Edna : Heat Gauge
    public void SetEdnaGauge(float percent)
    {
        if (!isMainStatusBar) return;
        if (ednaGauge) ednaGauge.SetGauge(percent);
    }

    //Louisa : Tactical Gauge
    public void SetLouisaGauge(float percent)
    {
        if (!isMainStatusBar) return;
        if (louisaGauge) louisaGauge.SetGauge(percent);
    }

    public void SetElzeGauge(float percent)
    {
        if (!isMainStatusBar) return;
        //if (elzeGauge) elzeGauge.SetGauge(percent);
    }


    [Header("Status Effect Notifier")]
    public UITable notificationContainer;
    public GameObject notificationPrefab;

    public StatusEffectNotification CreateNotif(string statusId, string text, int stack)
    {
        GameObject go = NGUITools.AddChild(notificationContainer.gameObject, notificationPrefab);
        StatusEffectNotification notif = go.GetComponent<StatusEffectNotification>();

        notif.SetNotification(statusId, text, stack);

        notificationContainer.Reposition();

        return notif;
    }

    public void Reposition()
    {
        Timing.RunCoroutine(DelayReposition().CancelWith(gameObject));
    }

    IEnumerator<float> DelayReposition()
    {
        yield return Timing.WaitForOneFrame;
        ResetScale();
        notificationContainer.Reposition();
    }

    public void ChangeNotif()
    {
        Timing.RunCoroutine(WaitNotif().CancelWith(gameObject));
    }

    IEnumerator<float> WaitNotif()
    {
        while (!status.initialized)
        {
            yield return Timing.WaitForOneFrame;
        }

        foreach (var statusNotif in notificationContainer.GetChildList())
            statusNotif.gameObject.SetActive(false);

        var notifs = status.heroStatusEffect.allStatus;
        foreach (var key in notifs.Keys.ToList())
        {
            if (notifs.ContainsKey(key))
            {
                notifs[key].gameObject.transform.parent = notificationContainer.transform;
                notifs[key].gameObject.SetActive(true);
            }
        }

        Reposition();
    }

    public void ResetScale()
    {
        foreach (var statusNotif in notificationContainer.GetChildList())
            statusNotif.localScale = Vector3.one;
    }
}
