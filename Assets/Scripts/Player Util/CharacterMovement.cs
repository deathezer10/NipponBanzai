﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    /*--------------------------------------------------- INITIALIZATION ---------------------------------------------------*/
    private Vector3
        v3_player_new_dir, 
        v3_player_last_position;   //Storing of camera's last moved position  


    private EntityPlayer
        Component_Player;

    private GameObject
        go_camera;

    private bool
        b_moving, 
        b_opposite;

    void Awake()
    {
        Component_Player = gameObject.GetComponent<EntityPlayer>();
    }

    void Start()
    {
        v3_player_last_position = transform.position;   //Saving the player's last position into a Vector3 variable  
        go_camera = null;

        if (go_camera == null)
            go_camera = GameObject.Find("Main Camera");
    }

    /*---------------------------------------------------------------------------------------------------------------------*/


    /*------------------------------------------------------ UPDATES ------------------------------------------------------*/
    void Update()
    {
        //Debug.Log("STATE: " + Component_Player.GetPlayerState());

        if(Component_Player.GetPlayerTargetState() == EntityPlayer.TARGET_STATE.AIMING)
            v3_player_new_dir = new Vector3(go_camera.transform.forward.x, Component_Player.transform.forward.y, go_camera.transform.forward.z).normalized;


        if (Component_Player != null && Component_Player.Rb_rigidbody != null)
        {
            if (Component_Player.GetPlayerState() == EntityPlayer.State.IDLE)
            {
                if (v3_player_last_position != transform.position)
                    v3_player_last_position = transform.position;   //Saving the Player's last position into a Vector3 variable     

                b_moving = false;
                b_opposite = false;
            }
            else if (Component_Player.GetPlayerState() == EntityPlayer.State.MOVING || Component_Player.GetPlayerState() == EntityPlayer.State.ATTACK)
            {
                v3_player_last_position = transform.position;   //Saving the Player's last position into a Vector3 variable                             

                switch (Component_Player.GetPlayerTargetState())
                {
                    case EntityPlayer.TARGET_STATE.AIMING:
                        {
                            if (!Component_Player.An_animator.GetBool("IsMelee"))
                            {
                                Vector3 new_pos = Component_Player.Rb_rigidbody.transform.position;

                                if (Input.GetKey(KeyCode.W))
                                {
                                    new_pos += Time.unscaledDeltaTime * (transform.forward).normalized * Component_Player.GetStats().F_speed * 0.3f;
                                }
                                else if (Input.GetKey(KeyCode.S))
                                {
                                    new_pos -= Time.unscaledDeltaTime * (transform.forward).normalized * Component_Player.GetStats().F_speed * 0.3f;
                                }

                                if (Input.GetKey(KeyCode.A))
                                {
                                    new_pos -= Time.unscaledDeltaTime * (transform.right).normalized * Component_Player.GetStats().F_speed * 0.3f;
                                }
                                else if (Input.GetKey(KeyCode.D))
                                {
                                    new_pos += Time.unscaledDeltaTime * (transform.right).normalized * Component_Player.GetStats().F_speed * 0.3f;
                                }

                                Component_Player.Rb_rigidbody.MovePosition(new_pos);

                            }
                        }
                        break;

                    case EntityPlayer.TARGET_STATE.NOT_AIMING:
                        {
                            Vector3 _speed = Vector3.zero;
                            Vector3 _max_speed = Time.unscaledDeltaTime * (transform.forward).normalized * Component_Player.GetStats().F_speed;

                            if (Input.GetKey(KeyCode.A))
                            {
                                v3_player_new_dir = -new Vector3(go_camera.transform.right.x, Component_Player.transform.forward.y, go_camera.transform.right.z).normalized;
                                _speed = _max_speed;
                            }
                            else if (Input.GetKey(KeyCode.D))
                            {
                                v3_player_new_dir = new Vector3(go_camera.transform.right.x, Component_Player.transform.forward.y, go_camera.transform.right.z).normalized;
                                _speed = _max_speed;
                            }

                            if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)))
                            {
                                if (Input.GetKey(KeyCode.W))
                                {
                                    //transform.position += Time.deltaTime * (transform.forward).normalized * Component_Player.GetStats().F_speed * 2;
                                    v3_player_new_dir += new Vector3(go_camera.transform.forward.x, Component_Player.transform.forward.y, go_camera.transform.forward.z).normalized;
                                    _speed = _max_speed;

                                }
                                else if (Input.GetKey(KeyCode.S))
                                {
                                    v3_player_new_dir += -new Vector3(go_camera.transform.forward.x, Component_Player.transform.forward.y, go_camera.transform.forward.z).normalized;
                                    _speed = _max_speed;
                                }
                            }
                            else
                            {
                                if (Input.GetKey(KeyCode.W))
                                {
                                    //transform.position += Time.deltaTime * (transform.forward).normalized * Component_Player.GetStats().F_speed * 2;
                                    v3_player_new_dir = new Vector3(go_camera.transform.forward.x, Component_Player.transform.forward.y, go_camera.transform.forward.z).normalized;
                                    _speed = _max_speed;
                                }
                                else if (Input.GetKey(KeyCode.S))
                                {
                                    v3_player_new_dir = -new Vector3(go_camera.transform.forward.x, Component_Player.transform.forward.y, go_camera.transform.forward.z).normalized;
                                    _speed = _max_speed;
                                }
                            }

                            if (!b_opposite && -gameObject.transform.forward == v3_player_new_dir)
                            {
                                b_opposite = true;
                            }
                         
                            if ((!b_moving || b_opposite) && Vector3.Angle(gameObject.transform.forward, v3_player_new_dir) < 25)
                            {
                                b_moving = true;
                                b_opposite = false;
                            }

                            if (b_moving && !b_opposite)
                                Component_Player.Rb_rigidbody.MovePosition(transform.position + _speed);
                        }
                        break;

                    default:
                        break;
                }
            }

            if (v3_player_new_dir != Vector3.zero)
            {
                Quaternion new_rotate = Quaternion.LookRotation(v3_player_new_dir);
                Quaternion new_target_rotation = Quaternion.Slerp(Component_Player.transform.rotation, new_rotate, 0.3f);

                Component_Player.Rb_rigidbody.MoveRotation(new_target_rotation);
            }
        }
    }
    /*----------------------------------------------------------------------------------------------------------------------*/

    protected void FixedUpdate()
    {
        if (Component_Player.GetPlayerState() == EntityPlayer.State.DASHING)
        {
            float f_speed_multiplier = 1000;

            switch (DoubleTapCheck.GetInstance().GetDoubleTapKey())
            {
                case KeyCode.W:
                    Dash(new Vector3(go_camera.transform.forward.x, Component_Player.transform.forward.y, go_camera.transform.forward.z).normalized, f_speed_multiplier);
                    break;

                case KeyCode.A:
                    Dash(-new Vector3(go_camera.transform.right.x, Component_Player.transform.forward.y, go_camera.transform.right.z).normalized, f_speed_multiplier);
                    break;

                case KeyCode.S:
                    Dash(-new Vector3(go_camera.transform.forward.x, Component_Player.transform.forward.y, go_camera.transform.forward.z).normalized, f_speed_multiplier);
                    break;

                case KeyCode.D:
                    Dash(new Vector3(go_camera.transform.right.x, Component_Player.transform.forward.y, go_camera.transform.right.z).normalized, f_speed_multiplier);
                    break;
                        
                default:
                    break;
            }
        }
    }

    public void Dash(Vector3 _dir, float _speed)
    {
        v3_player_new_dir = _dir;
        Component_Player.Rb_rigidbody.AddForce(_dir * _speed, ForceMode.Acceleration);
    }
}
