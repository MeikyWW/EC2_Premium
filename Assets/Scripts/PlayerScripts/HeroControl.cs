using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using Sirenix.OdinInspector;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
/* Mother of All Hero Controllers. This script includes general controls
 * such as Basic Movements and Attack Inputs.
 * Also takes external movement/animator alteration
 * such as Snare, Freeze, Slow, and Flinch
 * */

[RequireComponent(typeof(CharacterController))]
public class HeroControl : MonoBehaviour
{
    public bool allowSpecial
    {
        get => gm.AllowMasterySkill;
    }

    protected float rotateSpeed = 20, gravity = 25;
    protected CharacterController _controller;
    [HideInInspector]
    public CharacterController Controller
    {
        get
        {
            if (!_controller)
                _controller = GetComponent<CharacterController>();
            return _controller;
        }
        set
        {
            _controller = value;
        }
    }
    protected PlayerInput _input;
    protected HeroStatus _status;
    protected HeroProps _props;
    protected HeroHealth _health;
    protected Animator _animator;
    protected float h, v, joystickMagnitude, townSpdMod;
    [HideInInspector] public float baseMovSpd;
    protected bool isAttacking, isSiege, isZeravReapStay;
    protected float siegeRotationSpeed = 0.5f;
    protected GameManager gm;
    protected HeroHUD heroHud;
    protected CombatCamera _cam;
    protected HeroTargetCircle _targetCircle;

    [HideInInspector]
    public GameObject stunFx;
    public Transform throwPos;
    public bool lookAtTargetCircle;

    //Character Specific
    protected bool isRageActivated;
    protected bool isFinalSkill;
    protected bool disableGravity;

    //Mobile 
    #region SUBSCRIBTIONS 
    public virtual void OnEnable()
    {
        PartyManager.OnHeroSwitch += SetSkillButtons;
        GameManager.OnPartyChanged += SetSkillButtons;
        GameManager.OnGameStateChanged += MovementOverState;
        // GameManager.OnTeleport += ResetBonusSpeed;
        // GameManager.OnTeleport += ResetBonusProtection;
    }

    public virtual void OnDisable()
    {
        PartyManager.OnHeroSwitch -= SetSkillButtons;
        GameManager.OnPartyChanged -= SetSkillButtons;
        GameManager.OnGameStateChanged -= MovementOverState;
        // GameManager.OnTeleport -= ResetBonusSpeed;
        // GameManager.OnTeleport -= ResetBonusProtection;
    }
    #endregion

    public virtual bool IsFreeSkillMode()
    {
        return false;
    }

    bool isInitialized;
    protected void Initialize()
    {
        isInitialized = true;
        _controller = GetComponent<CharacterController>();
        _status = GetComponent<HeroStatus>();
        _props = GetComponent<HeroProps>();
        _health = GetComponent<HeroHealth>();
        _animator = GetComponent<Animator>();
        _animator.keepAnimatorStateOnDisable = true;
        gm = GameManager.instance;
        //heroHud = gm.heroHud;
        _cam = CombatCamera.instance;
#if UNITY_STANDALONE
        //_input = gm.input;
#endif
        baseMovSpd = _status.moveSpeed;

        InitHero();
        InitAllSkills();
    }
    protected virtual void InitHero()
    {
        allSkillCooldown = new float[skills.Length];
        stunFx = transform.Find("VFX/StunFx").gameObject;

        _status.Initialize();
        InitAttackSpeed();
    }
    protected virtual void InitAllSkills()
    {
        //RefreshAllSkillCooldown();
        SetSkillButtons();
    }
    public void InitAttackSpeed()
    {
        if (_status != null)
        {
            ResetAttackSpeed();
        }
    }

    public void SetAttackSpeed(float percent)
    {
        if (_status != null)
        {
            _status.attackSpeedAlter = percent;
            ResetAttackSpeed();
        }
    }

