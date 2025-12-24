using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using MEC;

public class CombatCamera : MonoBehaviour
{
    public static CombatCamera instance;
    public Transform target;
    public AlwaysLerpToTarget freelookTarget;
    private GameObject hero;

    public bool isFreeloking = true;
    public float rotationSpeed = 5.0f;
    public Vector3 posOffset, rotOffset;
    public float smoothing = 5;
    public float baseFreelookSmoothness = 7;

    public Transform cam;

    [System.Serializable]
    public struct CamOffset
    {
        [LabelText("Y")]
        public float yposition;
        [LabelText("Z")]
        public float zPosition;
        [LabelText("R")]
        public float rotation;
    }
    [ListDrawerSettings(Expanded = true, ShowIndexLabels = false)]
    public List<CamOffset> zoomLevels;
    [PropertyRange(0, "maxZoomLv")]
    public int interactionZoomLv;
    private int maxZoomLv { get => zoomLevels.Count - 1; }
    int zoomLv;
    bool closeUp;

    public void SwitchCameraZoomLevel()
    {
        zoomLv++;
        if (zoomLv >= zoomLevels.Count)
        {
            if (closeUp)
                zoomLv = 0;
            else
                zoomLv = 1;
        }

        PlayerPrefs.SetInt("zoomLv", zoomLv);
        SetZoomLevel();
    }

    public void CloseUpCameraChanged(bool isActive)
    {
        closeUp = isActive;
        if (zoomLv == 0 && !closeUp)
            SwitchCameraZoomLevel();
    }
    private void SetZoomLevel()
    {
        posOffset = GetPositionOffset(zoomLv);
        SetRotationX(zoomLevels[zoomLv].rotation);
    }
    private Vector3 GetPositionOffset(int lv)
    {
        return new Vector3(0, zoomLevels[lv].yposition, zoomLevels[lv].zPosition);
    }
    private void SetRotationX(float x)
    {
        rotOffset = new Vector3(x, 0, 0);
        //transform.localRotation = Quaternion.Euler(rotOffset);
    }
    private void LoadZoomLevel()
    {
        if (!PlayerPrefs.HasKey("zoomLv"))
            zoomLv = zoomLevels.Count - 1;
        else zoomLv = PlayerPrefs.GetInt("zoomLv");

        SetZoomLevel();
    }

    private void Awake()
    {
        instance = this;
        cam = transform.GetChild(0);
        LoadZoomLevel();
        //posOffset = defaultOffset;
    }

    [Header("Freelook Setup")]
    public float zoomSpeed = 5f;
    float velocityX = 0.0f;
    float velocityY = 0.0f;
    float velocityZ = 0.0f;
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
    public float distance = 5.0f;
    public float distanceMin = .5f;
    public float distanceMax = 15f;
    float xMovement = 0.0f;
    float yMovement = 0.0f;
    float zMovement = 0.0f;
    public FixedTouchField touchField;

    public float maxEnemiesDistance = 25f;

    float scrollDistance;
    public LayerMask collidedLayer;
    RaycastHit hit;
    Vector3 velocity = Vector3.zero;
    float X, Y;

    [Header("LightCone Minimap")]
    public Transform lightConeParent;
    public Transform lightCone;
    public float tempDistance;

    Settings setting;
    void LateUpdate()
    {
        if (camPoint)
        {
            Vector3 targetCamPos = camPoint.position;
            transform.position = Vector3.Lerp(transform.position, targetCamPos, smoothing * Time.unscaledDeltaTime);

            Vector3 targetCamRot = camPoint.rotation.eulerAngles;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(targetCamRot), smoothing * Time.unscaledDeltaTime);
            return;
        }

