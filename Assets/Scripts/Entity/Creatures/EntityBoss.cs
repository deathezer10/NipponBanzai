﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EntityBoss : EntityEnemy
{

    public enum BossState
    {
        FULLHEALTH,
        HALFHEALTH,
        QUARTERHEATLH
    };

    public enum AttackState
    {
        NONE,
        GRAVITY,
        LASER,
        ARTY,
        SPINATTACK,
        H_MEELE,
        V_MEELE
    };

    public enum ChargeState
    {
        NONE,
        STAGE_1,
        STAGE_2,
        END
    };

    AttackState enum_currentAttState;
    ChargeState enum_currentChargeState;
    BossState enum_currentBossState;

    float
        f_dissolveRate;

    Material
        m_bossMat;

    bool
        b_stateIsDone;

    #region Getter/Setter
    public AttackState Enum_currentAttState {
        get {
            return enum_currentAttState;
        }

        set {
            enum_currentAttState = value;
        }
    }

    public ChargeState Enum_currentChargeState {
        get {
            return enum_currentChargeState;
        }

        set {
            enum_currentChargeState = value;
        }
    }

    public bool B_stateIsDone {
        get {
            return b_stateIsDone;
        }

        set {
            b_stateIsDone = value;
        }
    }
    #endregion

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        if (CameraHandler.GetInstance().GetCameraType() == CameraHandler.CameraType.ThirdPerson)
        {
            if (!IsDead())
                base.Update();
            else
            {
                F_death_timer += Time.deltaTime;
                An_animator.SetBool("Dead", true);
                f_dissolveRate += Time.deltaTime * 0.25f;
                m_bossMat.SetFloat("_DissolveAmount", f_dissolveRate);

                if (F_death_timer > 5.0f)
                {
                    gameObject.SetActive(false);

                    //GameObject go = ObjectPool.GetInstance().GetItemObjectFromPool();
                    //go.GetComponent<EntityPickUp>().SetPosition(GetPosition());

                    //TODO: Spawn Soul For Player TO Collect
                }
            }
        }
    }

    public override void OnAttack()
    {
        Vector3 attack_hitbox = new Vector3(3, 2, 8);
        Vector3 attack_dir = transform.forward;

        if (enum_currentAttState == AttackState.H_MEELE)
        {
            switch (enum_currentChargeState)
            {
                case ChargeState.NONE:
                    break;
                case ChargeState.STAGE_1:
                    attack_hitbox = new Vector3(8, 2, 12);
                    attack_dir = -transform.right;
                    break;
                case ChargeState.STAGE_2:
                    attack_hitbox = new Vector3(8, 2, 12);
                    attack_dir = transform.right;
                    break;
                case ChargeState.END:
                    attack_hitbox = new Vector3(12, 2, 8);
                    break;
                default:
                    break;
            }
        }



        SetUpHitBox(gameObject.name, gameObject.tag, gameObject.GetInstanceID().ToString(), St_stats.F_damage, attack_hitbox, transform.position + (attack_dir * GetComponent<Collider>().bounds.extents.magnitude), transform.rotation);
    }

    public override void OnAOEAttack(float _size = 1.0f)
    {
        GameObject obj = ObjectPool.GetInstance().GetHitboxObjectFromPool();
        HitboxTrigger obj_hitbox = obj.GetComponent<HitboxTrigger>();

        DamageSource dmgsrc = new DamageSource();

        dmgsrc.SetUpDamageSource(St_stats.S_name + " " + gameObject.GetInstanceID().ToString(),
            gameObject.tag,
            gameObject.GetInstanceID().ToString(),
            St_stats.F_damage);

        obj_hitbox.SetHitbox(dmgsrc, new Vector3(_size, _size, _size));

        //obj_hitbox.transform.position = transform.position + (transform.forward * (obj_hitbox.transform.localScale * 0.8f).z);
        //obj_hitbox.transform.position = new Vector3(obj_hitbox.transform.position.x, obj_hitbox.transform.position.y + 1, obj_hitbox.transform.position.z);
        obj_hitbox.transform.position = transform.position;
        obj_hitbox.transform.rotation = transform.rotation;
    }

    public override void OnAttacked(DamageSource _dmgsrc, float _timer = 0.5f)
    {
        if (!B_isHit)
        {
            S_last_hit = _dmgsrc.GetName();

            float prevHP = St_stats.F_health;

            if (TagHelper.IsTagCritSpot(_dmgsrc.GetAttackedTag()) && B_isVulnerable)
                St_stats.F_health -= _dmgsrc.GetDamage() * 2; //YEET
            else
                St_stats.F_health -= _dmgsrc.GetDamage();

            Debug.Log("BOSS HP = " + St_stats.F_health);

            ResetOnHit(_timer);

            if (prevHP > 0 && St_stats.F_health <= 0)
            {
                DialogueTrigger.DoDialogue("BossDied");
            }

            if (enum_currentBossState != BossState.HALFHEALTH && St_stats.F_health < St_stats.F_max_health * 0.5)
            {
                enum_currentBossState = BossState.HALFHEALTH;
                SetBossState();
                CinematicPathing.GetPathWithName("LookAroundBoss").DoCinematicPath(
                    () =>
                    {
                        CameraHandler.GetInstance().ChangeCamera(CameraHandler.CameraType.ThirdPerson);
                    }
                );
            }
        }
    }

    public override void HardReset()
    {
        base.HardReset();
        ClearAITask();

        St_stats.S_name = "Perstilence";
        St_stats.F_max_health = 2000.0f;
        St_stats.F_health = St_stats.F_max_health;
        St_stats.F_damage = 25.0f;
        St_stats.F_defence = 15.0f;
        St_stats.F_speed = 8.0f;
        St_stats.F_mass = 5.0f;
        St_stats.F_knockback_resistance = 5.0f;

        B_isAttacking = false;
        B_isDodging = false;
        B_isHit = false;

        b_stateIsDone = false;

        f_dissolveRate = 0f;

        var enemiesToSpawn = new List<ObjectPool.ENEMY> { ObjectPool.ENEMY.ENEMY_MINIBOSS };

        m_bossMat = gameObject.GetComponentInChildren<Renderer>().material;

        //TODO: Set Animation Values


        ////TODO: Register AI Task
        RegisterAITask(new AIBossSpinAttack(1, this, typeof(EntityPlayer), 20.0f, 10));
        //RegisterAITask(new AIArtyState(2, this, typeof(EntityPlayer), 20, 12, 15, 3));
        RegisterAITask(new AIBossLaser(3, this, typeof(EntityPlayer), 20, 13, 20));
        //RegisterAITask(new AIAOEAttack(4, this, typeof(EntityPlayer), 20, 15, 25));
        //RegisterAITask(new AISpawnMobs(0, this, typeof(EntityPlayer), 10, 20, 3.0f, enemiesToSpawn));
        RegisterAITask(new AIChase(6, this, typeof(EntityPlayer), 50.0f, 50));
        RegisterAITask(new AIBossMeleeAttack(5, this, typeof(EntityPlayer), GetComponent<NavMeshAgent>().stoppingDistance, 8, 5));
    }

    public void NextChargeState()
    {
        Enum_currentChargeState += 1;
        An_animator.SetInteger("ChargeState", (int)Enum_currentChargeState);
    }

    public void NextChargeState(ChargeState _chargeState)
    {
        Enum_currentChargeState = _chargeState;
        An_animator.SetInteger("ChargeState", (int)Enum_currentChargeState);
    }

    public void NextAttackState(AttackState _attackState)
    {
        Enum_currentAttState = _attackState;
        An_animator.SetInteger("AttackState", (int)Enum_currentAttState);
    }

    public void ResetAnimation()
    {
        NextAttackState(EntityBoss.AttackState.NONE);
        NextChargeState(EntityBoss.ChargeState.NONE);
    }

    public void SetStateDone(bool _state)
    {
        b_stateIsDone = _state;
    }

    public void SetBossState()
    {
        //Phase 2
        //ClearAITask();
        Debug.Log("PHASE 2 AH");
        //RegisterAITask(new AIBossSpinAttack(1, this, typeof(EntityPlayer), 20.0f, 10));
        RegisterAITask(new AIArtyState(2, this, typeof(EntityPlayer), 20, 12, 15, 3));
        //RegisterAITask(new AIBossLaser(3, this, typeof(EntityPlayer), 20, 13, 20));
        RegisterAITask(new AIAOEAttack(4, this, typeof(EntityPlayer), 20, 15, 25));
        //RegisterAITask(new AIChase(6, this, typeof(EntityPlayer), 50.0f, 50));
        //RegisterAITask(new AIBossMeleeAttack(5, this, typeof(EntityPlayer), GetComponent<NavMeshAgent>().stoppingDistance, 8, 5));
    }
}
