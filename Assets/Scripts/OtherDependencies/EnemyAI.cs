using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;
using Sirenix.OdinInspector;

public class EnemyAI : MonoBehaviour
{
    [FoldoutGroup("General")] public EnemyThreatBehavior aggroType = EnemyThreatBehavior.aggressive; //Default is aggresive
    [FoldoutGroup("General")] public EnemyMovementType movementType;
    [FoldoutGroup("General")] public bool instantAttackOnAggro;
    [FoldoutGroup("General")] public float delayAttackOnAggro;

    //Stun Mech
    [FoldoutGroup("General")] public float stunFxOffset = 3.5f;
    [FoldoutGroup("General")] public bool waitRiseAnimation; //If checked, play 'rise' animation +before enableMove

    [FoldoutGroup("General")] public AudioClip[] sfx;
    [FoldoutGroup("General")] public bool disableAI;

    //References
    protected GameManager gm;
    protected EnemyStatus status;
    protected EnemyHealth health;
    [ShowInInspector, ReadOnly]
    public Transform TARGET
    {
        get
        {
            if (decoy) return decoy;
            else return mainTarget;
        }
    }
    protected Transform mainTarget;
    protected CharacterController controller;
    protected Animator anim;
    protected Transform caster;
    protected HudManager hud;
    protected AudioSource _audio;
    EnemyLink links;

    //Movement
    protected bool canMove;
    protected bool engageCombat;
    protected bool biting;
    protected bool isStatic, wallDetect;

    Vector3 initialSpawnPoint, newPosition;
    protected float distanceToTarget = 9999;
    private bool isAdded = false;
    [HideInInspector] public bool isAggroed = false;
    private bool isInited = false;

    float poisonSlow;
    float cursedSlow;
    float tremorSlow;
    float bulletRainSlow;
    float chillSlow;
    public float FinalAnimationSpeed
    {
        get
        {
            float startingValue = 1f;

            startingValue *= (1f - poisonSlow);

            startingValue *= (1f - cursedSlow);

            startingValue *= (1f - tremorSlow);

            startingValue *= (1f - bulletRainSlow);

            startingValue *= (1f - chillSlow);

            if (startingValue < 0.3f) startingValue = 0.3f;

            if (isFrozen) startingValue = 0f;
            return startingValue;
        }
    }

    #region SUBSCRIBTIONS 
    private void OnEnable()
    {
        Subscribe();
    }

    public virtual void OnDisable()
    {
        Unsubscribe(); RemoveDetectedEnemies();

        if (isAggroed)
        {
            isAggroed = false;
            gm.RemoveAggroedEnemies();
        }

        //EndChase();
    }
    #endregion

    protected virtual void Subscribe()
    {
        PartyManager.OnHeroSwitch += RegainAggro;
        GameManager.OnGameStateChanged += MovementOverState;
    }

    protected virtual void Unsubscribe()
    {
        PartyManager.OnHeroSwitch -= RegainAggro;
        GameManager.OnGameStateChanged -= MovementOverState;
    }

    protected virtual void Start()
    {
        //Init General references
        Init();
    }

    public void Init()
    {
        if (isInited) return;

        gm = GameManager.instance;
        hud = gm.GetComponent<HudManager>();
        status = GetComponent<EnemyStatus>();
        health = GetComponent<EnemyHealth>();
        controller = GetComponent<CharacterController>();
        _audio = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();
        isStatic = movementType == EnemyMovementType.stayInPlace;

        links = GetComponent<EnemyLink>();

        //Init AI specific references
        InitUniqueReferences();

        //init position
        initialSpawnPoint = transform.position;
        Timing.RunCoroutine(SetNewDestination().CancelWith(gameObject), Segment.Update);

        isInited = true;
    }

    protected void AddToDetectedEnemies()
    {
        if (!isAdded)
        {
            gm.AddToDetectedEnemies();
            isAdded = true;
        }
    }
    private void RemoveDetectedEnemies()
    {
        if (isAdded)
        {
            gm.RemoveFromDetectedEnemies();
            isAdded = false;
        }
    }
    protected virtual void InitUniqueReferences()
    {

    }

    public void MovementOverState(GameState state)
    {
        if (state == GameState.PAUSE)
        {
            if (gm.IsEngagedInCombat())
                anim.speed = 0f;
        }
        else
            anim.speed = gm.BaseTimeScale;
    }

