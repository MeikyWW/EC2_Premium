using UnityEngine;
using EZ_Pooling;

public class HudManager : MonoBehaviour {

    public static HudManager instance;

    public GameObject m_uiroot;
    public GameObject m_behindUI;
    public UIWidget gameplayTexture;

    public GameObject m_enemyDamage, m_playerDamage, m_customTicker;

    [Header("Potions")]
    public GameObject m_healHp;
    public GameObject m_healMp;

    [Header("Special")]
    public GameObject m_bleedDamage;
    public GameObject m_poisonDamage;
    public GameObject m_burnDamage, m_frostbiteDmg;
    public GameObject p_miss, p_resist, p_guard;
    public GameObject m_exp;
    public GameObject sigil_ticker;

    [Header("Enemy")]
    public GameObject batBite;
    public GameObject m_enemyDamageCustom;

    [Header("NPC")]
    public GameObject npcGUI;
    public GameObject playerOnlineGUI;

    [Header("Hero")]
    public GameObject amy_heavyblow;
    public GameObject louisa_perfect;
    public GameObject louisa_bullseye;

    GameManager gm;
    Vector3 vOne = Vector3.one;

	private void Start() 
    {
        instance = this;
        gm = GetComponent<GameManager>();
	}

    public void PopDamagePlayer(Transform target, float y_offset, float value)
    {
        Transform damage = EZ_PoolManager.Spawn(m_playerDamage.transform, vOne, Quaternion.identity, m_uiroot.transform);
        //Transform damage = NGUITools.AddChild(m_uiroot, m_playerDamage).transform;
        NGUIMath.OverlayPosition(damage, target.position + Vector3.up * y_offset, Camera.main, UICamera.mainCamera);
        damage.localPosition = new Vector3(damage.localPosition.x, damage.localPosition.y, 0);
        damage.localScale = vOne;
        damage.GetComponent<DamageHUD>().Pop(value, false);
    }
    public void PopDamageEnemy(Transform target, float y_offset, float value, bool critical)
    {
        Transform damage = EZ_PoolManager.Spawn(m_enemyDamage.transform, vOne, Quaternion.identity, m_uiroot.transform);
        //Transform damage = NGUITools.AddChild(m_uiroot, m_enemyDamage).transform;
        Vector3 offset = new Vector3(Random.Range(-1, 1), y_offset, Random.Range(-1, 1));
        NGUIMath.OverlayPosition(damage, target.position + offset, Camera.main, UICamera.mainCamera);
        damage.localPosition = new Vector3(damage.localPosition.x, damage.localPosition.y, 0);
        damage.localScale = vOne;
        damage.GetComponent<DamageHUD>().Pop(value, critical);
    }
    public void PopDamageEnemyCustom(Transform target, float y_offset, DamageRequest drq)
    {
        Transform damage = EZ_PoolManager.Spawn(m_enemyDamageCustom.transform, vOne, Quaternion.identity, m_uiroot.transform);
        //Transform damage = NGUITools.AddChild(m_uiroot, m_enemyDamage).transform;
        Vector3 offset = new Vector3(Random.Range(-1, 1), y_offset, Random.Range(-1, 1));
        NGUIMath.OverlayPosition(damage, target.position + offset, Camera.main, UICamera.mainCamera);
        damage.localPosition = new Vector3(damage.localPosition.x, damage.localPosition.y, 0);
        damage.localScale = vOne;
        damage.GetComponent<DamageHUD>().Pop(drq.damage, drq.isCritical, drq.customDmgPopUp.fontSize, drq.customDmgPopUp.color);
    }
    public void PopDebuffDamage(Transform target, float y_offset, float value, StatusEffects effect)
    {
        GameObject selected;
        switch (effect)
        {
            case StatusEffects.bleed: selected = m_bleedDamage; break;
            case StatusEffects.poison: selected = m_poisonDamage; break;
            case StatusEffects.burn: selected = m_burnDamage; break;
            case StatusEffects.frostbiteFever: selected = m_frostbiteDmg; break;
            default: selected = m_playerDamage; break;
        }

        Transform damage = EZ_PoolManager.Spawn(selected.transform, vOne, Quaternion.identity, m_uiroot.transform);
        //Transform damage = NGUITools.AddChild(m_uiroot, selected).transform;
        Vector3 offset = new Vector3(Random.Range(-1, 1), y_offset, Random.Range(-1, 1));
        NGUIMath.OverlayPosition(damage, target.position + offset, Camera.main, UICamera.mainCamera);
        damage.localPosition = new Vector3(damage.localPosition.x, damage.localPosition.y, 0);
        damage.localScale = vOne;
        damage.GetComponent<DamageHUD>().Pop(value, false);
    }

