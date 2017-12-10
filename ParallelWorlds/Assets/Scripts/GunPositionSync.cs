using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GunPositionSync : NetworkBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform handMount;
    [SerializeField] Transform gunPivot;
    [SerializeField] Transform rightHandHold;
    [SerializeField] Transform leftHandHold;
    [SerializeField] float threshold = 10f;
    [SerializeField] float smoothing = 5f;


    [SyncVar] float pitch;
    Vector3 lastOffset;
    float lastSyncedPitch;
    Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();

        if (isLocalPlayer)
        {
            gunPivot.parent = cameraTransform;
        }
        else
        {
            lastOffset = handMount.position - transform.position;
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            pitch = cameraTransform.localRotation.eulerAngles.x;
            if (Mathf.Abs(lastSyncedPitch - pitch) >= threshold)
            {
                CmdUpdatePitch(pitch);
                lastSyncedPitch = pitch;
            }
        }
        else
        {
            Quaternion newRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector3 currentOffset = handMount.position - transform.position;
            gunPivot.localPosition += currentOffset - lastOffset;
            lastOffset = currentOffset;

            gunPivot.localRotation = Quaternion.Lerp(gunPivot.localRotation, newRotation, Time.deltaTime * smoothing);
        }
    }

    [Command]
    void CmdUpdatePitch(float newPitch)
    {
        pitch = newPitch;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (null == anim)
        {
            return;
        }

        if (anim.GetCurrentAnimatorStateInfo(layerIndex).IsName("Base Layer.Death"))
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }
		else
		{
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
            anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandHold.position);
            anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandHold.rotation);

			anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
            anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandHold.position);
            anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandHold.rotation);
		}
    }
}