        if (target)
        {
            if(isFreeloking)
            {
                if (Vector3.Distance(target.position, freelookTarget.transform.position) > distanceMax + 5f)
                    freelookTarget.ForceToPosition();

                if (freelookTarget)
                {
                    if (touchField.Pressed)
                    {
#if UNITY_EDITOR    
                        //Debug.Log(Input.GetAxis("Mouse X"));
                        velocityX += xSpeed * setting.HorizontalSpeed * Input.GetAxis("Mouse X") * distance * 0.02f;
                        velocityY += ySpeed * setting.VerticalSpeed * Input.GetAxis("Mouse Y") * 0.02f;
#else
                        if (touchField.DetectedTouches.Count == 1)
                        {
                            try
                            {
                                var touch = Input.GetTouch(touchField.DetectedTouches[0]);
                                if (touch.phase == TouchPhase.Moved)
                                {
                                    Vector2 touchDeltaPosition = touch.deltaPosition;
                                    Vector2 normalized = new Vector2(touchDeltaPosition.x / Screen.width, touchDeltaPosition.y / Screen.height);
                                    X = -normalized.x;
                                    Y = -normalized.y;
                                }

                                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                                {
                                    X = 0f;
                                    Y = 0f;
                                }
                            }

                            catch
                            {
                                X = 0f;
                                Y = 0f;
                            }
                            
                            velocityX += X * setting.HorizontalSpeed * -xSpeed * 0.02f;
                            velocityY += Y * setting.VerticalSpeed * -ySpeed * 0.02f;
                        }

                        if (touchField.DetectedTouches.Count > 1)
                        {
                            try
                            {
                                Touch tZero = Input.GetTouch(touchField.DetectedTouches[0]);
                                Touch tOne = Input.GetTouch(touchField.DetectedTouches[1]);

                                Vector2 tZeroNormalized = new Vector2(tZero.position.x / Screen.width, tZero.position.y / Screen.height);
                                Vector2 tOneNormalized = new Vector2(tOne.position.x / Screen.width, tOne.position.y / Screen.height);
                                Vector2 tZeroDeltaNormalized = new Vector2(tZero.deltaPosition.x / Screen.width, tZero.deltaPosition.y / Screen.height);
                                Vector2 tOneDeltaNormalized = new Vector2(tOne.deltaPosition.x / Screen.width, tOne.deltaPosition.y / Screen.height);

                                Vector2 tZeroPrevious = tZeroNormalized - tZeroDeltaNormalized;
                                Vector2 tOnePrevious = tOneNormalized - tOneDeltaNormalized;

                                float oldTouchDistance = Vector2.Distance(tZeroPrevious, tOnePrevious);
                                float currentTouchDistance = Vector2.Distance(tZeroNormalized, tOneNormalized);

                                // get offset value
                                scrollDistance = oldTouchDistance - currentTouchDistance;
                            }
                            
                            catch
                            {
                                scrollDistance = 0f;
                            }
                        }
#endif
                    }
                    if (setting.invertX)
                        xMovement -= velocityX;
                    else
                        xMovement += velocityX;

                    if (setting.invertY)
                        yMovement += velocityY;
                    else
                        yMovement -= velocityY;

#if UNITY_EDITOR
                    scrollDistance = Input.GetAxis("Mouse ScrollWheel");
#endif
                    yMovement = ClampAngle(yMovement, yMinLimit, yMaxLimit);

                    Quaternion rotation = Quaternion.Euler(yMovement, xMovement, 0);

                    distance = Mathf.Clamp(distance - (-scrollDistance) * zoomSpeed * setting.ZoomSpeed, distanceMin, distanceMax);

                    ////tempDistance = distance;
                    //RaycastHit hit;
                    //if (Physics.Linecast(target.position, transform.position, out hit, collidedLayer))
                    //{
                    //    distance -= hit.distance;
                    //    //tempDistance = distance - hit.distance;
                    //}

                    //distance = Mathf.Clamp(distance, distanceMin, distanceMax);

                    Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);

                    Vector3 position = rotation * negDistance + freelookTarget.transform.position;

                    if(GameManager.instance)
                    {
                        Quaternion lightConeToRotate = Quaternion.Euler
                        (
                            new Vector3
                            (
                                lightCone.eulerAngles.x, rotation.eulerAngles.y, lightCone.eulerAngles.z
                            )
                        );

                        Vector3 lightConePos = GameManager.instance.ActiveHero.transform.position;
                        lightConeParent.position = lightConePos;
                        lightCone.rotation = lightConeToRotate;//Quaternion.Lerp(lightCone.rotation, lightConeToRotate, tempSmooth * Time.deltaTime);
                    }

                    transform.rotation = rotation; // Quaternion.Lerp(transform.rotation, rotation, smoothing * Time.deltaTime);
                    transform.position = position; // Vector3.Lerp(transform.position, position, smoothing * Time.deltaTime);

                    scrollDistance = Mathf.Lerp(scrollDistance, 0, Time.unscaledDeltaTime * setting.Smoothness * baseFreelookSmoothness);
                    velocityX = Mathf.Lerp(velocityX, 0, Time.unscaledDeltaTime * setting.Smoothness * baseFreelookSmoothness);
                    velocityY = Mathf.Lerp(velocityY, 0, Time.unscaledDeltaTime * setting.Smoothness * baseFreelookSmoothness);
                    X = 0;
                    Y = 0;
                }
            }

