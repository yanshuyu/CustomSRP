using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCameraController : MonoBehaviour
{
    [SerializeField] Transform targetTransform;
    [SerializeField, Min(0)] float followDistance = 5;
    [SerializeField, Range(0, 5)] float focusRange = 2.5f;
    [SerializeField, Range(0, 1)] float autoCenterDamping = 0.5f;
    [SerializeField, Min(0)] float rotationSpeed = 90f;
    [SerializeField] Vector2 minMaxVerticalAngles = new Vector2(-30, 89);
    [SerializeField, Min(0)] float autoAlignDelay = 3f;
    [SerializeField, Range(0, 90)] float autoAlignSmoothAngle = 45f;
    [SerializeField] LayerMask obstructionMask = -1;

    float lastManualRotateTime = 0f;
    Vector3 focusPos = Vector3.zero;
    Vector3 lastFocusPos = Vector3.zero;
    Vector3 orbitAngles = Vector3.zero;
    Quaternion alignToGravityRot = Quaternion.identity;

    Camera thisCamera;
    Vector3 cameraNearHalfExtend {
        get {
            Vector3 ext;
            ext.y = Mathf.Tan(thisCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * thisCamera.nearClipPlane;
            ext.x = ext.y * thisCamera.aspect;
            ext.z = 0;
            return ext;
        }
    }


    private void OnValidate() {
        minMaxVerticalAngles.x = Mathf.Clamp(minMaxVerticalAngles.x, -89, minMaxVerticalAngles.y);
        minMaxVerticalAngles.y = Mathf.Clamp(minMaxVerticalAngles.y, minMaxVerticalAngles.x, 89); 
    }

    // Start is called before the first frame update
    void Start() {
        thisCamera = gameObject.GetComponent<Camera>();
        focusPos = targetTransform.position;
        lastManualRotateTime = Time.unscaledTime;
        lastFocusPos = focusPos;
        orbitAngles = transform.localRotation.eulerAngles;
        orbitAngles.y = 0;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }


    private void LateUpdate() {
        UpdateFocusPosition();
        Quaternion lookRot = UpdateRotation();
        Vector3 lookDir =  lookRot * Vector3.forward;
        float distance = followDistance;
        // if (Physics.Raycast(focusPos, -lookDir, out RaycastHit hit, distance, obstructionMask)) {
        //     distance = hit.distance;
        // }
        if (Physics.BoxCast(focusPos, cameraNearHalfExtend, -lookDir, out RaycastHit hit, lookRot, distance - thisCamera.nearClipPlane, obstructionMask)) {
            distance = hit.distance + thisCamera.nearClipPlane;
        }
        Vector3 lookPos = focusPos - lookDir * distance;
        gameObject.transform.SetPositionAndRotation(lookPos, lookRot);
    }

    private void UpdateFocusPosition() {
        lastFocusPos = focusPos;
        float distance = Vector3.Distance(focusPos, targetTransform.position);
        float t = 1f;
        if (distance > 0.05f && autoCenterDamping > 0) {
            t = Mathf.Pow(autoCenterDamping, Time.unscaledDeltaTime);
        }
        if (distance > focusRange) {
            t =  Mathf.Min(t, focusRange / distance);
        }

        focusPos = Vector3.Lerp(targetTransform.position, focusPos, t);
    }

    private bool ManualRotation() {
        Vector3 input = new Vector3(Input.GetAxis("Vertical Camera"), Input.GetAxis("Horizontal Camera"), 0);
        float epsilon = 0.01f;
        if (Mathf.Abs(input.x) > epsilon || Mathf.Abs(input.y) > epsilon) {
            orbitAngles += input * rotationSpeed * Time.unscaledDeltaTime;
            lastManualRotateTime = Time.unscaledTime;
            return true;
        }

        return false;
    }

    private bool AutoAlignRotation() { // automic rotate camera around y axis to follow player
        if (Time.unscaledTime - lastManualRotateTime < autoAlignDelay)
            return false;
   
        Vector3 offset = Quaternion.Inverse(alignToGravityRot) * (focusPos - lastFocusPos);
        Vector2 xzMovement = new Vector2(offset.x, offset.z);
        float moveAount = xzMovement.magnitude;
        if (moveAount <= 0.001)
            return false;
        
        Vector2 moveDir = xzMovement / moveAount;
        float angle = Mathf.Acos(moveDir.y) * Mathf.Rad2Deg;
        if (moveDir.x < 0)
            angle = 360 - angle;

        float maxRotateChange = rotationSpeed * Time.unscaledDeltaTime;
        float detalAngle = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, angle));
        if (detalAngle < autoAlignSmoothAngle)
            maxRotateChange *= (detalAngle / autoAlignSmoothAngle);

        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, angle, maxRotateChange);

        // Vector3 xzMovement = new Vector3(focusPos.x - lastFocusPos.x, 0, focusPos.z - lastFocusPos.z);
        // float moveAmount = xzMovement.magnitude;
        // if (moveAmount <= 0.01)
        //     return false;

        // Vector3 moveDir = xzMovement / moveAmount;
        // orbitAngles.y = Mathf.Acos(Vector3.Dot(Vector3.forward, moveDir)) * Mathf.Rad2Deg;    

        return true;
    }

    private Quaternion UpdateRotation() {
        Quaternion rot = Quaternion.Euler(orbitAngles);
        bool hasRotation = ManualRotation() || AutoAlignRotation();
        if (hasRotation) {
            orbitAngles.x = Mathf.Clamp(orbitAngles.x, minMaxVerticalAngles.x, minMaxVerticalAngles.y);
            if (orbitAngles.y < 0)
                orbitAngles.y += 360;
            if (orbitAngles.y > 360)
                orbitAngles.y -= 360;
            rot = Quaternion.Euler(orbitAngles);
        }

        alignToGravityRot = Quaternion.FromToRotation(Vector3.up, -Physics.gravity.normalized);
        rot = alignToGravityRot * rot;

        return rot;
    }
}
