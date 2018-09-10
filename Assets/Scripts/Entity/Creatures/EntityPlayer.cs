﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPlayer : EntityLivingBase
{
    public enum State
    {
        IDLE,
        MOVING,
        DASHING,
        ATTACK,
        SUMMONING,
        DEAD
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

    delegate void m_checkfunction();
    Dictionary<State, m_checkfunction> m_checkfuntions = new Dictionary<State, m_checkfunction>();

    protected override void Start ()
    {
        base.Start();

        if (m_checkfuntions == null)
            m_checkfuntions = new Dictionary<State, m_checkfunction>();

        m_checkfuntions.Add(State.IDLE, IdleCheckFunction);
        m_checkfuntions.Add(State.MOVING, MovingCheckFunction);
        m_checkfuntions.Add(State.DASHING, DashingCheckFunction);
        m_checkfuntions.Add(State.ATTACK, AttackCheckFunction);
        m_checkfuntions.Add(State.SUMMONING, SummoningCheckFunction);
        m_checkfuntions.Add(State.DEAD, DeadCheckFunction);

        Stats temp_stats = new Stats();


        temp_stats.F_speed = 20;
        temp_stats.F_maxspeed = temp_stats.F_speed;
        temp_stats.F_health = 5;

        St_stats = temp_stats;

        f_shooting_max_interval = f_shooting_interval = 0.1f;
        f_charged_amount = 1.0f;
        f_charged_max_amount = 2;
        f_charged_increase_amount = 0.5f;
        b_is_charging_shot = false;
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

            return;
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            player_state = State.ATTACK;
         
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

            return;
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
        if(player_target_state != TARGET_STATE.AIMING)
            St_stats.F_speed = St_stats.F_maxspeed * 2;

        player_state = State.MOVING;
    }

    private void AttackCheckFunction()
    {

        if (player_target_state == TARGET_STATE.AIMING)
        {
            if(DoubleTapCheck.GetInstance().IsDoubleClickTriggered() && DoubleTapCheck.GetInstance().GetDoubleTapMouseKey() == KeyCode.Mouse0 && !b_is_charging_shot)
            {
                b_is_charging_shot = true;
            }

            if(b_is_charging_shot)
            {

                if (f_charged_amount < f_charged_max_amount)
                    f_charged_amount += f_charged_increase_amount * Time.deltaTime;
                else if (f_charged_amount > f_charged_max_amount)
                    f_charged_amount = f_charged_max_amount;

                if (Input.GetKeyUp(KeyCode.Mouse0))
                {
                    //spawn straight projectile based on size

                    if (b_is_charging_shot)
                        b_is_charging_shot = false;

                    if (f_charged_amount != 1)
                        f_charged_amount = 1;

                    f_shooting_interval = 0;
                }
            }
            else
            {
                if(f_shooting_interval >= f_shooting_max_interval)
                {
                    //spawn straight projectile
                    Debug.Log("SHOOTO");
                    f_shooting_interval = 0;
                }
            }
        }
        else
        {
            ++i_combo;

            if (i_combo > 3)
                i_combo = 1;

            if (b_is_charging_shot)
                b_is_charging_shot = false;

            if (f_charged_amount != 1)
                f_charged_amount = 1;
        }

        if (!b_is_charging_shot)
            player_state = State.IDLE;
    }

    private void SummoningCheckFunction()
    {
        player_state = State.IDLE;
    }

    private void DeadCheckFunction()
    {
        if (!IsDead())
            player_state = State.IDLE;
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

    protected override void Update ()
    {
        base.Update();

        if (Input.GetKey(KeyCode.Mouse1))
        {
            player_target_state = TARGET_STATE.AIMING;
            if(St_stats.F_speed != St_stats.F_maxspeed)
                St_stats.F_speed = St_stats.F_maxspeed;
        }
        else
        {
            player_target_state = TARGET_STATE.NOT_AIMING;
        }

        if (f_shooting_interval < f_shooting_max_interval)
            f_shooting_interval += Time.deltaTime;

        m_checkfuntions[player_state]();

        if (IsDead())
            player_state = State.DEAD;

        Debug.Log(player_state);
    }  

    public override void OnAttack()
    {

    }

    public override void OnAttacked(DamageSource _dmgsrc)
    {

    }
}