    public void ResetAttackSpeed()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        _animator.SetFloat("aspd", _status.attackSpeedAlter * (1 + (_status.FinalAttackSpeed / 180f)));
    }

    public void SetMoveSpeed(float percent) //0 ~ 1
    {
        if (_status != null)
        {
            _status.moveSpeedAlter = percent;
            ResetMoveSpeed();
        }
    }

    public void ResetMoveSpeed()
    {
        _status.moveSpeed = _status.FinalMoveSpeed;
        _animator.SetFloat("mspd", _status.moveSpeed / baseMovSpd);
    }

    //==== GENERAL MOVEMENT ====//
    protected bool restrictMovement, instaRotate, lockDodge;

    public void MovementOverState(GameState state)
    {
        if (state == GameState.PAUSE)
        {
            if (gm.IsEngagedInCombat())
                _animator.speed = 0f;

            else
            {
                h = 0f; v = 0f;
                if (!isFalling) _animator.SetFloat("speed", 0);
            }
        }
        else
        {
            _animator.speed = gm.BaseTimeScale;
        }
    }

    public bool IsMovementRestricted()
    {
        if (!_controller.enabled) return true;
        if (isStunned) return true;
        if (gm.ActiveHero != _status) return true;
        if (_health.die) return true;
        if (isSiege) return true;
        if (restrictMovement) return true;
        if (isUsingSkill) return true;
        if (_health.isFrozen) return true;
        if (isStrongPulled) return true;
        // if (gm.fishMechanic.isFishing) return true;

        return false;
    }

    public virtual void Die()

    {
        EndUseItem();
        _health.ResetSuperArmor();
        _health.megaArmor = false;

        //_status.heroStatusEffect.RemoveNotification("mega_armor");

        // ResetBonusSpeed();
        // ResetBonusProtection();
        EndSkill();
    }

    protected void MovementUpdate()
    {
        if (!_status.currentlyActive) return;

#if UNITY_ANDROID || UNITY_IOS
        h = MobileControllerHUD.instance.dpad.AxisX;
        v = MobileControllerHUD.instance.dpad.AxisY;

        //buat simulate movement di unity editor
        if (h == 0 && v == 0)
        {
            h = Input.GetAxisRaw("HorizontalArrow"); // HorizontalAxis();
            v = Input.GetAxisRaw("VerticalArrow"); // _input.VerticalAxis();
        }
#else
        h = _input.HorizontalAxis();
        v = _input.VerticalAxis();
#endif


        if (!isInitialized)
        {
            Initialize();
            return;
        }
        if (!Controller.enabled) return;
        if (isStunned)
        {
            if (!isFalling)
            {
                _animator.SetFloat("speed", 0);
                if (!disableGravity) _controller.Move(Vector3.down * gravity * Time.deltaTime);
            }
            else UpdateFall();
            return;
        }
        if (!_status.currentlyActive) return;
        if (_health.die) return;
        if (_health.GetFrozenState()) return;

        if (isStrongPulled)
        {
            UpdateStrongPull();
            return;
        }

        if (_targetCircle)
        {
            _targetCircle.MovementUpdate(h, v, Time.deltaTime);
            if (lookAtTargetCircle)
            {
                InstantLookAt(_targetCircle.transform);
            }
            return;
        }

        if (isSiege)
        {
            if (h != 0 || v != 0)
                Rotating(h, v);
            return;
        }


        if (!disableGravity)
            _controller.Move(Vector3.down * gravity * Time.deltaTime);


        if (IsMovementRestricted()) return;

        if (isAttacking)
        {
            //_controller.Move(Vector3.down * gravity * Time.deltaTime);
            return;
        }

        if (_status.combatStat == CombatStatus.OffCombat)
        {
            joystickMagnitude = new Vector2(h, v).magnitude;
            _animator.SetFloat("joystick", joystickMagnitude);

            townSpdMod = joystickMagnitude >= 0.5f ? 1 : 0.4f;
        }
        else
        {
            townSpdMod = 1;
        }

        if (h != 0 || v != 0)
        {
            OnMovementInput();

            //zerav spc movement
            if (isZeravReapStay)
                townSpdMod = 0.4f;

            Rotating(h, v);
            _controller.Move(transform.forward * _status.moveSpeed * townSpdMod * Time.deltaTime);
            if (!isFalling)
            {
                if (!disableGravity)
                    _controller.Move(Vector3.down * gravity * Time.deltaTime);

                _animator.SetFloat("speed", 1);
            }
            else UpdateFall();
        }
        else
        {
            if (!isFalling) _animator.SetFloat("speed", 0);
            else UpdateFall();
        }
    }
    void Rotating(float horizontal, float vertical)
    {
        Vector3 axisDirection = new Vector3(horizontal, 0, vertical);
        Vector3 targetDirection = Camera.main.transform.TransformDirection(axisDirection);
        targetDirection.y = 0;

        if (isSiege)
        {

            Quaternion lookRot = Quaternion.LookRotation(targetDirection);
            Vector3 rot = Quaternion.Slerp(transform.rotation, lookRot, siegeRotationSpeed * Time.deltaTime).eulerAngles;
            transform.eulerAngles = rot;
        }
        else if (isAttacking || instaRotate)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            transform.rotation = targetRotation;
        }
        else
        {
            Quaternion lookRot = Quaternion.LookRotation(targetDirection);

            Vector3 rot;
            rot = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime).eulerAngles;
            transform.eulerAngles = rot;
        }
    }

    public void FollowInputDirection()
    {
        if (h != 0 || v != 0)
        {
            Timing.RunCoroutine(DelayRotation().CancelWith(gameObject), Segment.LateUpdate);
        }
    }

    bool isFalling; float airTime;
    public void SetFall(bool falling)
    {
        isFalling = falling;

        if (_animator)
        {
            _animator.SetFloat("airTime", airTime);
            _animator.SetBool("fall", isFalling);
        }

        airTime = 0;
    }
    void UpdateFall()
    {
        if (isFalling)
        {
            //_controller.Move(Vector3.down * gravity * Time.deltaTime);
            airTime += Time.deltaTime;

            CheckGround();
        }
    }
    void CheckGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2, 1 << 0))
        {
            SetFall(false);
            _props.fallCheck.ResetTrigger();
        }
    }

    public void EnableMove()
    {
        restrictMovement = false;
    }
    public void DisableMove()
    {
        restrictMovement = true;
    }
    public void DisableMove(bool forceAnimation)
    {
        DisableMove();
        if (forceAnimation) _animator.SetFloat("speed", 0);
    }

    protected void CombatReady()
    {
        EnableMove();
        EnterIdleState();
    }
    public void ResetCharacterState()
    {
        EnterIdleState();
        EnableSkill();
        EnableDodge();
        EnableMove();
    }
    protected void SetSiege(bool state)
    {
        isSiege = state;
    }

    [Header("Sense Radius")]
    public float lookRadius;
    public List<Transform> GetClosestEnemies(int count, float radius)
    {
        Collider[] victims = Physics.OverlapSphere(transform.position, radius, 1 << 10 | 1 << 13);
        List<Collider> closeColliders = new List<Collider>();

        foreach (Collider c in victims)
        {
            float squaredDistance = (c.transform.position - transform.position).sqrMagnitude;

            if (closeColliders.Count < count)
            {
                closeColliders.Add(c);
            }
            else
            {
                float farthestDistance = 0f;
                int farthestIndex = 0;

                for (int i = 0; i < closeColliders.Count; i++)
                {
                    float dist = (closeColliders[i].transform.position - transform.position).sqrMagnitude;
                    if (dist > farthestDistance)
                    {
                        farthestDistance = dist;
                        farthestIndex = i;
                    }
                }

                if (squaredDistance < farthestDistance)
                {
                    closeColliders[farthestIndex] = c;
                }
            }
        }


        List<Transform> targets = new List<Transform>();
        foreach (Collider c in closeColliders)
        {
            targets.Add(c.transform);
        }

        return targets;
    }
    public Transform GetClosestEnemy()
    {
        return GetClosestEnemy(lookRadius);
    }
    public Transform GetClosestEnemy(float lookRadius)
    {
        Transform bestTargetEnemy = null;
        Transform bestTargetAnother = null;
        float closestDistanceEnemy = Mathf.Infinity;
        float closestDistanceAnother = Mathf.Infinity;

        Collider[] victims = Physics.OverlapSphere(transform.position, lookRadius, 1 << 10 | 1 << 13);
        foreach (Collider c in victims)
        {
            float range = (c.transform.position - transform.position).magnitude;

            if (c.CompareTag("enemy"))
            {
                if (range < closestDistanceEnemy)
                {
                    closestDistanceEnemy = range;
                    bestTargetEnemy = c.transform;
                }
            }
            else
            {
                if (range < closestDistanceAnother)
                {
                    closestDistanceAnother = range;
                    bestTargetAnother = c.transform;
                }
            }
        }

        return bestTargetEnemy != null ? bestTargetEnemy : bestTargetAnother;
    }


    //==== STRONK PULLED ===//

    [HideInInspector] public bool isStrongPulled;
    [HideInInspector] public float strongPullSpeed;
    Transform puller;

    //=== STRONK PULL ===//
    public void ApplyStrongPull(Transform puller, float speed)
    {
        if (_health.die) return;
        if (_health.ignoreFlinch) return;
        if (gm.IsGameOver()) return;
        if (IsSiege) return;
        isStrongPulled = true;
        strongPullSpeed = speed;
        this.puller = puller;
        DisableMove();
        _animator.SetBool("stunned", true);
        //gm.MoveHero(this, puller.position, transform.rotation);
    }

    public void UpdateStrongPull()
    {
        if (puller == null || !puller.gameObject.activeInHierarchy)
        {
            EndStrongPull();
            return;
        }

        InstantLookAt(puller);
        _controller.Move(transform.forward * strongPullSpeed * Time.deltaTime);
    }

    public void EndStrongPull()
    {
        isStrongPulled = false;
        EnableMove();

        if (!isStunned)
            _animator.SetBool("stunned", false);
    }

    public bool EnemyNearby(float lookRadius)
    {
        if (GetClosestEnemy(lookRadius) == null) return false;
        else return true;
    }
    protected void LookAtClosestEnemy()
    {
        Transform closest = GetClosestEnemy();
        if (closest) Timing.RunCoroutine(DelayLookAt(closest), Segment.EndOfFrame);
    }
    protected IEnumerator<float> DelayLookAt(Transform lookTarget)
    {
        yield return 0;

        if (lookTarget)
            transform.LookAt(new Vector3(lookTarget.position.x, transform.position.y, lookTarget.position.z));
    }
    public void InstantLookAt(Transform lookTarget)
    {
        Timing.RunCoroutine(DelayLookAt(lookTarget));
    }

    protected void SetTriggerAnimation(string trigger)
    {
        _animator.SetTrigger(trigger);
    }

    //==== Consumable Use ====//
    public bool StartUseConsumable()
    {
        if (gm.STATE == GameState.PAUSE) return false;
        if (_status.combatStat == CombatStatus.OffCombat) return false;
        if (isUsingSkill) return false;
        if (gm.ActiveHero.heroHealth.GetFrozenState()) return false;
        if (gm.IsGameOver()) return false;
        if (restrictMovement) return false;
        if (isSiege) return false;
        //isUsingSkill = justSkill = true;
        //_health.StartSuperArmor(0);
        ResetAttack();
        DisableMove();
        isUsingItem = true;
        _animator.SetFloat("useItemSpeed", _animator.GetFloat("aspd")); // + (_animator.GetFloat("aspd") * _status.socketEffect.quickConsume / 100));
        SetTriggerAnimation("useItem");

        return true;
    }

    public static event System.Action OnUseItem;
    protected void UseItem() //use potion/consumables
    {
        //if (!gm.heroHud.CanUseLastItem()) return;
        //_health.EndSuperArmor(0);
        Item usedItem = heroHud.ConsumeSelectedItem();
        OnUseItem?.Invoke();

        switch (usedItem.consumable.useType)
        {
            case ConsumableUseType.Normal:
            default:
                foreach (var effect in usedItem.consumable.effects)
                    UseNormalItem(effect);
                break;

            // case ConsumableUseType.Throwable:
            //     UseThrowableItem(usedItem.consumable.throwableData);
            //     break;
        }
    }

    void UseNormalItem(ConsumableEffect effect)
    {
        //Increase Consumable Potency
        float potencyIncreaseVal = _status.FinalConsumablePlus / 100 * effect.value;
        float potencyIncreaseDur = _status.FinalConsumablePlus / 100 * effect.duration;
        float finalValue = effect.value + potencyIncreaseVal;
        float finalDuration = effect.duration + potencyIncreaseDur;

        finalValue = Mathf.Floor(finalValue);

        //Apply consumable effects
        switch (effect.effect)
        {
            case ConsumableType.HPRecovery:
                _health.RestoreHp(finalValue, true);
                _status.TropicalSquashStart(finalValue, 0);
                break;

            case ConsumableType.HPRecoveryPercent:
                var pctAmount = _health.GetRestoreHpPercent(finalValue);
                _health.RestoreHp(pctAmount, true);
                _status.TropicalSquashStart(pctAmount, 0);
                break;

            case ConsumableType.HPRecoveryCombined:
                var pct = _health.GetRestoreHpPercent(finalValue);
                var rawIncrease = effect.rawValue + (_status.FinalConsumablePlus / 100 * effect.rawValue);
                var total = pct + rawIncrease;
                _health.RestoreHp(total, true);
                _status.TropicalSquashStart(total, 0);
                break;

            case ConsumableType.ManaRecovery:
                _status.AddManaValue(finalValue, true, true, true, false);
                _status.TropicalSquashStart(0, finalValue);
                break;

            case ConsumableType.CureAll: _health.RemoveAllStatusEffect(true); break;
            case ConsumableType.CurePara: _health.CureStatusEffect(StatusEffects.paralyze, true); break;
            case ConsumableType.CureBleed: _health.CureStatusEffect(StatusEffects.bleed, true); break;
            case ConsumableType.CurePoison: _health.CureStatusEffect(StatusEffects.poison, true); break;

            case ConsumableType.DmgBoost:
                StatusEffectManager.instance.SetStatusEffect(transform, StatusEffects.atkUp, finalDuration, finalValue);
                GameManager.instance.vfxManager.PotionCure(_status.potionFxPos, 0); break;

            case ConsumableType.EleResBoost:
                StatusEffectManager.instance.SetStatusEffect(transform, StatusEffects.eleResUp, finalDuration, finalValue);
                GameManager.instance.vfxManager.PotionCure(_status.potionFxPos, 0); break;

            case ConsumableType.PhyResBoost:
                StatusEffectManager.instance.SetStatusEffect(transform, StatusEffects.phyResUp, finalDuration, finalValue);
                GameManager.instance.vfxManager.PotionCure(_status.potionFxPos, 0); break;

            case ConsumableType.Poison: StatusEffectManager.instance.SetStatusEffect(transform, StatusEffects.poison, finalValue, 1); break;

            case ConsumableType.CritBoost:
                StatusEffectManager.instance.SetStatusEffect(transform, StatusEffects.critUp, finalDuration, finalValue);
                GameManager.instance.vfxManager.PotionCure(_status.potionFxPos, 0); break;

            case ConsumableType.SetStatusEffect:
                DamageRequest req = new DamageRequest()
                {
                    // statusEffect = effect.statusEffectData.statusEffect,
                    statusEffectDuration = effect.statusEffectData.statusEffectDuration,
                    statusEffectDamage = effect.statusEffectData.statusEffectPotency,
                    unresistable = effect.statusEffectData.unresistable
                };
                StatusEffectManager.instance.SetStatusEffect(transform, req); break;

            default: break;
        }
    }

    // void UseThrowableItem(ThrowableData throwable) //use potion/consumables
    // {
    //     //Summon Kunai from hand
    //     if (throwable.throwedPrefab != null)
    //     {
    //         GameObject throwed = Instantiate(throwable.throwedPrefab, throwPos.position, throwPos.rotation);

    //         var dmg = _status.GetModifiedFinalDamage() * (throwable.potency / 100);
    //         dmg += dmg * (_status.FinalConsumablePlus / 100);

    //         float finalStunDuration = throwable.stunDuration;// * (1 + (_status.FinalConsumablePlus / 100));
    //         float finalStatusEffectPotency = throwable.statusEffectData.statusEffectPotency;// * (1 + (_status.FinalConsumablePlus / 100));
    //         float finalStatusEffectDuration = throwable.statusEffectData.statusEffectDuration;// * (1 + (_status.FinalConsumablePlus / 100));
    //         var finalDmg = dmg;
    //         var crit = false;
    //         if (throwable.critable)
    //             ApplyCritical(dmg, out finalDmg, out crit);

    //         DamageRequest req = new DamageRequest()
    //         {
    //             damage = finalDmg,
    //             stunDuration = finalStunDuration,
    //             isCritical = crit,
    //             statusEffect = throwable.statusEffectData.statusEffect,
    //             statusEffectDamage = finalStatusEffectPotency,
    //             statusEffectDuration = finalStatusEffectDuration,
    //             accuracy = 100,
    //             knockback = 0
    //         };

    //         var throwedItem = throwed.GetComponent<ThrowedItem>();
    //         if (throwedItem != null)
    //         {
    //             throwedItem.Launch(transform, req);

    //             if (throwable.spawnedField != null)
    //             {
    //                 throwedItem.SetField(throwable.spawnedField);
    //             }
    //         }
    //     }
    // }

    void EndUseItem()
    {
        isUsingItem = false;
        instaRotate = false;
        ResetSpecialAttack();
        EnableMove();
        EnableDodge();
        //EnableSkill();
        //EndSkill();
        CancelUseItem(); //Use Potion Bug Fix #3
    }
    void CancelUseItem()
    {
        isUsingItem = false;
        heroHud.ItemUseInterrupted();
        _animator.ResetTrigger("useItem"); //Use Potion Bug Fix #2
    }

    //==== Hits and Knockbacks ====//

    public void Hit(Transform source, float knockback, bool responseOnHit)
    {
        //Debug.Log("taking hit. atk id = " + attackIndex);
        instaRotate = false;
        isUsingItem = false;
        justSkill = false;
        ResetAttack();

        EndIframe();
        ResetSpecialAttack();
        EnableMove();

        EndSkill();
        EnterIdleState();
        CancelUseItem();

        EnableDodge();
        _props.PlayHit();
        _animator.ResetTrigger("dodge");
        if (responseOnHit) SetTriggerAnimation("hit");

        Timing.RunCoroutine(Knockback(source, knockback, true, responseOnHit).CancelWith(gameObject), Segment.EndOfFrame);
    }
    protected void Recoil(Transform facing, float amount)
    {
        Timing.RunCoroutine(Knockback(facing, amount, true, true), Segment.EndOfFrame);
    }
    IEnumerator<float> Knockback(Transform source, float amount, bool instant, bool responseOnHit)
    {
        yield return 0;
        if (amount != 0)
        {
            if (source && responseOnHit) transform.LookAt(new Vector3(source.position.x, transform.position.y, source.position.z));

            //_controller.enabled = false;
            //var newPos = transform.position + (transform.forward * -amount);
            //transform.DOMoveZ(newPos.z, 2f).SetEase(Ease.OutSine).OnComplete(() =>
            //{
            //    _controller.enabled = true;
            //});

            if (instant)
            {
                if (responseOnHit)
                    _controller.Move(transform.forward * -amount);
                else
                    _controller.Move(source.forward * amount);
            }

            else
            {
            }
        }
    }
    void TakeHit()
    {
        DisableMove();
        DisableDodge();
    }
    void HitEnd()
    {
        if (isUsingSkill) return;

        justSkill = true;
        ResetAttack();


        EndSkill();
        /*EnterIdleState();
        */

        EnableSkill();
    }
    public void StartIframe()
    {
        if (_status)
            if (_status.protectionActive) return;

        gameObject.layer = 15;
    }
    public void EndIframe()
    {
        if (_status)
            if (_status.protectionActive) return;

        gameObject.layer = 9;
    }

    public bool IsSiege
    {
        get => isSiege;
    }

    //==== Stun ====//
    [HideInInspector]
    public bool isStunned = false;
    float stunDuration;
    public void Stun(float duration)
    {
        if (isUsingItem) CancelUseItem();
        if (!isStunned)
        {
            DisableMove();
            stunDuration = duration;

            isStunned = true;
            stunFx.SetActive(true);
            _animator.SetBool("stunned", true);

        }
        else
        {
            //refresh stun duration
            stunDuration = duration;
        }
    }
    protected void UpdateStunTimer()
    {
        if (isStunned)
        {
            stunDuration -= Time.deltaTime;
            if (stunDuration <= 0) StunEnd();
        }
    }

    public void StunEnd()
    {
        EnableMove();
        EnableDodge();
        EndSkill();

        isStunned = false;
        isUsingSkill = false;

        stunFx.SetActive(false);

        if (!isStrongPulled)
            _animator.SetBool("stunned", false);

        CancelUseItem();  //Use Potion Bug Fix #1
    }

    //==== BASIC ATTACK ====//
    protected int attackIndex, indexInc = 0;
    protected ActionButton pressedBtn;
    protected bool lockAtkInput, actionFramePassed = true, idleState;
    protected float attackBreak;
    // public static event System.Action<MissionAction> OnBasicAttack;
    // public virtual void BasicAtkPress()
    // {
    //     //Proceed 
    //     pressedBtn = ActionButton.BasicAtk;
    //     if (!lockAtkInput)
    //     {
    //         if (isAttacking) lockAtkInput = true;
    //         PlayAttackAnimation();
    //         OnBasicAttack?.Invoke(MissionAction.Attack);
    //     }
    // }
    protected virtual void PlayAttackAnimation()
    {
        if (isUsingSkill) return;
        if (isStunned) return;

        pressedBtn = ActionButton.None;
        attackIndex += indexInc;

        if (attackIndex > _status.basicAtkMV.Length)
        {
            attackIndex = -1;
        }
        else
        {
            try
            {
                attackBreak = _status.basicAtkBreak[attackIndex] * (1 + _status.speciality / 100) * (1 + extraBreakFixed / 100);
                SetMotionValue(_status.basicAtkMV[attackIndex]);
            }
            catch
            {
                attackBreak = 0;
                SetMotionValue(100);
            }

            _animator.SetInteger("attack", attackIndex);
        }
    }

    void ActionStart()
    {
        idleState = false;
        lockAtkInput = false;
        isAttacking = true;
        actionFramePassed = false;
        //DisableDodge();
        Timing.RunCoroutine(DelayRotation(), Segment.EndOfFrame);
    }
    void ActionEnd()
    {
        actionFramePassed = true;
        EnableDodge();
        PlayAttackAnimation();
        pressedBtn = ActionButton.None;
    }
    public void ResetAttack()
    {
        pressedBtn = ActionButton.None;
        attackIndex = -1;
        if (_animator) _animator.SetInteger("attack", attackIndex);
        isAttacking = false;
        lockAtkInput = false;
        actionFramePassed = true;
    }
    public virtual void EnterIdleState()
    {
        idleState = true;
        isAttacking = false;
        disableGravity = false;

        _health.ResetSuperArmorTemporary();
        _health.EndFlashEvasionFrame();
        EnableDodge();
        //EnableMove();

        if (!isUsingSkill)
        {
            EndIframe();
            DisableMegaArmor();
        }

        //isUsingSkill = false;

        EnableSkill();

        if (!justSkill)
        {
            //Debug.Log("reset attack index to -1");
            ResetAttack();
        }
        else
        {
            //Debug.Log("just skill. no reset. atk index = " + attackIndex);
            justSkill = false;
        }
    }
    public virtual void LateEnterIdleState()
    {
        if (!isUsingSkill)
        {
            DisableMegaArmor();
            EndIframe();
        }
        //EnableMove();
    }
    IEnumerator<float> DelayFreeRotation()
    {
        //yield return new WaitForEndOfFrame();
        yield return 0;

        if (h != 0 || v != 0)
        {
            Rotating(h, v);
        }
    }

    IEnumerator<float> DelayRotation()
    {
        yield return Timing.WaitForOneFrame;
        //yield return 0;

        if ((h != 0 || v != 0) && (!gm.userData.settings.autoTarget || GetClosestEnemy() == null))
        {
            Rotating(h, v);
        }
        else LookAtClosestEnemy();
    }

    protected IEnumerator<float> DelayedScreenShake(bool nextFrame, float delay, int shakeStr)
    {
        if (nextFrame) yield return Timing.WaitForOneFrame;
        else yield return Timing.WaitForSeconds(delay);

        print("delay shake");

        switch (shakeStr)
        {
            case 0: _cam.TinyShake(); break;
            case 1: _cam.LightShake(); break;
            case 2: _cam.MediumShake(); print("med shake"); break;
            case 3: _cam.HeavyShake(); break;
        }
    }

    //==== SKILLS and COOLDOWN ====//
    protected float[] allSkillCooldown;
    protected bool isUsingSkill, restrictSkill, justSkill, isUsingItem;
    float skillTimer;
    float violenceCooldown;
    protected virtual bool CanUseSkill(int skillIndex, float manaUsage, float cooldown, bool instant)
    {
        //check whether game is paused,
        //character is stunned, silenced, has enough mana,
        //skill is on cooldown, etc

        if (gm.STATE == GameState.PAUSE) return false;
        if (_status.combatStat == CombatStatus.OffCombat) return false;
        if (skills[skillIndex].disabledByEnsnare && _health.isEnsnared) return false;
        if (_health.isSilenced) return false;
        if (restrictSkill) return false;
        if (isStunned) return false;
        if (isStrongPulled) return false;
        if (_health.GetFrozenState()) return false;
        if (allSkillCooldown[skillIndex] > 0) return false;

        if (instant)
        {
            //instant skill is a skill that can be used at any point of combat.
            //without interrupting animation.
            if (!_status.HasEnoughMana(manaUsage)) return false;

            if (ApplyBurst())
            {
                allSkillCooldown[skillIndex] = 0.1f;
                skills[skillIndex].appliedCooldown = 0.1f;
            }
            else
            {
                allSkillCooldown[skillIndex] = cooldown;
                skills[skillIndex].appliedCooldown = cooldown;
            }
            return true;
        }
        else
        {
            if (isUsingSkill) return false;
            if (!_status.HasEnoughMana(manaUsage)) return false;

            isUsingSkill = justSkill = true;

            if (ApplyBurst())
            {
                allSkillCooldown[skillIndex] = 0.1f;
                skills[skillIndex].appliedCooldown = 0.1f;
            }
            else
            {
                allSkillCooldown[skillIndex] = cooldown;
                skills[skillIndex].appliedCooldown = cooldown;
            }
            ResetAttack();
            DisableMove();
            Timing.KillCoroutines(handlerForceEndSkill);
            //handlerForceEndSkill = Timing.RunCoroutine(ForceEndSkill().CancelWith(gameObject), EC2Constant.STATE_DEPENDENT);
            return true;
        }
    }

    CoroutineHandle handlerForceEndSkill;
    IEnumerator<float> ForceEndSkill()
    {
        yield return Timing.WaitForSeconds(5f);

        if (isUsingSkill)
        {
            EndSkill();
            Debug.Log("Char stuck");
        }
    }

    // public static event System.Action<MissionAction> OnSucceedSkill;
    protected void AddSkillExp(int index)
    {
        // OnSucceedSkill?.Invoke(MissionAction.Skill);
        int exp = _status.heroData.data.skillExp[index];
        int level = _status.heroData.data.skillLevels[index];

        if (level >= 10) return;

        exp++;
        if (exp >= EC2Utils.GetSkillMaxExp(level))
        {
            //skill level up
            level++; exp = 0;
            skills[index].SetSkillAttributes(level);// (_status.heroData.data.skillLevels[index]);
            // gm.NotifyLevelUp(skills[index].id, skills[index].SkillName(), true, false);
        }

        _status.heroData.data.skillLevels[index] = level;
        _status.heroData.data.skillExp[index] = exp;
    }
    public void SkillUnlock(int index)
    {
        int trueSkillIndex = skillUnlockOrder[index];
        int level = _status.heroData.data.skillLevels[trueSkillIndex];

        if (level > 0) return;

        _status.heroData.data.skillLevels[trueSkillIndex] = 1;
        AddNewSkillToEmptySlot(trueSkillIndex);

        //notify skill unlock
        // gm.NotifyLevelUp(skills[trueSkillIndex].id, skills[trueSkillIndex].SkillName(), true, true);
    }
    public void AddNewSkillToEmptySlot(int skillIndex)
    {
        int emptySlot = -1;
        for (int i = 0; i < 4; i++)
        {
            if (_status.heroData.data.skillSlot[i] == -1)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot >= 0)
        {
            _status.heroData.data.skillSlot[emptySlot] = skillIndex;
            SetSkillButtons();
        }
    }

    public virtual void EndSkill()
    {
        instaRotate = false;
        isUsingSkill = false;
        isUsingItem = false;
        justSkill = false;
        ResetAttack();

        EndIframe();
        ResetSpecialAttack();
        EnableMove();
        EnableDodge();
    }
    public void EnableSkill()
    {
        restrictSkill = false;
    }
    public void DisableSkill()
    {
        restrictSkill = true;
    }
    protected void UpdateSkillTimer()
    {
        skillTimer += Time.deltaTime;
    }
    void TimerStart()
    {
        skillTimer = 0;
    }
    void TimerEnd()
    {
        //Debug.Log("skill perform time : " + skillTimer);
    }
    public virtual void StartFishing()
    {
    }
    public virtual void EndFishing()
    {
    }

    public virtual void HideWeapons()
    {

    }

    public virtual void ShowWeapons()
    {
    }


    public virtual void GoToSafeArea()
    {
        EnterIdleState();
        EndSkill();
    }
    public virtual void GoToCombatArea()
    {
        EnterIdleState();
        EndSkill();
    }

    public bool IsUsingSkill()
    {
        return isUsingSkill;
    }

    public bool IsUsingItem()
    {
        return isUsingItem;
    }
    //==== Evasion ====//
    protected float evasionCooldown;
    public void StartEvasion()
    {
        if (_status.combatStat == CombatStatus.OffCombat) return;
        if (isUsingSkill) return;
        if (lockDodge) return;
        if (_health.GetFrozenState()) return;
        if (isStunned) return;
        if (isStrongPulled) return;
        if (_health.isEnsnared) return;
        if (evasionCooldown > 0) return;
        if (isSiege) return;

        CommenceEvasion();
    }
    protected void QuickRotate()
    {
        instaRotate = true;
        Timing.RunCoroutine(DelayRotation(), Segment.EndOfFrame);
    }
    protected void QuickFreeRotate()
    {
        instaRotate = true;
        Timing.RunCoroutine(DelayFreeRotation(), Segment.EndOfFrame);
    }
    protected virtual void CommenceEvasion()
    {
        _health.ResetSuperArmorTemporary();
        CancelUseItem();
        ResetAttack();
        DisableDodge();
        DisableMove();
        DisableSkill();
        QuickFreeRotate();
        OnEvasion();
        DisableMegaArmor();

        isUsingItem = false;
        isUsingSkill = justSkill = true;

        _health.StartFlashEvasionFrame();

        _animator.ResetTrigger("hit");
        SetTriggerAnimation("dodge");
        evasionCooldown = evasion.ApplyCooldown(0);// (_status.FinalCDReduction);
    }
    void EndFlashEvasionFrame()
    {
        if (maidPerfectReady) return;
        _health.EndFlashEvasionFrame();
        StartIframe();
    }
    void EndFlashEvasionFrame2()
    {
        _health.EndFlashEvasionFrame();
        StartIframe();
    }
    void EndEvasion()
    {
        if (h != 0 || v != 0) _animator.SetFloat("speed", 1);
        else _animator.SetFloat("speed", 0);

        EndSkill();
        EnterIdleState();
    }
    public void EnableDodge()
    {
        lockDodge = false;
    }
    public void DisableDodge()
    {
        lockDodge = true;
    }
    public void RefreshEvasion()
    {
        //evasionCooldown = 0.1f;
        //evasion.runningCooldown = 0.1f;

        evasionCooldown = 0f;
        evasion.runningCooldown = 0f;
    }

    public void RefreshAllSkillCooldown()
    {
        for (int i = 0; i < allSkillCooldown.Length; i++)
        {
            RefreshSkill(i);
        }
    }
    public void RefreshSkill(int skillId)
    {
        allSkillCooldown[skillId] = 0.2f;
        skills[skillId].runningCooldown = allSkillCooldown[skillId];
    }
    protected void ReduceAllSkillsCooldown(float pct)
    {
        for (int i = 0; i < allSkillCooldown.Length; i++)
        {
            ReduceSkillCooldown(i, pct);
        }
    }
    protected void ReduceSkillCooldown(int skillId, float pct)
    {
        if (allSkillCooldown[skillId] > 0)
        {
            var totalCD = skills[skillId].appliedCooldown;
            float value = pct / 100 * totalCD;

            allSkillCooldown[skillId] -= value;
            skills[skillId].runningCooldown = allSkillCooldown[skillId];
        }
    }
    protected void ReduceSkillCooldownSecond(int skillId, float second)
    {
        if (allSkillCooldown[skillId] > 0)
        {
            allSkillCooldown[skillId] -= second;
            skills[skillId].runningCooldown = allSkillCooldown[skillId];
        }
    }

    protected void UpdateSkillsCooldown()
    {
        for (int i = 0; i < allSkillCooldown.Length; i++)
        {
            if (allSkillCooldown[i] > 0)
            {
                allSkillCooldown[i] -= Time.deltaTime * (gameObject == gm.ActiveHero.gameObject ? 1f : 0.5f);
                skills[i].runningCooldown = allSkillCooldown[i];
            }
        }
    }
    protected void UpdateEvasionCooldown()
    {
        if (evasionCooldown <= 0) return;
        evasionCooldown -= Time.deltaTime;
        evasion.runningCooldown = evasionCooldown;
    }

    //==== SKILL SLOT ====//
    [Header("[Skill Data]")]
    public EC2HeroSkill evasion;
    public EC2HeroSkill[] skills;
    public int[] skillUnlockOrder;

    //Assigned skill slot
    public int slotA { get => _status.heroData.data.skillSlot[0]; }
    public int slotB { get { try { return _status.heroData.data.skillSlot[1]; } catch { return -1; } } }
    public int slotC { get { try { return _status.heroData.data.skillSlot[2]; } catch { return -1; } } }
    public int slotD { get { try { return _status.heroData.data.skillSlot[3]; } catch { return -1; } } }


    public static event System.Action OnSkillChanged;
    public void SetSkillButtons()
    {
        if (!isInitialized) Initialize();
        if (gm.ActiveHero != _status) return;
        // gm.heroHud.skillA.SetObserve(slotA < 0 ? null : skills[slotA]);
        // gm.heroHud.skillB.SetObserve(slotB < 0 ? null : skills[slotB]);
        // gm.heroHud.skillC.SetObserve(slotC < 0 ? null : skills[slotC]);
        // gm.heroHud.skillD.SetObserve(slotD < 0 ? null : skills[slotD]);

        _props.CheckStance();

        OnSkillChanged?.Invoke();
    }
    public void ChangeSkill(int slot, int skillId)
    {
        switch (slot)
        {
#if UNITY_ANDROID || UNITY_IOS
            case 0: _status.heroData.SetSkillSlot(0, skillId); break;
            case 1: _status.heroData.SetSkillSlot(1, skillId); break;
            case 2: _status.heroData.SetSkillSlot(2, skillId); break;
            case 3: _status.heroData.SetSkillSlot(3, skillId); break;
#else
            case 0: _status.heroData.SetSkillSlot(2, skillId); break;
            case 1: _status.heroData.SetSkillSlot(0, skillId); break;
            case 2: _status.heroData.SetSkillSlot(1, skillId); break;
            case 3: _status.heroData.SetSkillSlot(3, skillId); break;
#endif
        }

        if (skillId >= 0)
        {
            //set to cooldown
            float cooldown = skills[skillId].ApplyCooldown(0);// (_status.FinalCDReduction);
            allSkillCooldown[skillId] = cooldown;
            skills[skillId].runningCooldown = allSkillCooldown[skillId];
        }

        SetSkillButtons();
    }

    public virtual void BasicAtkRelease()
    {

    }
    public virtual void SpecialAtkPress()
    {
    }
    public virtual void SpecialAtkRelease()
    {

    }
    public void SkillAPress()
    {
        if (slotA < 0) return;
        SkillActivate(slotA);
    }
    public void SkillARelease()
    {
        if (slotA < 0) return;
        SkillRelease(slotA);
    }
    public void SkillBPress()
    {
        if (slotB < 0) return;
        SkillActivate(slotB);
    }
    public void SkillBRelease()
    {
        if (slotB < 0) return;
        SkillRelease(slotB);
    }
    public void SkillCPress()
    {
        if (slotC < 0) return;
        SkillActivate(slotC);
    }
    public void SkillCRelease()
    {
        if (slotC < 0) return;
        SkillRelease(slotC);
    }
    public void SkillDPress()
    {
        if (slotD < 0) return;
        SkillActivate(slotD);
    }
    public void SkillDRelease()
    {
        if (slotD < 0) return;
        SkillRelease(slotD);
    }
    protected virtual void SkillActivate(int skillIndex)
    {
    }
    protected virtual void SkillRelease(int skillIndex)
    {

    }


    //monolith effect
    public void SkillHijack()
    {
        //get random number on skill slot
        int random = Random.Range(0, 4);

        //get the skill id
        int skillId = _status.heroData.data.skillSlot[random];

        //set to original cd
        float cooldown = skills[skillId].ApplyCooldown(0);
        allSkillCooldown[skillId] = cooldown;
        skills[skillId].runningCooldown = allSkillCooldown[skillId];
    }

    CoroutineHandle windingCoroutine;
    CoroutineHandle windingNotif;
    public static System.Action<Transform> elze_noble_onSkillUse;
    protected virtual void SkillUsed()
    {
        if (_status.socketEffect.wind.count > 0)
        {
            Timing.KillCoroutines(windingCoroutine, () => _status.KillStatusEffectNotification(windingNotif, "icon_rune_" + SocketType.Wind.ToString().ToLower()));

            windingCoroutine = Timing.RunCoroutine(WindUpSpeed(_status.socketEffect.wind.GetValue(1)), EC2Constant.STATE_DEPENDENT);
        }

        _status.extraSkillDmg = 0;
        _status.maidSkillDmg = 0;
        _status.heroStatusEffect.RemoveNotification("costume_" + CostumeSet.Maid.ToString().ToLower() + "_0");

        elze_noble_onSkillUse?.Invoke(transform);
    }

    IEnumerator<float> WindUpSpeed(float duration)
    {
        _status.isWindActive = true;
        windingNotif = Timing.RunCoroutine(_status.StatusEffectByDuration("icon_rune_" + SocketType.Wind.ToString().ToLower(), duration, null),
            EC2Constant.STATE_DEPENDENT);
        ResetAttackSpeed();
        ResetMoveSpeed();

        yield return Timing.WaitForSeconds(duration);

        _status.isWindActive = false;

        yield return Timing.WaitForOneFrame;
        ResetAttackSpeed();
        ResetMoveSpeed();
    }

    public virtual void CommenceSwitch()
    {
        _props.PlaySwitch();

        if (_status.socketEffect.recharge.count > 0)
        {
            _status.AddManaValue(_status.socketEffect.recharge.GetValue(0), true);
        }

        if (_status.costumeEffect.santaValue > 0)
        {
            _status.SantaRushStart();
        }

        if (_status.setEffect.abyssInferno > 0)
        {
            TriggerPhoenixResetSkill();
        }

        foreach (var skill in skills)
        {
            if (skill)
                skill.ApplyCooldown(_status.FinalCDReduction);
        }
    }

    public virtual void BeforeSwitch()
    {
        // ResetBonusSpeed();
        // ResetBonusProtection();

        if (_status.setEffect.abyssInferno > 1)
        {
            TriggerPhoenixHeal();
        }
    }

    public virtual void OnSwitchedOut()
    {
        Debug.Log(_status.heroReference.hero + " switch out");
    }

    //==== MASTERIES ====//
    // [Header("[Mastery Data]")]
    // public EC2HeroMastery[] mastery;
    public bool MasteryUnlocked(int index)
    {
        return _status.heroData.data.masteryLvl[index] > 0;
    }
    protected int MasteryLevel(int index)
    {
        return _status.heroData.data.masteryLvl[index];
    }
    public virtual void InitAllMasteries()
    {

    }
    public virtual void InitAttributes()
    {

    }

    //==== MANA GAIN ====//
    protected int totalEnemyHit = 0;
    public void AddEnemyHit()
    {
        totalEnemyHit++;
    }
    public void BasicAttackManaRegen()
    {
        if (totalEnemyHit == 0) return;

        float result;

        try
        {
            result = _status.basicAtkRegen[attackIndex].manaIncreaseOnInitialHit +
                _status.basicAtkRegen[attackIndex].manaIncreasePerExtraEnemy * (totalEnemyHit - 1);
            if (result > _status.basicAtkRegen[attackIndex].maximumManaIncrease)
                result = _status.basicAtkRegen[attackIndex].maximumManaIncrease;
        }
        catch
        {
            result = 5;
        }

        //if(_status.socketEffect.manaDrain.count > 0)
        //{
        //    result += (result * _status.socketEffect.manaDrain.values[0]);
        //}

        _status.AddManaValue(result);
        totalEnemyHit = 0;

        extraDamageWhole = 0;
        //attackBreak = 0;
    }
    public virtual void ManaGained(float amt)
    {

    }

    public virtual void DamageTaken(float amt, Transform source)
    {
        /*if (_status.socketEffect.thorn > 0f && source.GetComponent<EnemyHealth>() != null)
            source.GetComponent<EnemyHealth>()
                .TakeDamage(transform, amt * _status.socketEffect.thorn / 100, false, 0, _status.pierce, _status.armorBreak, out bool dead);
        */

        if (_status.setEffect.abyssGlacial > 0)
        {
            FrostNovaAccumulation(amt);
        }
    }

    //==== DAMAGE OUTPUT ====//
    protected float motionValue;
    protected string motionSourceKey;
    public void SetMotionValue(float percent)
    {
        SetMotionValue(percent, "");
    }
    public void SetMotionValue(float percent, string dmgSourceKey)
    {
        motionValue = percent;
        motionSourceKey = dmgSourceKey;

        //calculate crit here, so all enemies share the same damage
        isCritical = Random.Range(0, 100) < _status.FinalCriticalRate + extraCriticalOnce;
    }

    [HideInInspector]
    public float lifeDrainCooldown;

    bool lifeDrainOnCD;
    protected void UpdateLifeDrainCooldown()
    {
        if (!lifeDrainOnCD) return;

        lifeDrainCooldown -= Time.deltaTime;
        if (lifeDrainCooldown > 0f)
        {
            _status.heroStatusEffect.SetNotification("icon_rune_" + SocketType.Drain.ToString().ToLower() + "_disable", Mathf.CeilToInt(lifeDrainCooldown).ToString());
        }
        else
        {
            lifeDrainOnCD = false;
            _status.heroStatusEffect.RemoveNotification("icon_rune_" + SocketType.Drain.ToString().ToLower() + "_disable");
        }
    }

    //--------------------//
    //--- BURST CD 

    [HideInInspector]
    public float burstCooldown;

    [ShowIf("@GetComponent<HeroStatus>().heroReference.hero == Hero.Amy")]
    public float amy_heavyblow_break, amy_heavyblow_stunchance, amy_heavyblow_dmgboost;
    [HideInInspector]
    protected bool amy_heavyblow_proc, amy_heavyblow_active;
    protected int amy_heavyblow_stack;
    [ShowIf("@GetComponent<HeroStatus>().heroReference.hero == Hero.Amy")]
    public Transform heavyblow_hit_fx;
    [ShowIf("@GetComponent<HeroStatus>().heroReference.hero == Hero.Elze")]
    public Transform chillingtouch_hit_fx;

    protected bool elze_freeze_break, elze_freeze_crit, elze_batk_chill_proc;
    protected float elze_extraMv_on_freeze, elze_freeze_duration;
    protected float elze_chillingtouch_chilled_mod, elze_chillingtouch_frozen_mod;

    bool burstOnCD;
    protected void UpdateBurstCooldown()
    {
        if (!burstOnCD) return;

        burstCooldown -= Time.deltaTime;
        if (burstCooldown > 0f)
        {
            _status.heroStatusEffect.SetNotification("icon_rune_" + SocketType.Burst.ToString().ToLower() + "_disable",
                Mathf.CeilToInt(burstCooldown).ToString());
        }
        else
        {
            burstOnCD = false;
            _status.heroStatusEffect.RemoveNotification("icon_rune_" + SocketType.Burst.ToString().ToLower() + "_disable");
        }
    }

    public bool DamageEnemy(EnemyHealth target, DamageRequest dmgReq)//, AttackType attackType, DamageModifierType damageType, float _knockback, float _stun)
    {
        victim = target;

        float ATTACK_MV = motionValue;

        //if (damageType == DamageModifierType.BasicAttack)
        //    BasicAttackCustomize();

        SetBuffDamage();

        float finalDamage = _status.GetModifiedFinalDamage() + extraDamageWhole;
        ApplyBonusHPConversion(ref finalDamage);
        ApplySoulRipperBonus(ref dmgReq, ref finalDamage);

        //Debug.Log("base mv : " + ATTACK_MV);

        //Elze - Extras on Frozen Enemies
        if (target.HasStatusEffect(StatusEffects.freeze, out _))
        {
            ATTACK_MV += elze_extraMv_on_freeze;

            float extramv = ATTACK_MV * elze_chillingtouch_frozen_mod;
            ATTACK_MV += extramv;

            if (elze_freeze_break)
            {
                target.RemoveStatusEffectImmediate(StatusEffects.freeze);
            }

            if (elze_freeze_crit)
            {
                isCritical = true;
                elze_freeze_crit = false;
            }

            //Debug.Log("frozen mv : " + ATTACK_MV);

        }
        //Elze - Extras on Chilled Enemies
        if (target.HasStatusEffect(StatusEffects.chill, out _))
        {
            float extramv = ATTACK_MV * elze_chillingtouch_chilled_mod;
            ATTACK_MV += extramv;

            // Debug.Log("chill mv : " + ATTACK_MV);

        }

        if (dmgReq.damageModifierType == DamageModifierType.BasicAttack)
        {
            BasicAttackCustomize();

            //Claris - Initiate damage increase
            finalDamage += extraBasicAttackDamage;

            finalDamage = finalDamage + (finalDamage * _status.FinalBasicAtkDamage) / 100f;

            //gladiator
            if (alaster_gladiator_mod > 1)
                finalDamage *= alaster_gladiator_mod;

        }
        else if (dmgReq.damageModifierType == DamageModifierType.SkillAttack)
        {
            finalDamage = finalDamage + (finalDamage * _status.FinalSkillAtkDamage) / 100f;
        }

        finalDamage *= ATTACK_MV / 100f;

        ApplyBuffedDamage(ref finalDamage);

        //Amy - Heavy Blow
        float amy_extrabreak = amy_heavyblow_proc ? amy_heavyblow_break : 0;
        if (amy_heavyblow_proc)
        {
            float hb_addDmg = finalDamage * amy_heavyblow_dmgboost / 100;
            finalDamage += hb_addDmg;

            Instantiate(heavyblow_hit_fx.gameObject, target.transform.position + Vector3.up * 2, transform.rotation);
        }

        //Elze - Chilling Touch proc
        if (elze_batk_chill_proc)
        {
            DamageRequest req = new DamageRequest()
            {
                statusEffect = StatusEffects.chill,
                statusEffectDamage = elze_freeze_duration,
                statusEffectDuration = 10,
                hero = Hero.Elze
            };
            StatusEffectManager.instance.SetStatusEffect(target.transform, req);
            Instantiate(chillingtouch_hit_fx.gameObject, target.transform.position + Vector3.up * 2, transform.rotation);
        }
        //===========//


        if (isCritical)
        {
            finalDamage *= (1 + (_status.FinalCriticalDamage / 100));
        }

        var damageRequest = new DamageRequest(dmgReq)
        {
            damage = finalDamage,
            isCritical = isCritical,
            knockback = dmgReq.knockback,
            pierce = _status.pierce,
            _break = attackBreak + amy_extrabreak,
            accuracy = _status.FinalAccuracy,
            cdmg = _status.FinalCriticalDamage,
            damageSourceKey = motionSourceKey
        };

        target.TakeDamage(transform, damageRequest, out DamageResult result);
        if (!result.isMiss)
        {
            if (damageRequest.isCritical)
            {
                GameManager.instance.vfxManager.Crit(target.transform.position, dmgReq.attackType);
                ApplyDrain(damageRequest.damage);
            }

            //Stun Attack
            if (!result.isDead)
            {
                if (dmgReq.stunDuration > 0)
                {
                    target.GetComponent<EnemyAI>().Stun(dmgReq.stunDuration);
                }
                else
                {
                    //try amy's stun chance
                    if (amy_heavyblow_proc)
                    {
                        target.GetComponent<EnemyAI>().Stun(1);
                    }
                }
            }

            //Basic Attacks with Debuffs
            if (affectBleeding)
            {
                //apply bleed
                StatusEffectManager.instance.SetStatusEffect(target.transform, sp_req);
            }

            UniqueAttackProcs(target.transform.position);
            if (dmgReq.damageModifierType == DamageModifierType.BasicAttack)
            {
                BasicAttackHit();
                OnBasicAttackHit();
            }
            else
            {
                OnSkillAttackHit();
            }
        }

        elze_extraMv_on_freeze = 0;
        extraPctDamageOnce = 0;
        extraDamageRaw = 0;
        return result.isMiss;
    }

    public void ApplyDrain(float damage)
    {
        if (_status.socketEffect.drain.count > 0)
        {
            if (lifeDrainCooldown <= 0f)
            {
                var restoreValue = damage * (_status.socketEffect.drain.GetValue(0) / 100f); // 10000 * 1 / 100 = 100
                var pct20MaxHP = (_status.socketEffect.drain.GetValue(1) / 100f) * _status.FinalMaxHP;
                if (restoreValue > pct20MaxHP)
                    restoreValue = pct20MaxHP;

                _health.RestoreHp(restoreValue, true, true);

                lifeDrainCooldown = _status.socketEffect.drain.GetValue(2);
                lifeDrainOnCD = true;
            }
        }
    }

    public bool ApplyBurst()
    {
        if (_status.socketEffect.burst.count > 0)
        {
            if (burstCooldown <= 0f)
            {
                if (ApplyRandomize(_status.socketEffect.burst.GetValue(0)))
                {
                    burstCooldown = _status.socketEffect.burst.GetValue(1);
                    burstOnCD = true;
                    return true;
                }
            }
        }

        return false;

    }

    //Mastery Modifiers
    protected EnemyHealth victim;
    protected float extraPctDamageOnce;    //extra damage for one hit, one enemy. eg, claris sharp cut that increases damage to bleed enemies only
    protected float extraDamageWhole;   //extra damage for a whole attack, hits all enemies. eg, claris initiate hit
    protected float extraDamageRaw;   //extra damage for one hit (raw value)
    protected float extraBasicAttackDamage;   //fixed extra damage from a mastery. careful, please reset the number in responsible mastery
    protected int boostedBasicAttackCount; //number of basic attack boosted by certain mastery
    protected float extraBreakFixed;   //fixed extra break from a mastery. careful, please reset the number in responsible mastery
    protected virtual void BasicAttackCustomize()
    {

    }
    protected virtual void UseBoostedBasicAttack()
    {

    }
    public virtual void InflictNegativeEffects()
    {

    }
    public virtual void EndNegativeEffects()
    {

    }
    public virtual void CleanseDebuff()
    {

    }
    public virtual void DisableAntiDeadblow()
    {

    }
    protected virtual void OnEvasion()
    {

    }
    protected virtual void OnMovementInput()
    {

    }

    protected virtual void SetBuffDamage()
    {

    }
    protected virtual void ApplyBuffedDamage(ref float finalDmg)
    {
        if (extraPctDamageOnce > 0f)
            finalDmg *= (1 + extraPctDamageOnce);

        extraPctDamageOnce = 0f;
    }

    protected virtual void ApplyBonusHPConversion(ref float finalDmg)
    {
    }
    protected virtual void ApplySoulRipperBonus(ref DamageRequest finalDmg, ref float dmg)
    {
    }

    //==== ATTACK MODIFIERS ====//
    protected bool isCritical;
    protected float extraCriticalOnce;
    protected void ApplyCritical(float damage, out float out_Dmg, out bool out_Crit)
    {
        float totalCrit = _status.FinalCriticalRate + extraCriticalOnce;

        out_Crit = Random.Range(0, 100) <= totalCrit;

        if (out_Crit) out_Dmg = damage * (1 + (_status.FinalCriticalDamage / 100));
        else out_Dmg = damage;

        //Debug.Log("extra crit : " + extraCriticalOnce);
        extraCriticalOnce = 0;
    }

    public virtual float GetFinalSkillDamage()
    {
        float baseDmg = _status.GetModifiedFinalDamage() +
            (_status.GetModifiedFinalDamage() * (_status.FinalSkillAtkDamage / 100f));

        _status.extraSkillDmg = 0;
        _status.maidSkillDmg = 0;
        _status.heroStatusEffect.RemoveNotification("costume_" + CostumeSet.Maid.ToString().ToLower() + "_0");

        return baseDmg;
    }

    protected float alaster_gladiator_mod;
    public virtual float GetFinalBasicDamage()
    {
        float baseDmg = _status.GetModifiedFinalDamage() + (_status.GetModifiedFinalDamage() * (_status.FinalBasicAtkDamage / 100f));

        if (alaster_gladiator_mod > 1)
            baseDmg *= alaster_gladiator_mod;

        return baseDmg;
    }

    protected bool ApplyRandomize(float applyChance) => applyChance >= Random.Range(0f, 100f);

    //====== SET EFFECTS ======//
    DamageRequest sp_req;
    [HideInInspector]
    public bool affectBleeding;
    protected void ApplyBleeding(float chance, DamageRequest req)
    {
        if (Random.Range(0, 100) <= chance)
        {
            affectBleeding = true;
            sp_req = req;

            //Debug.Log("bleed success");
        }
        else
        {
            affectBleeding = false;

            //Debug.Log("bleed fail");
        }
    }
    void ResetSpecialAttack()
    {
        affectBleeding = false;
    }

    public void UniqueAttackProcs(Vector3 pos)
    {
        UniqueAttackProcs(pos, _status.GetModifiedFinalDamage());
    }
    public void UniqueAttackProcs(Vector3 pos, float _damage)
    {
        if (_status.costumeEffect.crovenBlast > 0) // activate when 5/5 croven equipped
        {
            DamageRequest crovenReq = new DamageRequest()
            {
                damage = _damage,
                isCritical = false,
                hero = _status.heroReference.hero
            };

            // gm.uniqueAtkProcs.Proc_BlackGale(pos, crovenReq);
        }

        if (_status.costumeEffect.schoolManaSteal > 0) // activate when 5/5 academia equipped
        {
            // var proc = GameManager.instance.uniqueAtkProcs.Proc_ManaSteal(pos);
            // if (proc)
            // {
            //     _status.AddManaValue(50f, true);
            //     //HudManager.instance.PopHealMp(transform, 1, 50f);
            //     //GameManager.instance.vfxManager.PotionMP(_status.potionFxPos, 0);
            // }
        }

        // if (_status.setEffect.mardukVigor > 0)
        // {
        //     ProcDemonicVigorStacks();
        // }
    }
    public void UpdateSetEffectCooldown()
    {
        if (!isInitialized) return;

        // if (_status.setEffect.demonicCurse > 0f)
        // {
        //     UpdateDemonicCurseCooldown();
        // }

        if (_status.setEffect.mardukVigor > 0)
        {
            UpdateDemonicVigorCooldown();
        }

        if (_status.setEffect.abyssVoid > 0)
        {
            UpdateEntropyCD();
        }

        if (_status.setEffect.abyssInferno > 0)
        {
            UpdatePhoenixTriggerCD();
        }

        UpdateWindveilDuration();
    }

    public void OnPerfectEvasionSuccess()
    {
        PartyManager.PerfectEvasionSuccess();
        TriggerMaidEvasion();
        TriggerWindVeil();
    }
    //Marlene - Demon Curse
    bool canUseDemonCurse;
    float demonicCurseCD = 10f, currentDemonicCD;
    // private void UpdateDemonicCurseCooldown()
    // {
    //     if (!canUseDemonCurse)
    //     {
    //         currentDemonicCD -= Time.deltaTime;
    //         if (currentDemonicCD <= 0f)
    //         {
    //             currentDemonicCD = demonicCurseCD;
    //             canUseDemonCurse = true;
    //             _status.heroStatusEffect.RemoveNotification("setEffect_" + EquipSet.Demonic.ToString().ToLower());
    //         }

    //         else
    //         {
    //             _status.heroStatusEffect.SetNotification("setEffect_" + EquipSet.Demonic.ToString().ToLower(), Mathf.CeilToInt(currentDemonicCD).ToString());
    //         }
    //     }

    //     if (GameManager.instance.ActiveHero == _status)
    //     {
    //         if (GameManager.instance.detectedEnemies > 0 && canUseDemonCurse)
    //         {
    //             GameManager.instance.uniqueAtkProcs.ShockwaveCurse((1 + (_status.FinalSkillAtkDamage / 100)) * _status.GetModifiedFinalDamage(), transform, _status.setEffect.demonicCurse);
    //             canUseDemonCurse = false;
    //         }
    //     }
    // }

    //Marduk - Vigor
    int vigorstack = 0;
    float currentDemonicVigorCD;
    // private void ProcDemonicVigorStacks()
    // {
    //     if (currentDemonicVigorCD > 0) return;

    //     vigorstack++;

    //     if (vigorstack >= 20)
    //     {
    //         vigorstack = 0;
    //         _status.heroStatusEffect.RemoveNotification("setEffect_" + EquipSet.MardukVigor.ToString().ToLower());


    //         //float vigordmg = _status.setEffect.mardukVigor * GetFinalBasicDamage();
    //         float vigordmg = _status.setEffect.mardukVigor * _status.GetModifiedFinalDamage();

    //         //Debug.Log(string.Format("Vigor Dmg {0}00% => {1}. Heals {2}% ", _status.setEffect.mardukVigor * 2, vigordmg, _status.setEffect.mardukVigor));

    //         DamageRequest vigorReq = new DamageRequest()
    //         {
    //             damage = vigordmg,
    //             isCritical = false,
    //             hero = _status.heroReference.hero
    //         };

    //         gm.uniqueAtkProcs.DemonicVigorExplosion(transform, vigorReq);
    //         _health.RestoreHpPercent(10, true);
    //     }
    //     else
    //     {
    //         _status.heroStatusEffect.SetNotification("setEffect_" + EquipSet.MardukVigor.ToString().ToLower(), "", vigorstack);
    //     }

    //     currentDemonicVigorCD = 0.35f;
    // }
    private void UpdateDemonicVigorCooldown()
    {
        if (currentDemonicVigorCD > 0)
            currentDemonicVigorCD -= Time.deltaTime;
    }

    //Maid - Perfect Sweep
    [HideInInspector] public bool maidPerfectReady;
    [HideInInspector] float maidPerfectEvaCD = 10f;
    [HideInInspector] float currentMaidCD;
    public void UpdateMaidEvasionCD()
    {
        if (maidPerfectReady) return;
        if (_status.costumeEffect.maidValue > 0f)
        {
            currentMaidCD -= Time.deltaTime;
            if (currentMaidCD <= 0f)
            {
                currentMaidCD = maidPerfectEvaCD;
                maidPerfectReady = true;
                _status.heroStatusEffect.RemoveNotification("costume_" + CostumeSet.Maid.ToString().ToLower() + "_1");
            }
            else
            {
                _status.heroStatusEffect.SetNotification("costume_" + CostumeSet.Maid.ToString().ToLower() + "_1",
                    Mathf.CeilToInt(currentMaidCD).ToString());
            }
        }
    }
    private void TriggerMaidEvasion()
    {
        //Debug.Log("Maid");
        maidPerfectReady = false;
        if (_status.costumeEffect.maidValue > 0f)
        {
            _status.maidSkillDmg = 25f; // 25%
            _status.heroStatusEffect.SetNotification("costume_" + CostumeSet.Maid.ToString().ToLower() + "_0", "");
        }
    }

    //Abyssal - Entropy
    [FoldoutGroup("Set Effects")]
    public float entropyCd;
    public void TriggerEntropyDrain()
    {
        if (_status.setEffect.abyssVoid <= 0) return;
        if (entropyCd > 0) return;

        float finalMp = (_status.FinalMaxMP * 0.05f) + 50;
        _status.AddManaValue(finalMp, true);

        entropyCd = 10;

        if (_status.setEffect.abyssVoid < 2) return;
        _status.entropyExtraMp = 3;
    }
    private void UpdateEntropyCD()
    {
        if (entropyCd > 0)
        {
            entropyCd -= Time.deltaTime;
            _status.heroStatusEffect.SetNotification("setEffect_entropy", Mathf.CeilToInt(entropyCd).ToString());
        }
        else
        {
            _status.heroStatusEffect.RemoveNotification("setEffect_entropy");
            _status.entropyExtraMp = 0;
        }
    }

    //Abyssal - Wind Veil
    [FoldoutGroup("Set Effects")]
    public float windVeilDuration;
    public bool windVeilActive;
    public void TriggerWindVeil()
    {
        var windveilLv = _status.setEffect.abyssGale;
        if (windveilLv < 1) return;

        //give windveil buff
        windVeilActive = true;
        windVeilDuration = 3;

        _health.buff_abyssal_windveil = true;

        if (windveilLv > 1)
        {
            _status.abyssal_windveil_aspd = 50;
            ResetAttackSpeed();
        }
    }
    private void UpdateWindveilDuration()
    {
        if (windVeilDuration < 0) return;

        windVeilDuration -= Time.deltaTime;
        if (windVeilDuration <= 0)
        {
            windVeilActive = false;
            _health.buff_abyssal_windveil = false;
            _status.abyssal_windveil_aspd = 0;
            ResetAttackSpeed();

            _status.heroStatusEffect.RemoveNotification("setEffect_windveil");
        }
        else
        {
            _status.heroStatusEffect.SetNotification("setEffect_windveil", Mathf.CeilToInt(windVeilDuration).ToString());
        }
    }

    //Abyssal - Frost Nova
    [FoldoutGroup("Set Effects")]
    public float frostNovaAccumulatedDmg;
    private void FrostNovaAccumulation(float damageTaken)
    {
        frostNovaAccumulatedDmg += damageTaken;
        float threshold = 0.1f * _status.FinalMaxHP;

        if (frostNovaAccumulatedDmg >= threshold)
        {
            //Trigger explosion
            float frostNovaDmgMod = _status.setEffect.abyssGlacial > 1 ? 4 : 2;
            ApplyCritical(frostNovaDmgMod * _status.FinalMaxHP, out float finalDmg, out bool crit);

            DamageRequest novaReq = new DamageRequest()
            {
                damage = finalDmg,
                isCritical = crit,
                hero = _status.heroReference.hero
            };

            // gm.uniqueAtkProcs.FrostNovaExplosion(transform, novaReq);
            frostNovaAccumulatedDmg = 0;
        }
    }

    //Abyssal - Phoenix Trigger
    [FoldoutGroup("Set Effects")]
    public float phoenixTriggerInCd, phoenixTriggerOutCd;
    private void TriggerPhoenixResetSkill()
    {
        if (phoenixTriggerInCd > 0) return;

        RefreshAllSkillCooldown();
        phoenixTriggerInCd = 20;

    }
    private void TriggerPhoenixHeal()
    {
        if (phoenixTriggerOutCd > 0) return;

        _health.RestoreHpPercent(100, true);
        phoenixTriggerOutCd = 30;
    }
    private void UpdatePhoenixTriggerCD()
    {
        if (phoenixTriggerInCd > 0)
        {
            phoenixTriggerInCd -= Time.deltaTime;
            _status.heroStatusEffect.SetNotification("setEffect_phoenix_in", Mathf.CeilToInt(phoenixTriggerInCd).ToString());
        }
        else
        {
            _status.heroStatusEffect.RemoveNotification("setEffect_phoenix_in");
        }

        if (phoenixTriggerOutCd > 0)
        {
            phoenixTriggerOutCd -= Time.deltaTime;
            _status.heroStatusEffect.SetNotification("setEffect_phoenix_out", Mathf.CeilToInt(phoenixTriggerOutCd).ToString());
        }
        else
        {
            _status.heroStatusEffect.RemoveNotification("setEffect_phoenix_out");
        }
    }



    public virtual void TakeDirectDamage()
    {

    }

    public virtual void StaggerResisted()
    {
        HudManager.instance.PopResist(transform);
    }
    protected virtual void BasicAttackHit()
    {
        UseBoostedBasicAttack();
    }

    protected virtual void OnBasicAttackHit()
    {

    }
    protected virtual void OnSkillAttackHit()
    {

    }

    // public virtual void ResetBonusSpeed()
    // {
    //     _status.heroStatusEffect.RemoveNotification("am_fieryupper");
    //     _status.amy_field_aspd = 0;
    //     _status.amy_field_mspd = 0;
    //     ResetAttackSpeed();
    //     ResetMoveSpeed();

    //     _status.passionFields = new List<Amy_PassionField>();
    // }

    // public virtual void ResetBonusProtection()
    // {
    //     _health.damageRedPct = 0;
    //     _status.heroStatusEffect.RemoveNotification("al_risingslash");

    //     _status.protectionFields = new List<Alaster_ProtectionField>();
    // }
    // public virtual void ResetBonusMorale()
    // {
    //     _status.louisa_morale_batk = 0;
    //     _status.louisa_morale_pres = 0;
    //     _status.heroStatusEffect.RemoveNotification("lu_morale");

    //     _status.moraleFields = new List<Louisa_MoraleBoost>();
    // }



    protected void SetMegaArmor(bool on)
    {
        if (_health) _health.megaArmor = on;
        /*
        if (on)
            _status.heroStatusEffect.SetNotification("mega_armor", "");
        else
            _status.heroStatusEffect.RemoveNotification("mega_armor");*/
    }
    protected void EnableMegaArmor()
    {
        SetMegaArmor(true);
    }
    protected void DisableMegaArmor()
    {
        SetMegaArmor(false);
    }

    public virtual void Disengage()
    {
        if (_status)
            if (_status.heroStatusEffect)
                _status.heroStatusEffect.RemoveNotification("status_combat");
    }
    public virtual void Engage()
    {
        if (_status)
            if (_status.heroStatusEffect)
                _status.heroStatusEffect.SetNotification("status_combat", "");
    }


    //Ex skills
    // protected bool ExActivated(int skillIndex)
    // {
    //     if (skills[skillIndex].ex_rune == null) return false;
    //     if (skills[skillIndex].ex_rune.Count == 0) return false;

    //     return _status.HasExRune(skills[skillIndex].ex_rune[0].rune);
    // }
    // protected float ExSkillValue(int skillIndex)
    // {
    //     if (skills[skillIndex].ex_rune.Count < 1) return 0;

    //     return _status.GetExVal(skills[skillIndex].ex_rune[0].rune);
    // }
    // protected float ExSkillModifier(int skillIndex)
    // {
    //     if (skills[skillIndex].ex_rune.Count < 1) return 0;

    //     return skills[skillIndex].ex_rune[0].modifier;
    // }

    public static event System.Action<Hero, bool> OnRageStateChange;

    protected void OnRageStateChangeCall(Hero hero, bool isReady)
    {
        OnRageStateChange(hero, isReady);
    }

    protected int toggleweaponindex = -1;
    public virtual void ToggleWeapon()
    {
        toggleweaponindex++;
        if (toggleweaponindex > 2)
        {
            toggleweaponindex = 0;
        }

        switch (toggleweaponindex)
        {
            case 0:
                _props.HideWeapon();
                break;
            case 1:
                _props.ShowWeapon();
                _props.AttachWeaponToHand();
                break;
            case 2:
                _props.ShowWeapon();
                _props.AttachWeaponToSheath();
                break;
        }
    }
}