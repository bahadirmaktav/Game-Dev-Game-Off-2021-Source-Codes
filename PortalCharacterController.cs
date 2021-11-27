﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCharacterController : MonoBehaviour
{
    [Header("Gravity Input Paramaters")]
    [SerializeField] private float temporaryOutPortalShiftConstant = 0.5f;
    [SerializeField] private float characterVelocityReduceWhenEnteredPortal = 0.3f;

    [Header("Initializations")]
    protected CharacterMovementController characterMovementController;
    protected Rigidbody2D rb;
    protected CharacterAIMovementController characterAIMovementController;
    protected PortalPlacementController portalPlacementController;
    protected BoxCollider2D coll;
    public GameObject characterPrefab;

    [Header("Gravity Control Parameters")]
    protected float portalDetectRayLength = 1f;
    protected int layerMaskOnlyPortal = 1 << 12;
    protected bool enteredPortal;
    protected bool exitedPortal;
    protected bool isDestroyOpStart;
    protected Vector2 initialCharacterPosHolder;
    protected GameObject oldGameObj;

    protected void Initializations(GameObject character)
    {
        characterMovementController = character.GetComponent<CharacterMovementController>();
        characterAIMovementController = character.GetComponent<CharacterAIMovementController>();
        portalPlacementController = GetComponent<PortalPlacementController>();
        rb = character.GetComponent<Rigidbody2D>();
        coll = character.GetComponent<BoxCollider2D>();
        initialCharacterPosHolder = rb.position;
        enteredPortal = false;
        exitedPortal = true;
        isDestroyOpStart = false;
    }

    protected void Awake()
    {
        GameObject character = GameObject.Find("Character");
        Initializations(character);
    }

    protected void FixedUpdate()
    {
        if (exitedPortal && (initialCharacterPosHolder - rb.position).magnitude > temporaryOutPortalShiftConstant * 3)
        {
            coll.enabled = true;
            exitedPortal = false;
        }
        if (isDestroyOpStart && (initialCharacterPosHolder - rb.position).magnitude > temporaryOutPortalShiftConstant * 3)
        {
            Destroy(oldGameObj);
            GameObject[] portals = GameObject.FindGameObjectsWithTag("Portal");
            for (int i = 0; i < portals.Length; i++) { Destroy(portals[i]); }
            portalPlacementController.isPortalPlacementActive = true;
            isDestroyOpStart = false;
        }

        bool activatePathFinder = characterAIMovementController.activatePathFinder;
        if (activatePathFinder && !enteredPortal)
        {
            bool isGravityVer = characterMovementController.isGravityVer;
            float gravityDirCorrecter = characterMovementController.gravityDirCorrecter;
            Vector2 rayDir = (isGravityVer) ? Vector2.down * gravityDirCorrecter : Vector2.left * gravityDirCorrecter;
            RaycastHit2D hit = Physics2D.Raycast(rb.position, rayDir, portalDetectRayLength, layerMaskOnlyPortal);
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.name == "InPortal")
                {
                    enteredPortal = true;
                }
            }
        }

        if (enteredPortal)
        {
            coll.enabled = false;
            characterMovementController.isGrounded = false;
            characterAIMovementController.movDirConstant *= characterVelocityReduceWhenEnteredPortal;
            characterAIMovementController.activatePathFinder = false;
            oldGameObj = rb.gameObject;
            float oldGameObjVelocityAmount = (characterMovementController.isGravityVer) ? Mathf.Abs(characterMovementController.velocity.y) : Mathf.Abs(characterMovementController.velocity.x);
            Vector2 outPortalPos = GameObject.Find("OutPortal").transform.position;
            Vector2 gravityDir = portalPlacementController.outPortalGravityDir;
            GameObject teleportedCharacter = Instantiate(characterPrefab, outPortalPos + -gravityDir * temporaryOutPortalShiftConstant, Quaternion.Euler(0, 0, 0));
            oldGameObj.name = "OldCharacter";
            teleportedCharacter.name = "TeleportedCharacter";
            teleportedCharacter.GetComponent<CharacterLevelController>().totalCollectableObjectsCounter = oldGameObj.GetComponent<CharacterLevelController>().totalCollectableObjectsCounter;
            Initializations(teleportedCharacter);
            SpriteRenderer spriteRenderer = teleportedCharacter.transform.GetChild(0).GetComponent<SpriteRenderer>();
            if (gravityDir == Vector2.up)
            {
                teleportedCharacter.transform.rotation = Quaternion.Euler(0, 0, 0);
                spriteRenderer.flipY = true;
            }
            else if (gravityDir == Vector2.down)
            {
                teleportedCharacter.transform.rotation = Quaternion.Euler(0, 0, 0);
                spriteRenderer.flipY = false;
            }
            else if (gravityDir == Vector2.right)
            {
                teleportedCharacter.transform.rotation = Quaternion.Euler(0, 0, -90);
                spriteRenderer.flipY = true;
            }
            else if (gravityDir == Vector2.left)
            {
                teleportedCharacter.transform.rotation = Quaternion.Euler(0, 0, -90);
                spriteRenderer.flipY = false;
            }
            characterMovementController.velocity = gravityDir * oldGameObjVelocityAmount;
            characterMovementController.gravityDirectionModifier = gravityDir;
            characterMovementController.isGravityVer = (gravityDir.x != 0) ? false : true;
            enteredPortal = false;
            isDestroyOpStart = true;
            portalPlacementController.allowedPortalPairPlacamentNumber -= 1;
        }
    }
}
