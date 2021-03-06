﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManaPotion : Item
{
    private float
        f_heal_amount;

    public ItemManaPotion()
    {
        SetUpItem();
    }

    public override void SetUpItem()
    {
        inf_info = new Info();

        f_heal_amount = 50;

        inf_info.It_type = ITEM_TYPE.MANA_POTION;
        inf_info.S_name = "Mana Potion";
        inf_info.S_desc = "Recovers a portion of the user's mana";
    }

    public override bool OnUse(EntityLivingBase _ent)
    {
        if (_ent.St_stats.F_mana >= _ent.St_stats.F_max_mana)
            return false;

        _ent.St_stats.F_mana += f_heal_amount;
        return true;
    }

    public override void OnGround(EntityPickUps _go)
    {
        _go.DoFloating();
    }

    public override void OnTaken(EntityPickUps _go, EntityLivingBase _taker)
    {
        _taker.GetInventory().AddItemToInventory(_go.CurrentItem().GetInfo().It_type, 1);
    }

    public override void OnExpire(EntityPickUps _go)
    {

    }
}