    public void PopHealHp(Transform target, float y_offset, float value)
    {
        Transform damage = NGUITools.AddChild(m_uiroot, m_healHp).transform;
        NGUIMath.OverlayPosition(damage, target.position + Vector3.up * y_offset, Camera.main, UICamera.mainCamera);
        damage.localPosition = new Vector3(damage.localPosition.x, damage.localPosition.y, 0);
        damage.GetComponent<DamageHUD>().Pop(value, false);
    }
    public void PopHealMp(Transform target, float y_offset, float value)
    {
        Transform damage = NGUITools.AddChild(m_uiroot, m_healMp).transform;
        NGUIMath.OverlayPosition(damage, target.position + Vector3.up * y_offset, Camera.main, UICamera.mainCamera);
        damage.localPosition = new Vector3(damage.localPosition.x, damage.localPosition.y, 0);
        damage.GetComponent<DamageHUD>().Pop(value, false);
    }
    public void PopExpGold(Transform target, float y_offset, int valueXp, int valueGold)
    {
        Transform xp = NGUITools.AddChild(m_uiroot, m_exp).transform;
        NGUIMath.OverlayPosition(xp, target.position + Vector3.up * y_offset, Camera.main, UICamera.mainCamera);
        xp.localPosition = new Vector3(xp.localPosition.x, xp.localPosition.y, 0);

        xp.GetComponent<DamageHUD>().PopExp(valueXp);
        xp.GetComponent<DamageHUD>().PopGold(valueGold);
    }

    public void PopMiss(Transform target)
    {
        Transform miss = NGUITools.AddChild(m_uiroot, p_miss).transform;
        NGUIMath.OverlayPosition(miss, target.position + Vector3.up, Camera.main, UICamera.mainCamera);
        miss.localPosition = new Vector3(miss.localPosition.x, miss.localPosition.y, 0);
    }
    public void PopResist(Transform target)
    {
        Transform miss = NGUITools.AddChild(m_uiroot, p_resist).transform;
        NGUIMath.OverlayPosition(miss, target.position /*- Vector3.up*/, Camera.main, UICamera.mainCamera);
        miss.localPosition = new Vector3(miss.localPosition.x, miss.localPosition.y, 0);
    }
    public void PopGuard(Transform target)
    {
        Transform miss = NGUITools.AddChild(m_uiroot, p_guard).transform;
        NGUIMath.OverlayPosition(miss, target.position/* + Vector3.up * 2*/, Camera.main, UICamera.mainCamera);
        miss.localPosition = new Vector3(miss.localPosition.x, miss.localPosition.y, 0);
    }
    public void PopHeavyBlow(Transform target)
    {
        Transform miss = NGUITools.AddChild(m_uiroot, amy_heavyblow).transform;
        NGUIMath.OverlayPosition(miss, target.position + Vector3.up * 2, Camera.main, UICamera.mainCamera);
        miss.localPosition = new Vector3(miss.localPosition.x, miss.localPosition.y, 0);
    }
    public void PopPerfectShot(Transform target)
    {
        Transform miss = NGUITools.AddChild(m_uiroot, louisa_perfect).transform;
        NGUIMath.OverlayPosition(miss, target.position + Vector3.right * Random.Range(-1f, 1f) + Vector3.up * 5f, Camera.main, UICamera.mainCamera);
        miss.localPosition = new Vector3(miss.localPosition.x, miss.localPosition.y, 0);
    }
    public void PopBullseye(Transform target)
    {
        PopBullseye(target, 0);
    }
    public void PopBullseye(Transform target, float offset)
    {
        Transform miss = NGUITools.AddChild(m_uiroot, louisa_bullseye).transform;
        NGUIMath.OverlayPosition(miss, target.position + Vector3.right * Random.Range(-1f, 1f) + Vector3.up * (Random.Range(1.5f, 2.5f) + offset), Camera.main, UICamera.mainCamera);
        miss.localPosition = new Vector3(miss.localPosition.x, miss.localPosition.y, 0);
    }

    public void BatBite(Transform src)
    {
        Transform bite = NGUITools.AddChild(m_uiroot, batBite).transform;
        bite.GetComponent<EC2GuiFollow>().SetTarget(src);
        bite.GetComponent<SetRenderQueue>().m_target = gameplayTexture;
    }

    public EC2GUINpcName CreateNPCName(Transform src, float y_offset, string npcName, string npcTitle)
    {
        return CreateNPCName(src, y_offset, npcName, npcTitle, Color.white, new Color(0.5707547f, 0.5905432f, 1f));
    }
    public EC2GUINpcName CreateNPCName(Transform src, float y_offset, string npcName, string npcTitle, Color npcNameColor, Color npcTitleColor)
    {
        Transform guiName = NGUITools.AddChild(m_behindUI, npcGUI).transform;
        guiName.GetComponent<EC2GuiFollow>().SetTarget(src, !string.IsNullOrEmpty(npcTitle) ? y_offset : y_offset - 0.8f);
        var guiNPC = guiName.GetComponent<EC2GUINpcName>();
        guiNPC.SetInfo(npcName, npcTitle, npcNameColor, npcTitleColor);

        return guiNPC;
    }

    public Edna_SigilTicker CreateTicker(Transform src, float y_offset)
    {
        Transform guiTicker = NGUITools.AddChild(m_behindUI, sigil_ticker).transform;
        guiTicker.GetComponent<EC2GuiFollow>().SetTarget(src, y_offset);
        var ticker = guiTicker.GetComponent<Edna_SigilTicker>();
        return ticker;
    }

    public HUD_CustomLabel CreateHudText(Transform src, float y_offset)
    {
        Transform guiTicker = NGUITools.AddChild(m_behindUI, m_customTicker).transform;
        guiTicker.GetComponent<EC2GuiFollow>().SetTarget(src, y_offset);
        var ticker = guiTicker.GetComponent<HUD_CustomLabel>();
        return ticker;
    }
}