            else
            {
                Vector3 targetCamPos = target.position - posOffset;
                transform.position = Vector3.Lerp(transform.position, targetCamPos, smoothing * Time.unscaledDeltaTime);

                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(rotOffset), smoothing * Time.unscaledDeltaTime);
            }
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

    private void Start()
    {
        if (GameManager.instance)
            setting = GameManager.instance.userData.settings;

        else
            setting = GlobalUserData.GetUserSetting();
        CloseUpCameraChanged(setting.closeUpCamera);
        isFreeloking = setting.freelookCamera;
        lightConeParent.gameObject.SetActive(setting.freelookCamera);

        MenuOptions.OnChangeCameraSetting += ApplyNewSetting;
        MenuOptions.OnChangeCamera += CloseUpCameraChanged;
    }

    private void OnDestroy()
    {
        MenuOptions.OnChangeCameraSetting -= ApplyNewSetting;
        MenuOptions.OnChangeCamera -= CloseUpCameraChanged;
    }

    void ApplyNewSetting()
    {
        try
        {
            if (GameManager.instance)
                setting = GameManager.instance.userData.settings;

            else
                setting = GlobalUserData.GetUserSetting();
            isFreeloking = setting.freelookCamera;
            lightConeParent.gameObject.SetActive(setting.freelookCamera);
        }
        catch 
        {
        }
    }

    public void ForceToTargetPos()
    {
        Vector3 targetCamPos = target.position - posOffset;
        transform.position = targetCamPos;
    }

    void OnEnable()
    {
        originalPos = cam.localPosition;
    }

    public void SetTarget(Transform _target, bool instant, bool zoomIn, bool disableFreelook)
    {
        target = _target;

        freelookTarget.SetTarget(target.Find("FreelookTarget"));
        if (!freelookTarget.target) freelookTarget.SetTarget(target, new Vector3(0f, 1.7f, 0f));

        if(disableFreelook)
            isFreeloking = false;

        else
        {
            if (GameManager.instance)
                setting = GameManager.instance.userData.settings;

            else
                setting = GlobalUserData.GetUserSetting();
            isFreeloking = setting.freelookCamera;
        }

        if (instant) ForceToTargetPos();
        if (zoomIn) ZoomIn();
    }

    public void SetTarget(Transform _target, bool instant, bool zoomIn)
    {
        SetTarget(_target, instant, zoomIn, false);
    }

    public void SetTarget(Transform _target, bool instant)
    {
        SetTarget(_target, instant, false);
    }

    [Button("Return Cam")]
    public void ReturnToPlayer()
    {
        camPoint = null;

        smoothing = 5;

        if (GameManager.instance.HeroesInCharge.Count == 0) return;
        if (GameManager.instance.ActiveHero == null) return;

        target = GameManager.instance.ActiveHero.transform;
        if (GameManager.instance)
            setting = GameManager.instance.userData.settings;

        else
            setting = GlobalUserData.GetUserSetting();
        isFreeloking = setting.freelookCamera;
        freelookTarget.SetTarget(target.Find("FreelookTarget"));
        if (!freelookTarget.target) freelookTarget.SetTarget(target, new Vector3(0f, 1.7f, 0f));

        ZoomOut();
        
        //temporarySmooth = true;
        //Timing.RunCoroutine(DelayTemporarySmooth(0.75f));
    }

    public void ReturnSlow()
    {
        smoothing = 1;
        target = GameManager.instance.ActiveHero.transform;

        freelookTarget.SetTarget(target.Find("FreelookTarget"));
        if (!freelookTarget.target) freelookTarget.SetTarget(target, new Vector3(0f, 1.7f, 0f));

        var setting = GlobalUserData.GetUserSetting();
        isFreeloking = setting.freelookCamera;
        ZoomOut();

    }

    public void SetTargetSlow(Transform _target)
    {
        target = _target;

        freelookTarget.SetTarget(target.Find("FreelookTarget"));
        if (!freelookTarget.target) freelookTarget.SetTarget(target);

        smoothing = 1;
        touchField.Pressed = false;
        isFreeloking = false;
    }
    public void SetTargetFast(Transform _target)
    {
        target = _target;

        freelookTarget.SetTarget(target.Find("FreelookTarget"));
        if (!freelookTarget.target) freelookTarget.SetTarget(target);

        smoothing = 5;
        touchField.Pressed = false;
        isFreeloking = false;
    }

    bool isZoomed;
    public void ZoomIn()
    {
        if (isZoomed) return;
        if (zoomLv <= interactionZoomLv) return;

        isZoomed = true;
        posOffset = GetPositionOffset(interactionZoomLv);
        SetRotationX(zoomLevels[interactionZoomLv].rotation);
    }
    public void ZoomOut()
    {
        if (!isZoomed) return;
        if (zoomLv <= interactionZoomLv) return;

        isZoomed = false;
        SetZoomLevel();
        //posOffset = defaultOffset;
    }

    //==== SHAKE VARIANTS ====//
    Vector3 originalPos;

    public void MicroShake()
    {
        cam.DOShakePosition(0.1f, 0.05f, 30, 90, false, false).OnComplete(ReturnCam);
    }
    public void TinyShake()
    {
        cam.DOShakePosition(0.1f, 0.15f, 30, 90, false, false).OnComplete(ReturnCam);
    }
    public void LightShake()
    {
        LightShake(0.1f);
    }
    public void LightShake(float duration)
    {
        cam.DOShakePosition(duration, 0.3f, 30, 90, false, false).OnComplete(ReturnCam);
    }
    public void MediumShake()
    {
        MediumShake(0.1f);
    }
    public void MediumShake(float duration)
    {
        cam.DOShakePosition(duration, 0.4f, 35, 45, false, true).OnComplete(ReturnCam);
    }
    public void HeavyShake()
    {
        HeavyShake(0.2f);
    }
    public void HeavyShake(float duration)
    {
        cam.DOShakePosition(duration, 0.5f, 40, 90, false, true).OnComplete(ReturnCam);
    }
    public void HeavierShake()
    {
        cam.DOShakePosition(0.3f, 0.5f, 40, 90, false, true).OnComplete(ReturnCam);
    }
    public void EarthQuake()
    {
        EarthQuake(0.5f);
    }
    public void EarthQuake(float dur)
    {
        cam.DOShakePosition(dur, 0.3f, 35, 45, false, false).OnComplete(ReturnCam);
    }
    public void LightEarthQuake(float dur)
    {
        cam.DOShakePosition(dur, 0.2f, 35, 45, false, false).OnComplete(ReturnCam);
    }
    void ReturnCam()
    {
        cam.localPosition = Vector3.zero;
        cam.localRotation = Quaternion.identity;
    }
    public void CustomShake(float duration, float strength, int vibrate, float randomness)
    {
        cam.DOShakePosition(duration, strength, vibrate, randomness, false, false).OnComplete(ReturnCam);
    }
    public void StopShake()
    {
        cam.DOKill(false);
    }

    public void Shake(ShakeLevel lv)
    {
        switch (lv)
        {
            case ShakeLevel.light: LightShake(); break;
            case ShakeLevel.medium: MediumShake(); break;
            case ShakeLevel.heavy: HeavyShake(); break;
            case ShakeLevel.earthquake: EarthQuake(); break;
            default: break;
        }
    }
    public void Shake(ShakeLevel lv, float duration)
    {
        switch (lv)
        {
            case ShakeLevel.light: LightShake(duration); break;
            case ShakeLevel.medium: MediumShake(duration); break;
            case ShakeLevel.heavy: HeavyShake(duration); break;
            case ShakeLevel.earthquake: EarthQuake(duration); break;
            default: break;
        }
    }


    //==== CAMERA POINT ====//
    private Transform camPoint;
    [Button("Set Cam Point")]
    public void SetCameraPoint(Transform _src)
    {
        camPoint = _src;

        if (_src == null) return;

        freelookTarget.SetTarget(_src.Find("FreelookTarget"));
        if (!freelookTarget.target) freelookTarget.SetTarget(target);

        smoothing = 4;
    }

    public void SetCameraFov(float fov)
    {
        cam.GetComponent<Camera>().DOFieldOfView(fov, 0.5f);
    }
}

public enum ShakeLevel
{
    none, light, medium, heavy, earthquake
}