using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    public Transform target;         // „O„q„Œ„u„{„„, „r„€„{„‚„…„s „{„€„„„€„‚„€„s„€ „q„…„t„u„„ „r„‚„p„‹„p„„„„ƒ„‘ „{„p„}„u„‚„p („„u„‚„ƒ„€„~„p„w)
    public float distance = 5.0f;    // „Q„p„ƒ„ƒ„„„€„‘„~„y„u „{„p„}„u„‚„ „€„„ „ˆ„u„|„y
    public float xSpeed = 120.0f;    // „R„{„€„‚„€„ƒ„„„ „r„‚„p„‹„u„~„y„‘ „„€ „s„€„‚„y„x„€„~„„„p„|„y
    public float ySpeed = 120.0f;    // „R„{„€„‚„€„ƒ„„„ „r„‚„p„‹„u„~„y„‘ „„€ „r„u„‚„„„y„{„p„|„y

    public float yMinLimit = -20f;   // „M„y„~„y„}„p„|„„~„„z „…„s„€„| „€„q„x„€„‚„p „„€ „r„u„‚„„„y„{„p„|„y
    public float yMaxLimit = 80f;    // „M„p„{„ƒ„y„}„p„|„„~„„z „…„s„€„| „€„q„x„€„‚„p „„€ „r„u„‚„„„y„{„p„|„y

    public CharacterMovement characterMovement; // „R„ƒ„„|„{„p „~„p „ƒ„{„‚„y„„„ „t„r„y„w„u„~„y„‘ „„u„‚„ƒ„€„~„p„w„p

    private float x = 0.0f;
    private float y = 0.0f;
    private float fixedYPosition; // „P„u„‚„u„}„u„~„~„p„‘ „t„|„‘ „‡„‚„p„~„u„~„y„‘ „†„y„{„ƒ„y„‚„€„r„p„~„~„€„z Y „„€„x„y„ˆ„y„y „{„p„}„u„‚„ „r„€ „r„‚„u„}„‘ „„‚„„w„{„p

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("„P„€„w„p„|„…„z„ƒ„„„p, „~„p„x„~„p„‰„„„„u „ˆ„u„|„ („„u„‚„ƒ„€„~„p„w„p) „t„|„‘ „r„‚„p„‹„u„~„y„‘ „{„p„}„u„‚„ „r „y„~„ƒ„„u„{„„„€„‚„u.");
            enabled = false; // „O„„„{„|„„‰„p„u„} „ƒ„{„‚„y„„„, „u„ƒ„|„y „ˆ„u„|„ „~„u „~„p„x„~„p„‰„u„~„p
            return;
        }

        //characterMovement = target.GetComponent<CharacterMovement>(); // „P„€„|„…„‰„p„u„} „ƒ„ƒ„„|„{„… „~„p „ƒ„{„‚„y„„„ „t„r„y„w„u„~„y„‘ „„u„‚„ƒ„€„~„p„w„p
        if (characterMovement == null)
        {
            Debug.LogError("„R„{„‚„y„„„ CharacterMovement „~„u „~„p„z„t„u„~ „~„p „€„q„Œ„u„{„„„u „ˆ„u„|„y. „T„q„u„t„y„„„u„ƒ„, „‰„„„€ „€„~ „„‚„y„{„‚„u„„|„u„~ „{ „„u„‚„ƒ„€„~„p„w„….");
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        fixedYPosition = transform.position.y; // „I„~„y„ˆ„y„p„|„y„x„y„‚„…„u„} „†„y„{„ƒ„y„‚„€„r„p„~„~„…„ Y „„€„x„y„ˆ„y„ „~„p„‰„p„|„„~„„} „x„~„p„‰„u„~„y„u„}
    }

    void LateUpdate()
    {
        if (target)
        {
            x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;

            y = ClampAngle(y, yMinLimit, yMaxLimit); // „O„s„‚„p„~„y„‰„y„r„p„u„} „r„u„‚„„„y„{„p„|„„~„„z „…„s„€„| „€„q„x„€„‚„p

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 targetPosition = target.position; // „P„€„|„…„‰„p„u„} „„€„x„y„ˆ„y„ „ˆ„u„|„y

            Vector3 position = rotation * negDistance + targetPosition;

            // „E„ƒ„|„y „„u„‚„ƒ„€„~„p„w „„‚„„s„p„u„„, „†„y„{„ƒ„y„‚„…„u„} Y „„€„x„y„ˆ„y„ „{„p„}„u„‚„
            if (characterMovement.isActuallyJumping)
            {
                position.y = fixedYPosition; // „I„ƒ„„€„|„„x„…„u„} „†„y„{„ƒ„y„‚„€„r„p„~„~„…„ Y „„€„x„y„ˆ„y„
            }
            else
            {
                fixedYPosition = position.y; // „O„q„~„€„r„|„‘„u„} „†„y„{„ƒ„y„‚„€„r„p„~„~„…„ Y „„€„x„y„ˆ„y„, „{„€„s„t„p „„u„‚„ƒ„€„~„p„w „~„u „„‚„„s„p„u„„, „‰„„„€„q„ „{„p„}„u„‚„p „„‚„€„t„€„|„w„p„|„p „ƒ„|„u„t„€„r„p„„„ „x„p „€„q„‹„u„z „r„„ƒ„€„„„€„z
            }

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