    public void EnableMove()
    {
        if (taunted) return;
        if (isStunned) return;
        if (isFrozen) return;
        if (isParalyzed) return;
        canMove = true;
        OnMoveEnabled();
    }
    public virtual void DisableMove()
    {
        Timing.KillCoroutines(moveHandler);
        canMove = false;
    }
    protected void MoveForward()
    {
        MoveForward(1);
    }
    protected void MoveForward(float speedModifier)
    {
        MoveForward(speedModifier, false);
    }
    protected void MoveForward(float speedModifier, bool reverse)
    {
        if (banished) return;

        float mod = reverse ? -1 : 1;

        anim.SetFloat("speed", mod);

        float finalspeed = speedModifier * FinalAnimationSpeed;
        if (finalspeed < 0.3f) finalspeed = 0.3f;

        controller.Move(transform.forward * status.moveSpeed * finalspeed * Time.deltaTime * mod);
        if (!engageCombat) controller.Move(Vector3.down * 25 * Time.deltaTime);
    }
    [HideInInspector] public bool banished;
    protected void UpdateGravity()
    {
        if (banished) return;

        if (!biting) controller.Move(Vector3.down * 25 * Time.deltaTime);
    }
    protected void StopMove()
    {
        if (anim) anim.SetFloat("speed", 0);
    }
    protected void EnableSpeed()
    {
        if (anim) anim.SetFloat("speed", 1);
    }
    protected virtual void OnMoveEnabled()
    {

    }

