using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Experimental.GraphView;
using TMPro;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using UnityEditor;

public class Player : MonoBehaviour
{
    private bool isGrounded;
    private bool isSprinting;

    private Transform cam;
    private World world;

    private const float WALK_SPEED = 3f;
    private const float SPRINT_SPEED = 6f;
    private const float JUMP_FORCE = 5f;
    private const float GRAVITY = -9.8f;

    private const float PLAYER_RADIUS = 0.4f;
    private const float PLAYER_HEIGHT = 2f;
    private const float PLAYER_EYES_HEIGHT = 1.8f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    public Transform highlightBlock;
    public Transform placeHighlightBlock;

    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public byte selectedBlockIndex = 1;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();

    }

    private void FixedUpdate()
    {
        CalculateVelocity();

        if (jumpRequest) {
            Jump();
        }

        transform.Translate(velocity, Space.World);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * mouseHorizontal);
        cam.Rotate(Vector3.right * -mouseVertical);

        GetPlayerInput();
        PlaceCursorBlocks();
    }

    private void Jump() {
        verticalMomentum = JUMP_FORCE;
        isGrounded = false;
        jumpRequest = false;
    }

    private void CalculateVelocity() {
        //vertical momentum with gravity
        if (verticalMomentum > GRAVITY)
        {
            verticalMomentum += Time.fixedDeltaTime * GRAVITY;
        }

        //sprinting
        if (isSprinting)
        {
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * SPRINT_SPEED;
        }
        else
        {
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * WALK_SPEED;
        }

        //vertical momentum (falling/jumping)
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        //block collision check / apply velocity
        if ((velocity.z > 0 && front) || (velocity.z < 0 && back)) 
        {
            velocity.z = 0;
        }
        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
        {
            velocity.x = 0;
        }

        if (velocity.y < 0)
        {
            velocity.y = checkDownSpeed(velocity.y);
        }
        else if (velocity.y > 0) {
            velocity.y = checkUpSpeed(velocity.y);
        }
    }

    private void GetPlayerInput() 
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");

        if (Input.GetButtonDown("Sprint"))
        {
            isSprinting = true;
        }
        if (Input.GetButtonUp("Sprint"))
        {
            isSprinting = false;
        }

        if (isGrounded && Input.GetButton("Jump"))
        {
            jumpRequest = true;
        }

        if (highlightBlock.gameObject.activeSelf)
        {
            //destroy block
            //GetButton allows you to hold
            if (Input.GetButton("Fire1"))
            {
                world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);
            }
            //place block
            //GetButtonDown requires you to tap
            if (Input.GetButtonDown("Fire2") && !HighlightBlockInPlayer())
            {
                world.GetChunkFromVector3(placeHighlightBlock.position).EditVoxel(placeHighlightBlock.position, selectedBlockIndex);
            }
        }
    }

    bool HighlightBlockInPlayer() 
    {
        bool insidePlayer = false;
        Vector3Int playerPos = new Vector3Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y), Mathf.FloorToInt(transform.position.z));

        for (int i = 0; i < Mathf.CeilToInt(PLAYER_HEIGHT); i++) 
        {
            if (placeHighlightBlock.position == playerPos)
            {
                insidePlayer = true;
            }
            playerPos.y++;
        }

        return insidePlayer;
    }

    private void PlaceCursorBlocks() 
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while (step < reach) 
        {
            Vector3 pos = cam.position + (cam.forward * step);

            if (world.CheckForVoxel(pos)) 
            {
                highlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placeHighlightBlock.position = lastPos;

                highlightBlock.gameObject.SetActive(true);
                placeHighlightBlock.gameObject.SetActive(true);
                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            step += checkIncrement;
        }

        highlightBlock.gameObject.SetActive(false);
        placeHighlightBlock.gameObject.SetActive(false);

    }

    private float checkDownSpeed(float downSpeed) 
    {
        if (
        (world.CheckForVoxel(new Vector3(transform.position.x - PLAYER_RADIUS, transform.position.y + downSpeed, transform.position.z - PLAYER_RADIUS))) && (!left  && !back) ||
        (world.CheckForVoxel(new Vector3(transform.position.x + PLAYER_RADIUS, transform.position.y + downSpeed, transform.position.z - PLAYER_RADIUS))) && (!right && !back) ||
        (world.CheckForVoxel(new Vector3(transform.position.x + PLAYER_RADIUS, transform.position.y + downSpeed, transform.position.z + PLAYER_RADIUS))) && (!right && !front) ||
        (world.CheckForVoxel(new Vector3(transform.position.x - PLAYER_RADIUS, transform.position.y + downSpeed, transform.position.z + PLAYER_RADIUS))) && (!left  && !front))
        {
            isGrounded = true;
            return 0;
        }

        isGrounded = false;
        return downSpeed;
    }

    private float checkUpSpeed(float upSpeed)
    {
        if (
        (world.CheckForVoxel(new Vector3(transform.position.x - PLAYER_RADIUS, transform.position.y + PLAYER_HEIGHT + upSpeed, transform.position.z - PLAYER_RADIUS))) && (!left  && !back) ||
        (world.CheckForVoxel(new Vector3(transform.position.x + PLAYER_RADIUS, transform.position.y + PLAYER_HEIGHT + upSpeed, transform.position.z - PLAYER_RADIUS))) && (!right && !back) ||
        (world.CheckForVoxel(new Vector3(transform.position.x + PLAYER_RADIUS, transform.position.y + PLAYER_HEIGHT + upSpeed, transform.position.z + PLAYER_RADIUS))) && (!right && !front) ||
        (world.CheckForVoxel(new Vector3(transform.position.x - PLAYER_RADIUS, transform.position.y + PLAYER_HEIGHT + upSpeed, transform.position.z + PLAYER_RADIUS))) && (!left  && !front))
        {
            verticalMomentum = 0;
            return 0;
        }
        return upSpeed;
    }

    public bool front {
        get {
            return (
            world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y,                     transform.position.z + PLAYER_RADIUS)) ||
            world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + PLAYER_HEIGHT - 1, transform.position.z + PLAYER_RADIUS)));
        }
    }
    public bool back {
        get {
            return (
            world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y,                     transform.position.z - PLAYER_RADIUS)) ||
            world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + PLAYER_HEIGHT - 1, transform.position.z - PLAYER_RADIUS)));
        }
    }
    public bool left {
        get {
            return (
            world.CheckForVoxel(new Vector3(transform.position.x - PLAYER_RADIUS, transform.position.y,                     transform.position.z)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - PLAYER_RADIUS, transform.position.y + PLAYER_HEIGHT - 1, transform.position.z)));
        }
    }
    public bool right {
        get {
            return (
            world.CheckForVoxel(new Vector3(transform.position.x + PLAYER_RADIUS, transform.position.y,                     transform.position.z)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + PLAYER_RADIUS, transform.position.y + PLAYER_HEIGHT - 1, transform.position.z)));
        }
    }
}
