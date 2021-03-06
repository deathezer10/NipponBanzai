﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// Author: Esther
public class DialogueHandler : MonoBehaviour
{

    public Text nameText; //Name of Person conversing
    public Text dialogueText; //Dialogue

    public delegate void OnDialogueEndDelegate();
    public static OnDialogueEndDelegate m_OnDialogueEnd;

    public static bool isInDialogue { get; set; }

    private Queue<string> sentences;

    private const float tweenPosY = 200;

    private bool isSentencing = false;

    private string currentSentence = "";

    // Use this for initialization
    void Start()
    {
        sentences = new Queue<string>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isInDialogue)
        {
            DisplayNextSentence();
        }

    }

    public void StartDialogue(Dialogue dialogue)
    {
        nameText.transform.parent.GetComponent<RectTransform>().DOAnchorPosY(tweenPosY, 1).SetEase(Ease.OutExpo);

        // remove later
        // Debug.Log("Starting conversation with " + dialogue.name);
        nameText.text = dialogue.name;

        //Clearing all sentences from/if any previous dialogue
        sentences.Clear();

        foreach (string sentence in dialogue.sentences)
        {
            sentences.Enqueue(sentence); // Queueing up the sentences of the dialogue
        }
        DisplayNextSentence();

        isInDialogue = true;
    }

    public void DisplayNextSentence()
    {
        if (isSentencing)
        {
            StopAllCoroutines();
            dialogueText.text = currentSentence;
            isSentencing = false;
        }
        else
        {
            if (sentences.Count == 0)
            {
                EndDialogue();
                return;
            }

            currentSentence = sentences.Dequeue(); // Getting the next sentence in the queue

            //This to make sure that it stops animating if the user rushes through CONTINUE
            StopAllCoroutines();
            //Start animating! 
            StartCoroutine(TypeSentence(currentSentence));
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        isSentencing = true;
        dialogueText.text = "";
        //Loop through all character in individual sentence
        foreach (char letter in sentence.ToCharArray()) // ToCharArray is a function that converts string into a character array
        {
            dialogueText.text += letter; //Appending letter to the end of the string
            //After each letter, wait a small amount of time
            yield return null; //This returns a single frame
        }
        isSentencing = false;
    }

    void EndDialogue()
    {
        nameText.transform.parent.GetComponent<RectTransform>().DOAnchorPosY(-tweenPosY, 1).SetEase(Ease.OutExpo);

        isInDialogue = false;

        if (m_OnDialogueEnd != null)
        {
            m_OnDialogueEnd.Invoke();
            m_OnDialogueEnd = EmptyDelegate;
        }
    }

    public void SetInDialog(bool toggle)
    {
        isInDialogue = toggle;
    }

    void EmptyDelegate()
    {

    }

}

[System.Serializable]
public class Dialogue
{
    // Store all information needed for a single dialogue
    public string name = ""; // Name of NPC

    public List<string> sentences = new List<string>();

}
