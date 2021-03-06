﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EntityPlayer : EntityLivingBase
{
    public enum State
    {
        IDLE,
        MOVING,
        DASHING,
        ATTACK,
        HEAVY_ATTACK,
        SUMMONING,
        DEAD,
        RECALL
    }

    public enum TARGET_STATE
    {
        AIMING,
        NOT_AIMING
    }

    public enum DIRECTION
    {
        FRONT = 1,
        BACK,
        RIGHT,
        LEFT
    }

    private State
        player_state;

    private DIRECTION
        player_dir;

    private TARGET_STATE
        player_target_state;

    private int
        i_combo;

    private float
        f_shooting_interval,
        f_shooting_max_interval,

        f_charged_amount,
        f_charged_max_amount,

        f_charged_increase_amount;

    private bool
        b_is_charging_shot;

    private List<GameObject>
        list_joints;

    private GameObject
        go_charging_particle,
        go_sword_particle,
        go_target;

    private CharacterMovement
        cm_player_movement;

    //private List<Transform>
    //    list_last_joint_transform;

    delegate void m_checkfunction();
    Dictionary<State, m_checkfunction> m_checkfuntions = new Dictionary<State, m_checkfunction>();

    protected override void Start()
    {
        base.Start();

        GetInventory().ReplaceInventory(SaveData.LoadInventory());

        if (m_checkfuntions == null)
            m_checkfuntions = new Dictionary<State, m_checkfunction>();

        list_joints = new List<GameObject>();
        // list_last_joint_transform = new List<Transform>();

        m_checkfuntions.Add(State.IDLE, IdleCheckFunction);
        m_checkfuntions.Add(State.MOVING, MovingCheckFunction);
        m_checkfuntions.Add(State.DASHING, DashingCheckFunction);
        m_checkfuntions.Add(State.ATTACK, AttackCheckFunction);
        m_checkfuntions.Add(State.HEAVY_ATTACK, HeavyAttackCheckFunction);
        m_checkfuntions.Add(State.SUMMONING, SummoningCheckFunction);
        m_checkfuntions.Add(State.DEAD, DeadCheckFunction);
        m_checkfuntions.Add(State.RECALL, RecallCheckFunction);

        if (GetComponent<CharacterMovement>() != null)
        {
            cm_player_movement = GetComponent<CharacterMovement>();
            go_target = null;
        }

        go_charging_particle = null;

        foreach (Transform _trans in gameObject.GetComponentsInChildren<Transform>())
        {
            if (TagHelper.IsTagJoint(_trans.gameObject.tag))
                list_joints.Add(_trans.gameObject);
        }

        if (list_joints.Count > 0)
        {
            if (list_joints[0].transform.childCount > 0)
                go_sword_particle = list_joints[0].transform.GetChild(0).gameObject;
        }
    }

    public override void HardReset()
    {
        base.HardReset();

        St_stats = new Stats();
        St_stats.F_maxspeed = St_stats.F_speed = 5;
        St_stats.F_max_health = St_stats.F_health = 100;
        St_stats.F_max_mana = St_stats.F_mana = 100;
        St_stats.F_damage = 20;
        St_stats.F_mass = 1;

        f_shooting_max_interval = f_shooting_interval = 0.1f;
        f_charged_amount = 1.0f;
        f_charged_max_amount = 2;
        f_charged_increase_amount = 2.0f;
        b_is_charging_shot = false;
        i_combo = 1;
        F_mana_regen_amount = 1;

        if (GetInventory().GetAllSkills().Count <= 0)
        {
            //TEMPO PLS REMOVE
            SkillBase _skill = new SkillFlash();
            _skill.SetUpSkill();
            GetInventory().AddSkill(_skill);

            _skill = new SkillSwordSummoning();
            _skill.SetUpSkill();
            GetInventory().AddSkill(_skill);

            _skill = new SkillBeam();
            _skill.SetUpSkill();
            GetInventory().AddSkill(_skill);
            //
        }
    }

    private void IdleCheckFunction()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            player_state = State.MOVING;
            return;
        }

        if (DoubleTapCheck.GetInstance().IsDoubleTapTriggered() &&
             (DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.W
             || DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.A
             || DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.S
             || DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.D))
        {
            player_state = State.DASHING;
            An_animator.SetBool("IsDashing", true);

            return;
        }

        if (GetPlayerTargetState() == TARGET_STATE.AIMING)
        {
            if (GetInventory().GetCurrSkill() != null)
            {
                if (Input.GetKeyDown(KeyCode.Mouse2))
                    GetInventory().GetCurrSkill().OnSkillPressed();
                if (Input.GetKeyUp(KeyCode.Mouse2))
                    GetInventory().GetCurrSkill().OnSkillReleased();

                if (Input.GetKey(KeyCode.Mouse2) && GetInventory().GetCurrSkill().IsUnderCooldown() && St_stats.F_mana > 0)
                {
                    float _manaused = St_stats.F_mana - GetInventory().GetCurrSkill().GetManaAmount();

                    GetInventory().GetCurrSkill().OnSkillBegin();

                    if (_manaused >= 0)
                    {
                        St_stats.F_mana = _manaused;
                        GetInventory().GetCurrSkill().StartSkill(this, GetInventory().GetCurrSkill().GetManaAmount());
                    }
                    else
                    {
                        St_stats.F_mana = 0;
                        GetInventory().GetCurrSkill().StartSkill(this, GetInventory().GetCurrSkill().GetManaAmount() + _manaused);
                    }

                    An_animator.SetBool("IsSummoning", true);
                    An_animator.SetInteger("SummoningID", GetInventory().GetCurrSkill().I_id);
                    player_state = State.SUMMONING;
                    return;
                }
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.Mouse2))
            {
                player_state = State.HEAVY_ATTACK;
                return;
            }
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            player_state = State.ATTACK;

            return;
        }

        if (Input.GetKeyUp(KeyCode.B))
        {
            player_state = State.RECALL;
            return;
        }

        St_stats.F_speed = St_stats.F_maxspeed;
        i_combo = 0;
        player_dir = DIRECTION.FRONT;
    }

    private void MovingCheckFunction()
    {
        if (!(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)))
        {
            player_state = State.IDLE;
        }

        if (DoubleTapCheck.GetInstance().IsDoubleTapTriggered() &&
            (DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.W
            || DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.A
            || DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.S
            || DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.D))
        {
            player_state = State.DASHING;
            An_animator.SetBool("IsDashing", true);

            return;
        }

        if (GetPlayerTargetState() == TARGET_STATE.AIMING)
        {
            if (GetInventory().GetCurrSkill() != null)
            {
                if (Input.GetKeyDown(KeyCode.Mouse2))
                    GetInventory().GetCurrSkill().OnSkillPressed();
                if (Input.GetKeyUp(KeyCode.Mouse2))
                    GetInventory().GetCurrSkill().OnSkillReleased();

                if (Input.GetKey(KeyCode.Mouse2) && GetInventory().GetCurrSkill().IsUnderCooldown() && St_stats.F_mana > 0)
                {
                    float _manaused = St_stats.F_mana - GetInventory().GetCurrSkill().GetManaAmount();

                    GetInventory().GetCurrSkill().OnSkillBegin();

                    if (_manaused >= 0)
                    {
                        St_stats.F_mana = _manaused;
                        GetInventory().GetCurrSkill().StartSkill(this, GetInventory().GetCurrSkill().GetManaAmount());
                    }
                    else
                    {
                        St_stats.F_mana = 0;
                        GetInventory().GetCurrSkill().StartSkill(this, GetInventory().GetCurrSkill().GetManaAmount() + _manaused);
                    }

                    An_animator.SetInteger("SummoningID", GetInventory().GetCurrSkill().I_id);
                    An_animator.SetBool("IsSummoning", true);
                    player_state = State.SUMMONING;
                    return;
                }
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.Mouse2))
            {
                player_state = State.HEAVY_ATTACK;
                return;
            }
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            player_state = State.ATTACK;

            return;
        }

        i_combo = 0;
    }

    private void DashingCheckFunction()
    {
        if (player_target_state != TARGET_STATE.AIMING)
            St_stats.F_speed = St_stats.F_maxspeed * 2;

        if (!An_animator.GetBool("IsDashing"))
            player_state = State.MOVING;
    }

    private void AttackCheckFunction()
    {
        St_stats.F_speed = St_stats.F_maxspeed;

        if (DoubleTapCheck.GetInstance().IsDoubleTapTriggered() &&
             (DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.W
             || DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.A
             || DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.S
             || DoubleTapCheck.GetInstance().GetDoubleTapKey() == KeyCode.D))
        {
            player_state = State.DASHING;
            An_animator.SetBool("IsDashing", true);
            EndAttackAnimation();

            return;
        }

        if (An_animator.GetBool("IsAttacking"))
        {
            if (player_target_state == TARGET_STATE.AIMING && !An_animator.GetBool("IsMelee"))
            {
                if (DoubleTapCheck.GetInstance().IsDoubleClickTriggered() && DoubleTapCheck.GetInstance().GetDoubleTapMouseKey() == KeyCode.Mouse0 && !b_is_charging_shot)
                {
                    b_is_charging_shot = true;
                }

                if (b_is_charging_shot)
                {
                    if (f_charged_amount < f_charged_max_amount)
                    {
                        if (go_charging_particle == null)
                            go_charging_particle = ParticleHandler.GetInstance().SpawnParticle(ParticleHandler.ParticleType.Charging, list_joints[0].transform, new Vector3(0, 0, 0), Vector3.one, Vector3.zero, 0);

                        f_charged_amount += f_charged_increase_amount * Time.unscaledDeltaTime;
                    }
                    else if (f_charged_amount >= f_charged_max_amount)
                    {
                        if (go_charging_particle != null)
                        {
                            Destroy(go_charging_particle);
                            go_charging_particle = null;
                        }

                        f_charged_amount = f_charged_max_amount;
                    }

                    if (Input.GetKeyUp(KeyCode.Mouse0))
                    {
                        Vector3 target = Camera.main.ScreenToWorldPoint(Input.mousePosition) + Camera.main.transform.forward * 35;

                        StraightBullet sb = ObjectPool.GetInstance().GetProjectileObjectFromPool(ObjectPool.PROJECTILE.HEART_PROJECTILE).GetComponent<StraightBullet>();
                        sb.SetUpProjectile(gameObject, list_joints[0].transform.position, target - gameObject.transform.position, 5, St_stats.F_damage * f_charged_amount, 40, new Vector3(f_charged_amount * 0.25f, f_charged_amount * 0.25f, f_charged_amount * 0.25f));
                        ParticleHandler.GetInstance().SpawnParticle(ParticleHandler.ParticleType.Heart_Burst, list_joints[0].transform, new Vector3(0, 0.5f, 0), Vector3.one, Vector3.zero, 1.0f);

                        if (go_charging_particle != null)
                        {
                            Destroy(go_charging_particle);
                            go_charging_particle = null;
                        }

                        if (b_is_charging_shot)
                            b_is_charging_shot = false;

                        if (f_charged_amount != 1)
                            f_charged_amount = 1;

                        f_shooting_interval = 0;

                        An_animator.SetBool("IsAttacking", false);
                    }
                }
                else
                {

                    if (f_shooting_interval >= f_shooting_max_interval)
                    {
                        Vector3 target = Camera.main.ScreenToWorldPoint(Input.mousePosition) + Camera.main.transform.forward * 35;

                        StraightBullet sb = ObjectPool.GetInstance().GetProjectileObjectFromPool(ObjectPool.PROJECTILE.HEART_PROJECTILE).GetComponent<StraightBullet>();
                        sb.SetUpProjectile(gameObject, list_joints[0].transform.position, target - gameObject.transform.position, 5, St_stats.F_damage * f_charged_amount, 10, new Vector3(f_charged_amount * 0.25f, f_charged_amount * 0.25f, f_charged_amount * 0.25f));

                        ParticleHandler.GetInstance().SpawnParticle(ParticleHandler.ParticleType.Heart_Burst, list_joints[0].transform, new Vector3(0, 0.5f, 0), Vector3.one, Vector3.zero, 1.0f);

                        ap_audioPlayer.PlayClip("PlayerFireBullet", Random.Range(0.6f, 1.0f));

                        f_shooting_interval = 0;
                    }
                }
            }
            else
            {

                go_sword_particle.SetActive(true);
                if (b_is_charging_shot)
                    b_is_charging_shot = false;

                if (f_charged_amount != 1)
                    f_charged_amount = 1;

                An_animator.SetBool("IsMelee", true);

                if (cm_player_movement != null)
                {
                    float
                        _distance_near = 1,
                        _distance_away = 10,
                        _angle = 25;

                    if (go_target != null)
                    {
                        if (!go_target.activeSelf || Vector3.Angle(transform.forward, (go_target.transform.position - GetPosition()).normalized) > _angle)
                            go_target = null;
                    }

                    if (go_target == null)
                    {
                        foreach (GameObject go in ObjectPool.GetInstance().GetActiveEntityObjects())
                        {
                            if (Vector3.Angle(transform.forward, (go.transform.position - GetPosition()).normalized) < _angle && !go.GetComponent<EntityLivingBase>().IsDead())
                            {
                                if (go.CompareTag("Enemy") && go_target == null && Vector3.Distance(go.transform.position, GetPosition()) > _distance_near && Vector3.Distance(go.transform.position, GetPosition()) < _distance_away)
                                {
                                    go_target = go;
                                }
                                else if (go_target != null && go.CompareTag("Enemy") && Vector3.Distance(go_target.transform.position, GetPosition()) > Vector3.Distance(go.transform.position, GetPosition()))
                                {
                                    if (Vector3.Distance(go.transform.position, GetPosition()) > _distance_near && Vector3.Distance(go.transform.position, GetPosition()) < _distance_away)
                                        go_target = go;
                                }
                            }
                        }
                    }

                    if (go_target != null)
                    {
                        if (Vector3.Distance(go_target.transform.position, GetPosition()) > _distance_near && i_combo != 1)
                        {
                            cm_player_movement.Dash((new Vector3(go_target.transform.position.x, GetPosition().y, go_target.transform.position.z) - GetPosition()).normalized, Vector3.Distance(go_target.transform.position, GetPosition()) * 8000 * Time.fixedUnscaledDeltaTime);
                            i_combo = 2;
                        }
                        else
                        {
                            cm_player_movement.Dash((new Vector3(go_target.transform.position.x, GetPosition().y, go_target.transform.position.z) - GetPosition()).normalized, 0.1f * Time.fixedUnscaledDeltaTime);
                            i_combo = 1;
                        }
                    }
                    else
                    {
                        i_combo = 1;
                    }
                }
            }
        }

        if (!b_is_charging_shot && !An_animator.GetBool("IsAttacking"))
        {
            player_state = State.IDLE;
        }
    }

    private void HeavyAttackCheckFunction()
    {
        St_stats.F_speed = St_stats.F_maxspeed;

        if (An_animator.GetBool("IsAttacking"))
        {
            if (b_is_charging_shot)
                b_is_charging_shot = false;

            if (f_charged_amount != 1)
                f_charged_amount = 1;

            i_combo = 3;

            An_animator.SetBool("IsMelee", true);
        }

        if (!An_animator.GetBool("IsAttacking"))
        {
            player_state = State.IDLE;
        }
    }

    private void SummoningCheckFunction()
    {
        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            GetInventory().GetCurrSkill().OnSkillPressed();

            if (GetInventory().GetCurrSkill().S_Name == "Light Beam")
            { // only cancel light beam lol
                EndSummoningAnimation();
                GetInventory().GetCurrSkill().EndSkill();
                player_state = State.IDLE;
            }
        }

        if (Input.GetKeyUp(KeyCode.Mouse2))
            GetInventory().GetCurrSkill().OnSkillReleased();

        if (!An_animator.GetBool("IsSummoning"))
        {
            GetInventory().GetCurrSkill().EndSkill();
            player_state = State.IDLE;
        }

        GetInventory().GetCurrSkill().RunSkill();
        Debug.Log("Summoning");

    }

    private void DeadCheckFunction()
    {
        if (!IsDead())
            player_state = State.IDLE;
    }

    private void RecallCheckFunction()
    {

    }

    public State GetPlayerState()
    {
        return player_state;
    }

    public TARGET_STATE GetPlayerTargetState()
    {
        return player_target_state;
    }

    public DIRECTION GetPlayerDir()
    {
        return player_dir;
    }

    public void SetPlayerState(State _player_new_state)
    {
        player_state = _player_new_state;
    }

    protected override void Update()
    {
        if (CameraHandler.GetInstance().GetCameraType() != CameraHandler.CameraType.ThirdPerson)
            return;

        base.Update();

        //if (GetInventory().GetInventoryContainer().Count > 0)
        //{
        //    foreach (var dic in GetInventory().GetInventoryContainer())
        //    {
        //        Debug.Log(dic.Key.ToString() + " x" + dic.Value.ToString());
        //    }
        //}

        foreach (SkillBase sb in GetInventory().GetAllSkills())
        {
            sb.UpdateSkill();
        }

        m_checkfuntions[player_state]();

        if (player_state == State.DASHING)
            B_isDodging = true;
        else
            B_isDodging = false;

        if (IsDead())
        {
            if (player_state == State.SUMMONING)
            {
                EndSummoningAnimation();
                GetInventory().GetCurrSkill().EndSkill();
            }

            player_state = State.DEAD;
            player_target_state = TARGET_STATE.NOT_AIMING;
        }

        if (!IsDead())
        {
            //Debug.Log("Health: " + St_stats.F_health + " / " + St_stats.F_max_health);
            //Debug.Log("Mana: " + St_stats.F_mana + " / " + St_stats.F_max_mana);

            if (Input.GetKeyDown(KeyCode.Alpha1) && St_stats.F_health < St_stats.F_max_health)
            {
                if (GetInventory().UseItemInInventory(this, Item.ITEM_TYPE.HEALTH_POTION))
                    ParticleHandler.GetInstance().SpawnParticle(ParticleHandler.ParticleType.Heal, transform, new Vector3(0, 2, 0), Vector3.zero, Vector3.zero, 1.8f);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) && St_stats.F_mana < St_stats.F_max_mana)
            {
                if (GetInventory().UseItemInInventory(this, Item.ITEM_TYPE.MANA_POTION))
                    ParticleHandler.GetInstance().SpawnParticle(ParticleHandler.ParticleType.Mana, transform, new Vector3(0, 2, 0), Vector3.zero, Vector3.zero, 1.8f);

            }

            if (player_state != State.SUMMONING)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    GetInventory().GetNextSkill(false);
                }
                else if (Input.GetKeyDown(KeyCode.E))
                {
                    GetInventory().GetNextSkill(true);
                }
            }

            if (Input.GetKey(KeyCode.Mouse1))
            {
                //if(list_last_joint_transform.Count == 0)
                //{
                //    foreach(GameObject go in list_joints)
                //    {
                //        list_last_joint_transform.Add(go.transform);
                //    }
                //}

                player_target_state = TARGET_STATE.AIMING;
                if (St_stats.F_speed != St_stats.F_maxspeed)
                    St_stats.F_speed = St_stats.F_maxspeed;

                if (!b_is_charging_shot)
                {
                    if (Input.GetKey(KeyCode.Mouse0))
                        An_animator.SetBool("IsShooting", true);
                    else
                        An_animator.SetBool("IsShooting", false);
                }
                else
                {
                    if (Input.GetKeyUp(KeyCode.Mouse0))
                    {
                        An_animator.SetBool("IsShooting", true);
                    }
                    else
                    {
                        An_animator.SetBool("IsShooting", false);
                    }
                }

            }
            else
            {
                player_target_state = TARGET_STATE.NOT_AIMING;
                An_animator.SetBool("IsShooting", false);
            }

            if (f_shooting_interval < f_shooting_max_interval)
                f_shooting_interval += Time.unscaledDeltaTime;
        }

        switch (player_target_state)
        {
            case TARGET_STATE.AIMING:
                An_animator.SetBool("IsAiming", true);

                if (Input.GetKey(KeyCode.W))
                {
                    player_dir = DIRECTION.FRONT;
                    An_animator.SetBool("IsMoving", true);
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    player_dir = DIRECTION.BACK;
                    An_animator.SetBool("IsMoving", true);
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    player_dir = DIRECTION.LEFT;
                    An_animator.SetBool("IsMoving", true);
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    player_dir = DIRECTION.RIGHT;
                    An_animator.SetBool("IsMoving", true);
                }
                else
                {
                    An_animator.SetBool("IsMoving", false);
                }

                An_animator.SetInteger("Direction", (int)player_dir);
                break;
            case TARGET_STATE.NOT_AIMING:
                An_animator.SetBool("IsAiming", false);
                break;

            default:
                An_animator.SetBool("IsAiming", false);
                break;
        }

        switch (player_state)
        {
            case State.ATTACK:
                An_animator.SetBool("IsDead", false);
                An_animator.SetBool("IsAttacking", true);
                break;

            case State.HEAVY_ATTACK:
                An_animator.SetBool("IsDead", false);
                An_animator.SetBool("IsAttacking", true);
                go_sword_particle.SetActive(true);
                break;

            case State.DASHING:

                if (Input.GetKeyDown(KeyCode.Mouse2))
                    GetInventory().GetCurrSkill().OnSkillPressed();
                if (Input.GetKeyUp(KeyCode.Mouse2))
                    GetInventory().GetCurrSkill().OnSkillReleased();

                An_animator.SetBool("IsDead", false);
                go_sword_particle.SetActive(false);
                break;

            case State.DEAD:
                An_animator.SetBool("IsMoving", false);
                An_animator.SetBool("IsDead", true);
                An_animator.SetBool("IsAttacking", false);
                An_animator.SetBool("IsMelee", false);
                An_animator.SetBool("IsSummoning", false);
                go_sword_particle.SetActive(false);
                break;

            case State.IDLE:
                An_animator.SetBool("IsDead", false);
                if (player_target_state == TARGET_STATE.NOT_AIMING)
                    An_animator.SetBool("IsMoving", false);
                go_sword_particle.SetActive(false);
                break;

            case State.MOVING:
                if (player_target_state == TARGET_STATE.NOT_AIMING)
                    An_animator.SetBool("IsMoving", true);
                An_animator.SetBool("IsDead", false);
                go_sword_particle.SetActive(false);
                break;

            case State.SUMMONING:
                An_animator.SetBool("IsDead", false);
                go_sword_particle.SetActive(false);
                break;

            case State.RECALL:
                if (An_animator.GetBool("Recall") == false)
                {
                    An_animator.SetBool("Recall", true);
                    ap_audioPlayer.PlayClip("PlayerRecall");
                    ParticleHandler.GetInstance().SpawnParticle(ParticleHandler.ParticleType.Recall, transform, Vector3.zero, Vector3.one, Vector3.zero, 0);
                    transform.DOMoveY(transform.position.y + 0.5f, 2).OnComplete(() =>
                    {
                        FindObjectOfType<UIGameplayAssistant>().Transition(UIGameplayAssistant.TransitType.Out, 1.5f, () =>
                       {
                           SaveData.SaveInventory(GetInventory().GetInventoryContainer());
                           SceneHandler.GetInstance().ChangeSceneAsync(SceneHandler.SceneType.MainMenu, null);
                       });
                    });
                }
                go_sword_particle.SetActive(false);
                break;

            default:
                An_animator.SetBool("IsMoving", false);
                An_animator.SetBool("IsDead", false);
                An_animator.SetBool("IsAttacking", false);
                go_sword_particle.SetActive(false);
                break;
        }

        An_animator.SetFloat("MoveSpeed", GetStats().F_speed / GetStats().F_maxspeed);
        An_animator.SetInteger("Combo", i_combo);

    }

    protected override void LateUpdate()
    {
        if (CameraHandler.GetInstance().GetCameraType() != CameraHandler.CameraType.ThirdPerson)
            return;

        if (player_state != State.RECALL)
            base.LateUpdate();

        // Debug.Log("Player Health: " + St_stats.F_health + "/" + St_stats.F_max_health);
        // Debug.Log(player_state);
        //if (player_target_state == TARGET_STATE.AIMING)
        //{
        //    foreach (GameObject go in list_joints)
        //    {
        //        go.transform.LookAt(Camera.main.ScreenToWorldPoint(Input.mousePosition) + Camera.main.transform.forward * 100);
        //        go.transform.localEulerAngles = new Vector3(go.transform.localRotation.x + 45,
        //             go.transform.localRotation.y,
        //             go.transform.localRotation.z + 45);
        //    }
        //}

        if (Input.GetKeyUp(KeyCode.V))
        {
            DamageSource src = new DamageSource();
            src.SetUpDamageSource("God", "God", "God", 9999);

            OnAttacked(src);
        }

    }

    public override void OnAttack()
    {
        Vector3 _attack_hitbox;
        float _multiplier;

        switch (i_combo)
        {
            case 1:
                _multiplier = 1;
                _attack_hitbox = new Vector3(3, 1, 2);
                ap_audioPlayer.PlayClip("PlayerMeleeSmallSwing_" + Random.Range(1, 4));
                break;
            case 2:
                _multiplier = 1.5f;
                _attack_hitbox = new Vector3(2, 1, 3);
                ap_audioPlayer.PlayClip("PlayerMeleeMediumSwing_" + Random.Range(1, 4));
                break;

            case 3:
                _multiplier = 2;
                _attack_hitbox = new Vector3(3, 1, 3);
                ap_audioPlayer.PlayClip("PlayerMeleeBigSwing_1");
                break;

            default:
                _multiplier = 1;
                _attack_hitbox = Vector3.one;
                break;
        }

        SetUpHitBox(gameObject.name, gameObject.tag, "Melee", St_stats.F_damage * _multiplier, _attack_hitbox, transform.position + (transform.forward * GetComponent<Collider>().bounds.extents.magnitude * 1.5f), transform.rotation, 0.1f);

    }

    public override void OnAttacked(DamageSource _dmgsrc, float _timer = 0.5f)
    {

        //Debug.Log("Player hit guy: " + _dmgsrc.GetSourceTag());

        if (!IsDead() && !B_isHit && player_state != State.DASHING)
        {
            S_last_hit = _dmgsrc.GetName();
            St_stats.F_health -= _dmgsrc.GetDamage();
            if (player_state != State.SUMMONING)
                An_animator.SetTrigger("IsHit");
            ResetOnHit(_timer);

            // If player died, respawn
            if (IsDead())
            {
                var uiAssistant = FindObjectOfType<UIGameplayAssistant>();
                uiAssistant.Transition(UIGameplayAssistant.TransitType.Out, 6, () =>
                 {
                     uiAssistant.Transition(UIGameplayAssistant.TransitType.In, 1);

                     int index = GetInventory().GetCurrSkillIndex();
                     HardReset();
                     ObjectPool.GetInstance().ResetSpawnerManager();
                     GetInventory().SetCurrSkill(index);
                     FindObjectOfType<UIGameplayAssistant>().PopulateSkillIcons();

                     GameObject spawnPoint = GameObject.FindWithTag("Respawn");

                     if (spawnPoint == null)
                         transform.position = Vector3.zero;
                     else
                         transform.position = spawnPoint.transform.position;
                 });
            }
        }
        else if (player_state == State.DASHING && !B_isHit && !TagHelper.IsTagBanned(_dmgsrc.GetSourceTag()))
        {
            ResetOnHit(_timer);
            TimeHandler.GetInstance().AffectTime(0.1f, 50);
            //Debug.Log("Slowing down");
        }
    }

    public void EndAttackAnimation()
    {
        An_animator.SetBool("IsAttacking", false);
        An_animator.SetBool("IsMelee", false);
    }

    public void EndSummoningAnimation()
    {
        An_animator.SetBool("IsSummoning", false);
    }

    public void EndDashingAnimation()
    {
        An_animator.SetBool("IsDashing", false);
    }

    public void EndShootAnimation()
    {
        An_animator.SetBool("IsAttacking", false);
        An_animator.SetBool("IsMelee", false);
    }
}
