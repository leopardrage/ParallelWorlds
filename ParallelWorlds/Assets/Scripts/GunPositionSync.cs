using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GunPositionSync : NetworkBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _handMount;
    [SerializeField] private Transform _gunPivot;
    [SerializeField] private Transform _rightHandHold;
    [SerializeField] private Transform _leftHandHold;
    [SerializeField] private float _threshold = 10f;
    [SerializeField] private float _smoothing = 5f;

    [SyncVar] private float _pitch;

    private Vector3 _lastOffset;
    private float _lastSyncedPitch;
    private Animator _anim;

    private void Start()
    {
        _anim = GetComponent<Animator>();

        if (isLocalPlayer)
        {
            _gunPivot.parent = _cameraTransform;
        }
        else
        {
            _lastOffset = _handMount.position - transform.position;
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            _pitch = _cameraTransform.localRotation.eulerAngles.x;
            if (Mathf.Abs(_lastSyncedPitch - _pitch) >= _threshold)
            {
                CmdUpdatePitch(_pitch);
                _lastSyncedPitch = _pitch;
            }
        }
        else
        {
            Quaternion newRotation = Quaternion.Euler(_pitch, 0f, 0f);

            Vector3 currentOffset = _handMount.position - transform.position;
            _gunPivot.localPosition += currentOffset - _lastOffset;
            _lastOffset = currentOffset;

            _gunPivot.localRotation = Quaternion.Lerp(_gunPivot.localRotation, newRotation, Time.deltaTime * _smoothing);
        }
    }

    [Command]
    private void CmdUpdatePitch(float newPitch)
    {
        _pitch = newPitch;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (null == _anim)
        {
            return;
        }

        if (_anim.GetCurrentAnimatorStateInfo(layerIndex).IsName("Base Layer.Death"))
        {
            _anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            _anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }
        else
        {
            _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            _anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
            _anim.SetIKPosition(AvatarIKGoal.RightHand, _rightHandHold.position);
            _anim.SetIKRotation(AvatarIKGoal.RightHand, _rightHandHold.rotation);

            _anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
            _anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
            _anim.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandHold.position);
            _anim.SetIKRotation(AvatarIKGoal.LeftHand, _leftHandHold.rotation);
        }
    }
}
