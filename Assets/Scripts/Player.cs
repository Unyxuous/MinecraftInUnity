using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

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

    private const float PLAYER_RADIUS = 0.25f;
    private const float PLAYER_HEIGHT = 2f;
    private const float PLAYER_EYES_HEIGHT = 1.8f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    public int orientation;

    public Transform highlightBlock;
    public Transform placeHighlightBlock;

    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public HotBar hotbar;

    private void Start()
    {
        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();
        world.inUI = false;
    }

    private void FixedUpdate()
    {
        if (!world.inUI)
        {
            CalculateVelocity();

            if (jumpRequest)
            {
                Jump();
            }

            transform.Translate(velocity, Space.World);
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("Inventory"))
        {
            world.inUI = !world.inUI;
        }

        if (!world.inUI)
        {
            transform.Rotate(Vector3.up * mouseHorizontal * world.settings.mouseSensitivity);
            cam.Rotate(Vector3.right * -mouseVertical * world.settings.mouseSensitivity);

            GetPlayerInput();
            PlaceCursorBlocks();
        }

        //only concerned with rotating left/right, not up/down for now, so y is set to 0
        Vector3 XZDirection = transform.forward;
        XZDirection.y = 0;
        //orientation is based on VoxelData.voxelTriangles
        if (Vector3.Angle(XZDirection, Vector3.forward) <= 45)
        {
            orientation = 0;
        }
        else if (Vector3.Angle(XZDirection, Vector3.right) <= 45)
        {
            orientation = 5;
        }
        else if (Vector3.Angle(XZDirection, Vector3.back) <= 45)
        {
            orientation = 1;
        }
        else if (Vector3.Angle(XZDirection, Vector3.left) <= 45)
        {
            orientation = 4;
        }
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
        if (Input.GetButtonDown("Cancel"))
        {
            Application.Quit();
        }

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
            if (Input.GetButtonDown("Fire1"))
            {
                world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);
            }
            //place block
            //GetButtonDown requires you to tap
            if (Input.GetButtonDown("Fire2") && !HighlightBlockInPlayer())
            {
                if (hotbar.slots[hotbar.slotIndex].HasItem)
                {
                    world.GetChunkFromVector3(placeHighlightBlock.position).EditVoxel(placeHighlightBlock.position, hotbar.slots[hotbar.slotIndex].itemSlot.stack.id);
                    hotbar.slots[hotbar.slotIndex].itemSlot.Take(1);
                }
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
            Vector3 realPos = cam.position + (cam.forward * step);
            Vector3Int pos = new Vector3Int(Mathf.FloorToInt(realPos.x), Mathf.FloorToInt(realPos.y), Mathf.FloorToInt(realPos.z));

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
        int minusX = Mathf.FloorToInt(transform.position.x - PLAYER_RADIUS);
        int plusX =  Mathf.FloorToInt(transform.position.x + PLAYER_RADIUS);
        int plusY =  Mathf.FloorToInt(transform.position.y + downSpeed);
        int minusZ = Mathf.FloorToInt(transform.position.z - PLAYER_RADIUS);
        int plusZ =  Mathf.FloorToInt(transform.position.z + PLAYER_RADIUS);
        if (
        (world.CheckForVoxel(new Vector3Int(minusX, plusY, minusZ))) && (!left  && !back) ||
        (world.CheckForVoxel(new Vector3Int(plusX,  plusY, minusZ))) && (!right && !back) ||
        (world.CheckForVoxel(new Vector3Int(plusX,  plusY, plusZ)))  && (!right && !front)||
        (world.CheckForVoxel(new Vector3Int(minusX, plusY, plusZ)))  && (!left  && !front))
        {
            isGrounded = true;
            return 0;
        }

        isGrounded = false;
        return downSpeed;
    }

    private float checkUpSpeed(float upSpeed)
    {
        int minusX = Mathf.FloorToInt(transform.position.x - PLAYER_RADIUS);
        int plusX =  Mathf.FloorToInt(transform.position.x + PLAYER_RADIUS);
        int plusY =  Mathf.FloorToInt(transform.position.y + PLAYER_HEIGHT + upSpeed);
        int minusZ = Mathf.FloorToInt(transform.position.z - PLAYER_RADIUS);
        int plusZ =  Mathf.FloorToInt(transform.position.z + PLAYER_RADIUS);
        if (
        (world.CheckForVoxel(new Vector3Int(minusX, plusY, minusZ))) && (!left  && !back) ||
        (world.CheckForVoxel(new Vector3Int(plusX,  plusY, minusZ))) && (!right && !back) ||
        (world.CheckForVoxel(new Vector3Int(plusX,  plusY, plusZ)))  && (!right && !front)||
        (world.CheckForVoxel(new Vector3Int(minusX, plusY, plusZ)))  && (!left  && !front))
        {
            verticalMomentum = 0;
            return 0;
        }
        return upSpeed;
    }

    public bool front {
        get {
            int x  = Mathf.FloorToInt(transform.position.x);
            int y1 = Mathf.FloorToInt(transform.position.y);
            int y2 = Mathf.FloorToInt(transform.position.y + PLAYER_HEIGHT - 1);
            int z  = Mathf.FloorToInt(transform.position.z + PLAYER_RADIUS);

            return (
            world.CheckForVoxel(new Vector3Int(x, y1, z)) ||
            world.CheckForVoxel(new Vector3Int(x, y2, z)));
        }
    }
    public bool back {
        get {
            int x = Mathf.FloorToInt(transform.position.x);
            int y1 = Mathf.FloorToInt(transform.position.y);
            int y2 = Mathf.FloorToInt(transform.position.y + PLAYER_HEIGHT - 1);
            int z = Mathf.FloorToInt(transform.position.z - PLAYER_RADIUS);

            return (
            world.CheckForVoxel(new Vector3Int(x, y1, z)) ||
            world.CheckForVoxel(new Vector3Int(x, y2, z)));
        }
    }
    public bool left {
        get {
            int x = Mathf.FloorToInt(transform.position.x - PLAYER_RADIUS);
            int y1 = Mathf.FloorToInt(transform.position.y);
            int y2 = Mathf.FloorToInt(transform.position.y + PLAYER_HEIGHT - 1);
            int z = Mathf.FloorToInt(transform.position.z);

            return (
            world.CheckForVoxel(new Vector3Int(x, y1, z)) ||
            world.CheckForVoxel(new Vector3Int(x, y2, z)));
        }
    }
    public bool right {
        get {
            int x = Mathf.FloorToInt(transform.position.x + PLAYER_RADIUS);
            int y1 = Mathf.FloorToInt(transform.position.y);
            int y2 = Mathf.FloorToInt(transform.position.y + PLAYER_HEIGHT - 1);
            int z = Mathf.FloorToInt(transform.position.z);

            return (
            world.CheckForVoxel(new Vector3Int(x, y1, z)) ||
            world.CheckForVoxel(new Vector3Int(x, y2, z)));
        }
    }
}
