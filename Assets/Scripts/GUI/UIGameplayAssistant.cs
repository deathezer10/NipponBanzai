﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;


public class UIGameplayAssistant : MonoBehaviour
{

    public Image m_FadeTexture;
    public Image m_PlayerHealthTexture;
    public Image m_PlayerManaTexture;

    public Text m_HealthPotionAmount;
    public Text m_ManaPotionAmount;

    public GameObject m_SkillButtonPrefab;
    public GameObject m_SkillHolder;
    public List<Sprite> m_SkillIcons;

    public GameObject m_Minimap;
    public GameObject m_MinimapCamera;

    private EntityPlayer m_Player;

    float m_PrevPlayerHP;
    float m_PrevPlayerMP;

    bool m_IsInitialized = false;
    float m_SkillsIconGap; // Length of the gap between the skill icons

    public enum TransitType
    {
        In,
        Out
    }

    // Use this for initialization
    void Start()
    {
        if (!m_FadeTexture.gameObject.activeInHierarchy && m_FadeTexture.transform.parent != null)
            m_FadeTexture.transform.parent.gameObject.SetActive(true);

        Transition(TransitType.In, 1.5f);
    }

    public void Transition(TransitType transitType, float duration, System.Action onComplete = null)
    {
        if (transitType == TransitType.In)
        {
            var newColor = m_FadeTexture.color;
            newColor.a = 1;
            m_FadeTexture.color = newColor;
            m_FadeTexture.DOFade(0, duration).OnComplete(() => { if (onComplete != null) onComplete(); });
        }
        else
        {
            var newColor = m_FadeTexture.color;
            newColor.a = 0;
            m_FadeTexture.color = newColor;
            m_FadeTexture.DOFade(1, duration).OnComplete(() => { if (onComplete != null) onComplete(); });
        }
    }

    public void PopulateSkillIcons()
    {
        foreach (Transform child in m_SkillHolder.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (SkillBase skill in m_Player.GetInventory().GetAllSkills())
        {
            // skill.S_Name
            GameObject go = Instantiate(m_SkillButtonPrefab, m_SkillHolder.transform);
            go.transform.Find("Icon").GetComponent<Image>().sprite = FindSpriteWithName(skill.S_Name);
            go.name = skill.S_Name;
            go.GetComponent<SkillButton>().RegisterSkill(skill);
            skill.RegisterOnSkillPressed(() => { go.GetComponent<SkillButton>().OnSkillPressed(); });
            skill.RegisterOnSkillReleased(() => { go.GetComponent<SkillButton>().OnSkillReleased(); });
            skill.RegisterOnSkillBegin(() => { go.GetComponent<SkillButton>().OnSkillBegin(); });
        }

        var hLayout = m_SkillHolder.GetComponent<HorizontalLayoutGroup>();
        m_SkillsIconGap = hLayout.padding.left + hLayout.spacing + m_SkillButtonPrefab.GetComponent<RectTransform>().rect.width / 2;

    }

    Sprite FindSpriteWithName(string name)
    {
        foreach (Sprite sprite in m_SkillIcons)
        {
            if (sprite.name == name)
                return sprite;
        }
        return null;
    }

    private void LateUpdate()
    {
        if (!m_IsInitialized)
        {
            m_Player = GameObject.FindWithTag("Player").GetComponent<EntityPlayer>();
            m_PrevPlayerHP = m_Player.St_stats.F_health;
            m_PrevPlayerMP = m_Player.St_stats.F_mana;

            PopulateSkillIcons();
            m_IsInitialized = true;
        }

        // Subtle tweening of health/mana bar when its values are changed
        if (m_Player.St_stats.F_health != m_PrevPlayerHP)
        {
            float healthPercent = (m_Player.St_stats.F_health / m_Player.St_stats.F_max_health);
            m_PlayerHealthTexture.DOFillAmount(healthPercent, 0.2f).SetEase(Ease.OutExpo);
            m_PrevPlayerHP = m_Player.St_stats.F_health;
        }

        if (m_Player.St_stats.F_mana != m_PrevPlayerMP)
        {
            float manaPercent = (m_Player.St_stats.F_mana / m_Player.St_stats.F_max_mana);
            m_PlayerManaTexture.DOFillAmount(manaPercent, 0.2f).SetEase(Ease.OutExpo);
            m_PrevPlayerMP = m_Player.St_stats.F_mana;
        }

        // Assign potion amount text
        var playerInventory = m_Player.GetInventory().GetInventoryContainer();
        int hpPots = 0, manaPots = 0;

        foreach (var item in playerInventory)
        {
            if (item.Key == Item.ITEM_TYPE.HEALTH_POTION)
                hpPots = item.Value;
            if (item.Key == Item.ITEM_TYPE.MANA_POTION)
                manaPots = item.Value;
        }

        m_HealthPotionAmount.text = hpPots.ToString();
        m_ManaPotionAmount.text = manaPots.ToString();

        // Minimap Camera
        Vector3 newCamPos = m_MinimapCamera.transform.position;
        newCamPos.x = m_Player.transform.position.x;
        newCamPos.z = m_Player.transform.position.z;
        m_MinimapCamera.transform.position = newCamPos; // follow player's x and z position

        // Rotate the minimap depending on the current camera angle;        
        m_Minimap.transform.rotation = Quaternion.Euler(0, 0, TPCamera.f_CurrentAngle);

        // Skill icons movement
        if (Input.GetKeyUp(KeyCode.Q) || Input.GetKeyUp(KeyCode.E))
        {
            RectTransform holderTransform = m_SkillHolder.GetComponent<RectTransform>();
            holderTransform.DOAnchorPosX(m_Player.GetInventory().GetCurrSkillIndex() * -m_SkillsIconGap, 0.5f).SetEase(Ease.OutBack);
        }

    }

}
