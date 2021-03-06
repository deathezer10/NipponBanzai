﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillFlash : SkillBase
{
    public override void SetUpSkill()
    {
        s_name = "Brilliant Flash";
        s_description = "Applies panic to surrounding enemies";

        type_style = TYPE.DEFENCE;
        f_mana_amount = 10;
        i_id = 0;

        f_cooldown = 0;
        f_maxcooldown = 5;
        f_timer = 0;
    }

    public override void StartSkill(EntityLivingBase _caster, float _manaused)
    {
        go_caster = _caster;
        f_mana_amount_used = _manaused;
        f_cooldown = f_maxcooldown;
    }

    public override void UpdateSkill()
    {
        base.UpdateSkill();
    }

    public override void RunSkill()
    {
        f_timer += Time.deltaTime;

        if (f_timer > 1.5f)
        {
            go_caster.An_animator.SetBool("IsSummoning", false);

            StatusBase _effect = new StatusPanic(70 * (((f_mana_amount_used / f_mana_amount) * 100) / 100));

            foreach (GameObject go in ObjectPool.GetInstance().GetAllActiveInSurrounding(go_caster.GetPosition(), 15 * (((f_mana_amount_used / f_mana_amount) * 100) / 100), typeof(EntityEnemy)))
            {
                go.GetComponent<EntityEnemy>().Stc_Status.ApplyStatus(_effect);
                ParticleHandler.GetInstance().SpawnParticle(ParticleHandler.ParticleType.Blind, go.transform, new Vector3(0, 2, 0), Vector3.zero, Vector3.zero, 0.2f);
            }
        }
    }

    public override void EndSkill()
    {
        f_timer = 0;
        OnSkillEnd();
    }
}