    //Look At
    public bool overrideTurnspeed;
    [ShowIf("overrideTurnspeed")]
    public float originalTurnspeed;
    protected void LookAtTarget()
    {
        if (engageCombat)
        {
            if (!TARGET) return;
            LookAtTarget(TARGET.position);
        }
        else LookAtTarget(newPosition);
    }
    protected void LookAtTarget(Vector3 lookTarget)
    {
        float tspd;

        if (overrideTurnspeed)
            tspd = originalTurnspeed;
        else tspd = 10;
        LookAtTarget(lookTarget, tspd);
    }
    protected void LookAtTarget(Vector3 lookTarget, float turnSpeed)
    {
        if (health.dead) return;
        if (isFrozen) return;
        Vector3 dir = lookTarget - transform.position;
        if (dir != Vector3.zero)
        {
            Vector3 D = new Vector3(dir.x, 0, dir.z);
            Quaternion lookRot = Quaternion.LookRotation(D);
            Vector3 rot = Quaternion.Slerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime * FinalAnimationSpeed).eulerAngles;
            transform.eulerAngles = rot;
        }
    }
    protected void InstaLookAtTarget()
    {
        if (health.dead) return;
        if (!TARGET) return;
        if (isFrozen) return;

        Vector3 lookAt = new Vector3(TARGET.position.x, transform.position.y, TARGET.position.z);
        transform.LookAt(lookAt);
    }

    protected void InstaLookAt(Vector3 lookTarget)
    {
        if (health.dead) return;
        if (!TARGET) return;
        if (isFrozen) return;

        transform.LookAt(lookTarget);
    }

    protected IEnumerator<float> LateLookAtTarget()
    {
        while (isAttacking && !isStunned && !isParalyzed && !isFrozen)
        {
            yield return 0;
            LookAtTarget();
        }
    }

    protected bool lookingAtTarget;
    protected IEnumerator<float> LateLookAtTarget(Transform target, float turnSpeed)
    {
        lookingAtTarget = true;

        while (isAttacking && !isStunned && lookingAtTarget)
        {
            yield return 0;

            if (target)
            {
                LookAtTarget(target.position, turnSpeed);
            }
        }
    }
    public void StopTargetLook()
    {
        lookingAtTarget = false;
    }

    protected void PlaySFX(int idx)
    {
        if (idx < sfx.Length)
            if (sfx[idx]) _audio.PlayOneShot(sfx[idx]);
    }

    //==== Off Combat ====//
    protected void UpdateOffCombatMovement()
    {
        if (isStatic) return;
        if (status.roamDistance == 0) return;
        if (!canMove) return;
        if (engageCombat) return;
        if (isFrozen) return;

        distanceToTarget = Vector3.Distance(transform.position, newPosition);
        LookAtTarget();

        if (distanceToTarget > 2)
        {
            MoveForward();

            if (HittingWall())
            {
                DestinationReached();
            }
        }
        else DestinationReached();
    }
    protected void DestinationReached()
    {
        StopMove();
        DisableMove();
        Timing.RunCoroutine(SetNewDestination().CancelWith(gameObject), Segment.Update);
    }
    IEnumerator<float> SetNewDestination()
    {
        bool correctDestination;
        int numberOfTries = 0;
        wallDetect = false;

        yield return Timing.WaitForSeconds(UnityEngine.Random.Range(0.8f, 1.2f) * status.idleTime);

        do
        {
            correctDestination = true;

            //get random position around initial spawn point
            float randomX = initialSpawnPoint.x - UnityEngine.Random.Range(-1f, 1f) * status.roamDistance;
            float randomZ = initialSpawnPoint.z - UnityEngine.Random.Range(-1f, 1f) * status.roamDistance;
            newPosition = new Vector3(randomX, transform.position.y, randomZ);

            //make sure the destination is not too close
            if (Vector3.Distance(transform.position, newPosition) < status.roamDistance / 2)
                correctDestination = false;

            //make sure it's not hitting a wall or other enemy
            Vector3 direction = newPosition - transform.position;
            if (Physics.Raycast(transform.position, direction, status.roamDistance, 1 << 14 | 1 << 10))
                correctDestination = false;

            //get out of infinite loop when fails
            numberOfTries++;
            if (numberOfTries > 10)
            {
                newPosition = transform.position;
                correctDestination = true;
            }

        } while (!correctDestination);

        EnableMove();
        Timing.RunCoroutine(EnableWallDetection());
    }
    protected bool HittingWall()
    {
        if (!wallDetect) return false;

        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, 2, 1 << 14 | 1 << 0))
            return true;
        else return false;
    }
    IEnumerator<float> EnableWallDetection()
    {
        yield return Timing.WaitForSeconds(2f);
        wallDetect = true;
    }

    //==== Engage Combat ====//
    protected bool targetOutOfSight, chase;
    float targetOutOfSightDuration, offSightThreshold = 4;
    protected void CheckTarget()
    {
        if (TARGET == null)
        {
            try
            {
                mainTarget = gm.ActiveHero.transform;
            }
            catch { }
        }

        if (TARGET)
        {
            distanceToTarget = Vector3.Distance(transform.position, TARGET.position);
            if (TargetInVicinity())
            {
                if (!isAggroed)
                {
                    isAggroed = true;
                    gm.AddToAggroedEnemies();
                }
                chase = true;
            }
        }
    }
    protected bool TargetInVicinity()
    {
        bool inSight = distanceToTarget <= status.visionRange;
        targetOutOfSight = !inSight;

        return inSight;
    }
    protected bool TargetInAttackRange()
    {
        return distanceToTarget <= status.attackRange;
    }
    protected void UpdateTargetOutOfSight()
    {
        targetOutOfSightDuration += Time.deltaTime;
        if (targetOutOfSightDuration >= offSightThreshold)
        {
            //target is outside vision for too long. stop chasing.
            targetOutOfSightDuration = 0;
            StopMove();
            EndChase();
        }
    }
    public virtual void EndChase()
    {
        if (isAggroed)
        {
            isAggroed = false;
            gm.RemoveAggroedEnemies();
        }

        chase = false;
        engageCombat = false;
        if (status.useBossHpBar) status.hpBarManager.DisEngage();
    }
    protected void UpdateCombatMovement()
    {
        UpdateTauntedTime();
        UpdateGravity();

        if (distanceToTarget < 20) AddToDetectedEnemies();
        else RemoveDetectedEnemies();

        if (!canMove) return;
        if (isParalyzed) return;
        if (isStunned) return;
        if (isFrozen) return;
        //if (isAttacking) return;

        CheckTarget();

        switch (movementType)
        {
            case EnemyMovementType.chaseAndAttack: CombatMovementChase(); break;
            case EnemyMovementType.roamAndAttack: CombatMovementRoam(); break;
            case EnemyMovementType.stayInPlace: CombatStayInPlace(); break;
            case EnemyMovementType.custom: CombatMovementCustom(); break;
            default: break;
        }
    }

    void OverrideHit()
    {
        CombatRoamGetHit();
        CombatChaseGetHit();
    }

    //==== Type : Chasing ====//

    protected void CombatMovementChase()
    {
        if (chase)
        {
            if (!isAttacking) LookAtTarget();

            if (TargetInAttackRange())
            {
                StopMove();
                CommenceAttack();
            }
            else
            {
                MoveForward();
            }
        }
        else StopMove();
    }
    void CombatChaseGetHit()
    {
        if (movementType != EnemyMovementType.chaseAndAttack) return;

        if (!health.IsSuperArmor())
        {
            isAttacking = false;
            EnableMove();
        }
    }

    //==== Type : Roaming ====//
    float combatRoamDistance, combatRoamIdleTime, combatRoamTime;
    bool roamingToDestination;
    Vector3 combatRoamPos;
    void CombatMovementRoam()
    {
        if (chase)
        {
            if (!TargetInAttackRange())
            {
                roamingToDestination = false;
                CombatMovementChase();
            }
            else
            {
                if (roamingToDestination)
                {
                    //walk to destination
                    combatRoamDistance = Vector3.Distance(transform.position, combatRoamPos);
                    LookAtTarget(combatRoamPos);

                    if (combatRoamDistance > 2)
                    {
                        MoveForward();

                        if (HittingWall())
                        {
                            CombatRoamDestinationReached();
                        }
                    }
                    else CombatRoamDestinationReached();

                    combatRoamTime += Time.deltaTime;
                    if (combatRoamTime >= 4)
                    {
                        combatRoamTime = 0;
                        CombatRoamDestinationReached();
                    }
                }
                else
                {
                    if (canAttack) CommenceAttack();

                    UpdateCombatRoamWait();
                }
            }
        }
        else StopMove();
    }
    void SetCombatRoamDestination()
    {
        if (canAttack) return;
        if (isAttacking) return;

        EnableMove();

        roamingToDestination = true;
        bool correctDestination;
        int numberOfTries = 0;
        combatRoamPos = transform.position;

        do
        {
            correctDestination = true;

            //get random position around current position
            float randomX = transform.position.x - UnityEngine.Random.Range(-1f, 1f) * status.roamDistance;
            float randomZ = transform.position.z - UnityEngine.Random.Range(-1f, 1f) * status.roamDistance;
            combatRoamPos = new Vector3(randomX, transform.position.y, randomZ);

            //make sure the destination is not too close
            if (Vector3.Distance(transform.position, combatRoamPos) < status.roamDistance * 0.5f)
                correctDestination = false;

            //make sure it's not hitting a wall or other enemy
            Vector3 direction = combatRoamPos - transform.position;
            if (Physics.Raycast(transform.position, direction, status.roamDistance, 1 << 14 | 1 << 10))
                correctDestination = false;

            //get out of infinite loop when fails
            numberOfTries++;
            if (numberOfTries > 10)
            {
                combatRoamPos = transform.position;
                correctDestination = true;
            }

        } while (!correctDestination);
    }
    protected virtual void CombatRoamDestinationReached()
    {
        roamingToDestination = false;
        StopMove();
        DisableMove();

        if (canAttack) CommenceAttack();
        else SetCombatRoamDestination();
    }
    void UpdateCombatRoamWait()
    {
        if (canAttack) return;

        combatRoamIdleTime += Time.deltaTime;
        if (combatRoamIdleTime >= 1.5f)
        {
            combatRoamIdleTime = 0;
            SetCombatRoamDestination();
        }
    }
    protected virtual void CombatRoamGetHit()
    {
        if (roamingToDestination) return;
        if (movementType != EnemyMovementType.roamAndAttack) return;
        if (health.IsSuperArmor()) return;
        if (isStunned) return;
        if (isParalyzed) return;


        isAttacking = false;
        CombatRoamDestinationReached();
    }

    //==== Type : Stay In Place ====//
    protected virtual void CombatStayInPlace()
    {
        if (canAttack) CommenceAttack();
    }

    //==== Type : Custom ====//
    protected virtual void CombatMovementCustom()
    {

    }

    //==== Taunted ====//
    bool taunted;
    public Action onTaunted;
    float tauntedTime, randomTauntTime;
    public virtual void Taunted(bool decoyTaunt)
    {
        try
        {
            if (!status) status = GetComponent<EnemyStatus>();

            if (!status.IsAlive) return;

            if (!decoyTaunt)
            {
                taunted = engageCombat = true;
                canAttack = false;

                onTaunted?.Invoke();

                if (instantAttackOnAggro) randomTauntTime = 0.2f;
                else
                {
                    if (delayAttackOnAggro > 0) randomTauntTime = delayAttackOnAggro;
                    else randomTauntTime = UnityEngine.Random.Range(1f, 2f);
                }

                if (status)
                {
                    if (status.useBossHpBar)
                        status.hpBarManager.EngageBoss(status.enemyRef.Name(), status.level, health.ArmorPercent());

                    if (status.useThemeSong)
                        status.PlayThemeSong();

                }
                Timing.KillCoroutines(gameObject);

            }
            GameManager.instance.vfxManager.EnemyAggro(transform, stunFxOffset, 3);
        }
        catch { Debug.Log("Error on taunted"); }
    }
    void UpdateTauntedTime()
    {
        if (!taunted) return;
        tauntedTime += Time.deltaTime;
        if (tauntedTime >= randomTauntTime)
        {
            canAttack = true;
            attackDelayTimer = 0;
            taunted = false;
            tauntedTime = 0;
            EnableMove();

            OverrideHit();
        }
    }

    //==== Attack ====//
    protected bool canAttack = true, isAttacking;
    protected float attackDelayTimer;
    protected void UpdateAttackCooldown()
    {
        if (canAttack) return;
        if (isFrozen) return;

        attackDelayTimer += Time.deltaTime * FinalAnimationSpeed;
        if (attackDelayTimer >= status.attackDelay)
        {
            attackDelayTimer = 0;
            canAttack = true;
        }
    }


    //==== Disarm Checker ====//
    private float disarmTimer;
    protected void DisarmChecker()
    {
        //checks if enemy is stuck and doesn't respond to anything
        //if canattack / isattacking are both true at the same time, starts a 5s countdown
        //if countdown reaches 5s, enemy state will be reset
        if (canAttack && isAttacking)
        {
            disarmTimer += Time.deltaTime;
            if (disarmTimer >= 5)
            {
                disarmTimer = 0;
                canAttack = false;
                isAttacking = false;
                EnableMove();
            }
        }
        else disarmTimer = 0;
    }


    protected virtual void CommenceAttack()
    {

    }

    //-- weapon hitbox --//
    public virtual void HitHero(IDamageable damageable, Transform target)
    {

    }
    public virtual void HitObstacle()
    {

    }

    //-- hitbox trigger --//
    public virtual void TriggerHitbox(Transform hero)
    {

    }

    //==== Blackhole Mechanism ====//
    protected Transform pull_center;
    float pullDistance, releaseDistance, pullSpeed;

    public virtual void PullEffect(Transform source, float distance, float maxDistance)
    {
        PullEffect(source, distance, maxDistance, 40);
    }
    protected bool isPulled;
    public virtual void PullEffect(Transform source, float distance, float maxDistance, float pullSpd)
    {
        if (status.pullRes)
        {
            isPulled = false;
            pull_center = null;
            return;
        }

        if (source == null)
        {
            isPulled = false;
            pull_center = null;
            return;
        }

        isPulled = true;
        pull_center = source;
        pullDistance = distance;
        releaseDistance = maxDistance;
        pullSpeed = pullSpd;
    }
    protected void UpdateBlackhole()
    {
        if (banished) return;

        Vector3 blackholePos = new Vector3(pull_center.position.x, transform.position.y, pull_center.position.z);
        Vector3 offset = blackholePos - transform.position;

        distanceToTarget = Vector3.Distance(transform.position, blackholePos);
        if (distanceToTarget > releaseDistance)
            isPulled = false;

        if (distanceToTarget > pullDistance)
        {
            offset = offset.normalized * pullSpeed;
            controller.Move(offset * Time.deltaTime);
        }
    }

    public void HeroDetect()
    {
        if (aggroType == EnemyThreatBehavior.aggressive)
            GainAggro();
    }
    public void GainAggro()
    {
        if (!status) status = GetComponent<EnemyStatus>();
        if (status.isDead) return;
        if (engageCombat) return;
        if (taunted) return;
        Init();

        //Set Aggro to hero
        if (TARGET == null)
        {
            if (gm.isEscorting) mainTarget = gm.escortedNPC;
            else mainTarget = gm.ActiveHero.transform;
            isAggroed = true;
            gm.AddToAggroedEnemies();
        }
        Taunted(false);

        //Aggro linked friends
        if (links) links.AggroLinked();
    }
    protected Transform decoy;
    public void SetDecoy(Transform newTarget)
    {
        if (decoy != null) return;

        if (!status) status = GetComponent<EnemyStatus>();
        if (status.isDead) return;

        Init();

        decoy = newTarget;
        isAggroed = true;

        Taunted(true);
    }

    public void RegainAggro()
    {
        if (status.isDead) return;
        if (TARGET != null)
        {
            if (gm.isEscorting) mainTarget = gm.escortedNPC;
            else mainTarget = gm.ActiveHero.transform;
        }
    }
    public virtual void Hit()
    {
        if (isParalyzed) return;
        if (isStunned) return;
        if (isFrozen) return;

        GainAggro();
        OverrideHit();

        if (health.IsSuperArmor()) return;

        StopMove();
        //delay attack
        if (canAttack)
        {
            canAttack = false;
            attackDelayTimer = status.attackDelay - 0.5f;
        }
    }
    public virtual void Die()
    {
        DisableMove();

        EndLouisaFocus();
        Endstun();
        EndFreeze();
        EndParalyzeStun();

        EndChase();
        RemoveDetectedEnemies();
    }
    protected void DelayMove(float delay)
    {
        DelayMove(delay, true);
    }

    protected void DelayMove(float delay, bool cancel)
    {
        DelayMove(delay, cancel, true);
    }

    CoroutineHandle moveHandler;
    protected void DelayMove(float delay, bool cancel, bool stateDependent)
    {
        if (cancel)
        {
            if (stateDependent) moveHandler = Timing.RunCoroutine(_DelayMove(delay).CancelWith(gameObject), EC2Constant.STATE_DEPENDENT);
            else moveHandler = Timing.RunCoroutine(_DelayMove(delay).CancelWith(gameObject));
        }
        else
        {
            if (stateDependent) moveHandler = Timing.RunCoroutine(_DelayMove(delay), EC2Constant.STATE_DEPENDENT);
            else moveHandler = Timing.RunCoroutine(_DelayMove(delay));
        }
    }

    protected IEnumerator<float> _DelayMove(float delay)
    {
        yield return Timing.WaitForSeconds(delay);
        EnableMove();
    }

    public void IframeStart()
    {
        gameObject.layer = 15;
    }
    public void IframeEnd()
    {
        gameObject.layer = 10;
    }

    //==== Movement Debuff ====//
    protected void UpdateDebuff()
    {
        UpdateStun();
        UpdateParalyzeStun();
        Update_TribeBuff();
    }

    //==== Paralyze Stun ====//

    protected bool isParalyzed;
    float paralyzeDuration;
    [FoldoutGroup("General")] public bool useParalyzeAnimation;
    public void ParalyzeStun(float duration)
    {
        if (status.paralyzeRes) return;
        if (health.temporaryUnbreakableShield) return;
        if (health.armor > 0)
        {
            health.DamageArmor(7, true);
            return;
        }

        GameManager.instance.vfxManager.Stun(transform, stunFxOffset, duration);
        if (isStunned)
        {
            if (duration > stunDuration) stunDuration = duration;
            return;
        }

        if (health.shieldBreak) return;

        paralyzeDuration = duration;
        isParalyzed = true;
        if (useParalyzeAnimation) anim.SetBool("paralyze", true);
        else anim.SetBool("stun", true);
        Paralyzed();
        DisableMove();
    }

    void UpdateParalyzeStun()
    {
        if (!isParalyzed) return;
        if (isFrozen) return;

        paralyzeDuration -= Time.deltaTime;
        if (paralyzeDuration <= 0) EndParalyzeStun();
    }
    public void EndParalyzeStun()
    {
        isParalyzed = false;
        if (useParalyzeAnimation) anim.SetBool("paralyze", false);
        else anim.SetBool("stun", false);
        EnableMove();
        //attackDelayTimer = 0;
    }

    //==== Stun ====//
    protected bool isStunned;
    float stunDuration;
    private GameObject stunObj;
    public void Stun(float duration)
    {
        if (health.IsSuperArmor()) return;
        if (health.ShieldCount > 0) return;

        if (duration > stunDuration) stunDuration = duration;
        isStunned = true;
        anim.SetBool("stun", true);

        DisableMove();
        OnStunned();

        stunObj = GameManager.instance.vfxManager.Stun(transform, stunFxOffset, duration);
    }
    void UpdateStun()
    {
        if (!isStunned) return;
        if (isFrozen) return;

        stunDuration -= Time.deltaTime;
        if (stunDuration <= 0) Endstun();
    }
    public void Endstun()
    {
        isStunned = false;
        anim.SetBool("stun", false);
        if (!waitRiseAnimation) EnableMove();
        else attackDelayTimer = 0;

        if (stunObj) Destroy(stunObj);

        OnStunEnded();
    }

    protected virtual void OnStunned()
    {

    }
    protected virtual void OnStunEnded()
    {

    }

    public void ResetMoveSpeed()
    {
        anim.speed = this.FinalAnimationSpeed;
    }

    //==== Freeze ====//
    protected bool isFrozen;
    public virtual void Freeze()
    {
        isFrozen = true;
        ResetMoveSpeed();
        DisableMove();

        StatusEffectManager.instance.Freeze(transform);
    }
    public virtual void EndFreeze()
    {
        isFrozen = false;
        ResetMoveSpeed();
        if (!isStunned) EnableMove();

        StatusEffectManager.instance.Unfreeze(transform);
    }

    public bool GetFrozenState() => isFrozen;

    //=== POISONED ===//
    protected bool isPoisoned;
    public bool GetPoisonedState() => isPoisoned;

    public void ApplyPoison(float slowModifier)
    {
        isPoisoned = true;
        poisonSlow = slowModifier;
        ResetMoveSpeed();
    }

    public void RemovePoison()
    {
        isPoisoned = false;
        poisonSlow = 0f;
        ResetMoveSpeed();
    }

    //==== Armor Debuff ====//
    [HideInInspector]
    public bool isArmorDebuffed;
    public void DebuffArmor(float value) // not percentage
    {
        isArmorDebuffed = true;
        health.armorDebuff = value;
    }

    public void EndDebuffArmor()
    {
        isArmorDebuffed = false;
        health.armorDebuff = 0;
    }

    //==== Evasion Debuff / Louisa Focus ====//
    [HideInInspector]
    public bool isEvasionDebuffed;
    public void OnLouisaFocus(float value) // not percentage
    {
        isEvasionDebuffed = true;
        status.evasionMod = value;
    }

    public void EndLouisaFocus()
    {
        isEvasionDebuffed = false;
        status.evasionMod = 0;

        if (hero_louisa)
            hero_louisa.RemoveFocus(transform);
    }

    private Hero_Louisa hero_louisa;
    private int louisaFocusStack = 0;
    public void LouisaFocusBuildUp(int val, float duration, float potency, Hero_Louisa louisa)
    {
        hero_louisa = louisa;

        louisaFocusStack += val;
        if (louisaFocusStack >= 100)
        {
            StatusEffectManager.instance.SetStatusEffect(transform, StatusEffects.focused, duration, potency);
            louisaFocusStack = 0;

            hero_louisa.AddFocus(transform);
        }
    }

    //==== Curse Debuff ====//
    [HideInInspector]
    public bool isCursed;
    public void DebuffCurse(float slow, float armorModifier) // armor modifier not percentage
    {
        isCursed = true;
        cursedSlow = slow;
        health.curse_armorDebuff = armorModifier;
        ResetMoveSpeed();
    }

    public virtual void ProjectileFieldSpawned(Transform field)
    {

    }
    public void EndDebuffCurse()
    {
        isCursed = false;
        cursedSlow = 0f;
        health.curse_armorDebuff = 0f;
        ResetMoveSpeed();
    }

    //==== Tremor Debuff ====//
    [HideInInspector]
    public bool isTremor;
    public void DebuffTremor(float slow, float valueDamageDebuff) // 0~1 | 0~1
    {
        isTremor = true;
        tremorSlow = slow;
        status.damageDebuff_tremor = valueDamageDebuff;
        ResetMoveSpeed();
    }
    public void EndDebuffTremor()
    {
        isTremor = false;
        tremorSlow = 0f;
        status.damageDebuff_tremor = 0f;
        ResetMoveSpeed();
    }

    //==== Damage Debuff ====//
    public void BuffDamage(float value)
    {
        status.damageBuff = value;
        //Debug.Log("damage buffed : " + transform.name);
    }
    public void DebuffDamage(float value) // 0~1
    {
        //Debug.Log("damage debuff = " + value + "%");
        status.damageDebuff = value / 100f;
    }
    public void EndDebuffDamage()
    {
        status.damageDebuff = 0;
    }

    //==== Bullet Rain Debuff ====//
    [HideInInspector]
    public bool isSlowed;
    public void DebuffSlow(float slow) // 0~1 | 0~1
    {
        isSlowed = true;
        bulletRainSlow = slow;
        ResetMoveSpeed();
    }
    public void EndDebuffSlow()
    {
        isSlowed = false;
        bulletRainSlow = 0f;
        ResetMoveSpeed();
    }

    //==== Chill Debuff ====//
    public void DebuffChill(float slow) // 0~1
    {
        chillSlow = slow;
        ResetMoveSpeed();
    }
    public void EndDebuffChill()
    {
        chillSlow = 0;
        ResetMoveSpeed();
    }

    //==== Blind Debuff ====//
    public bool isDebuffBlind = false;
    public void DebuffBlind(float duration)
    {
        Timing.RunCoroutine(DebuffBlindCo(duration).CancelWith(gameObject));
    }

    IEnumerator<float> DebuffBlindCo(float duration)
    {
        isDebuffBlind = true;
        yield return Timing.WaitForSeconds(duration);
        isDebuffBlind = false;
    }

    // === CRIT // 
    public void DebuffCritRes(float value) // 0~1
    {
        status.critResDebuff = value;
    }
    public void EndDebuffCritRes()
    {
        status.critResDebuff = 0;
    }


    // === MYSTIC FLAME === //
    [HideInInspector] public bool isBurning;
    public static System.Action OnStartBurnt;
    public static System.Action OnEndedBurnt;
    public void Ignite()
    {
        if (!isBurning) OnStartBurnt?.Invoke();
        isBurning = true;
    }

    public void EndIgnite()
    {
        if (isBurning) OnEndedBurnt?.Invoke();
        isBurning = false;
    }


    // === BOGU TRIBE BUFF === //
    void Update_TribeBuff()
    {
        UpdateTribeBuff_Defense();
        UpdateTribeBuff_Attack();
    }
    protected float tribebuff_def_duration;
    public void TribeBuff_Defense(float potency, float duration)
    {
        health.StartSuperArmor();
        tribebuff_def_duration = duration;
        health.finalDmgReduction_tribe = potency;
    }
    void TribeBuff_Defense_End()
    {
        health.EndSuperArmor();
        health.finalDmgReduction_tribe = 0;
    }
    void UpdateTribeBuff_Defense()
    {
        if (tribebuff_def_duration < 0) return;

        tribebuff_def_duration -= Time.deltaTime;
        if (tribebuff_def_duration <= 0)
            TribeBuff_Defense_End();
    }

    protected float tribebuff_atk_duration;
    public void TribeBuff_Attack(float potency, float duration)
    {
        status.atkTribe = potency / 100;
        tribebuff_atk_duration = duration;
    }
    void TribeBuff_Attack_End()
    {
        status.atkTribe = 0;
    }
    void UpdateTribeBuff_Attack()
    {
        if (tribebuff_atk_duration < 0) return;

        tribebuff_atk_duration -= Time.deltaTime;
        if (tribebuff_atk_duration <= 0)
            TribeBuff_Attack_End();
    }



    //==== BREAK ====//
    public void StartBreak(float duration)
    {
        isStunned = true;
        stunDuration = duration;
        anim.SetBool("stun", true);
        health.breakTime = 0f;

        DisableMove();
        GameManager.instance.vfxManager.Stun(transform, stunFxOffset, duration);

        BreakSequence();
    }
    protected virtual void BreakSequence()
    {

    }

    protected virtual void Paralyzed()
    {

    }

    protected virtual void StunEndSeq()
    {

    }
    public void EndBreak()
    {
        StunEndSeq();
        Endstun();
    }

    protected void SetAttackingFalse()
    {
        isAttacking = false;
    }

    public float GetFinalIndicatorCountdown(float baseTime)
    {
        return baseTime + (baseTime * (1f - FinalAnimationSpeed));
    }

    public void SetInvincible(bool on)
    {
        if (on) gameObject.layer = 15;
        else gameObject.layer = 10;
    }


    public void Teleport(float delay, System.Action OnTeleported)
    {
        Timing.RunCoroutine(_Teleport(delay, OnTeleported).CancelWith(gameObject), Segment.EndOfFrame);
    }
    IEnumerator<float> _Teleport(float delay, System.Action OnTeleported)
    {
        controller.enabled = false;
        yield return Timing.WaitForSeconds(delay);
        //Get player 
        Transform player = gm.ActiveHero.transform;

        //Get position behind
        Vector3 pos = player.position - player.forward * 2;

        yield return Timing.WaitForOneFrame;

        transform.position = pos;

        controller.enabled = true;

        OnTeleported?.Invoke();
    }

    //Attack Func
    public void GetVictimSphere(Vector3 center, float radius, out Collider[] victims)
    {
        victims = Physics.OverlapSphere(center, radius, 1 << 9);
    }
    public void GetVictimRect(Transform hitbox, out Collider[] victims)
    {
        BoxCollider boxCollider = hitbox.GetComponent<BoxCollider>();

        //create temporary object to represent the prefab in the scene
        GameObject temp = new GameObject("TempBoxCollider");
        temp.transform.SetParent(hitbox.transform, false);
        temp.transform.localPosition = Vector3.zero;
        temp.transform.localRotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        // Get the local position and rotation of the pivot relative to the character
        Vector3 center = temp.transform.TransformPoint(boxCollider.center);
        Vector3 halfExtent = Vector3.Scale(boxCollider.size * 0.5f, temp.transform.lossyScale);

        // Perform the OverlapBox operation
        victims = Physics.OverlapBox(center, halfExtent, temp.transform.rotation, 1 << 9);

        GameObject.Destroy(temp);
    }
}