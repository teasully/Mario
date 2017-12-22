using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    static bool _KEYBOARD = false;

    public static PlayerController _Player;

    float _yVelocity = -1f, _fVelocity,
        _throwTimer; // Make sure id not just throw .001 seconds ago and regrab
    public float _gravityModifier = 1f;

    Vector2 axisLeft, _axisLeft2;

    bool _airborn, _longJumping, _diving, _summersaultSliding, _hitWall, _holding;

    AudioSource _currentSound;

    ParticleSystem _runTrail;

    Animator _anim;

    Transform _heldItem;

    Rigidbody _holdRB;
    Vector3 _Checkpoint;
    PlayerCube _holdScript, _checkcubeScript;

    MeshRenderer _throwGuess;

    void Start()
    {
        _Player = this;

        _camPos = transform.position - ((transform.position - _camPosDesired).normalized * _camDistance) + Vector3.up * _camHeight;
        _camPosDesired = _camPos;

        _runTrail = transform.GetChild(1).GetComponent<ParticleSystem>();

        _anim = GameObject.Find("man").GetComponent<Animator>();

        _holdRB = GameObject.Find("CheckCube").GetComponent<Rigidbody>();
        _holdRB.sleepThreshold = 0f;

        _Checkpoint = _holdRB.transform.position;
        _checkcubeScript = _holdRB.transform.GetComponent<PlayerCube>();

        _throwGuess = GameObject.Find("Estimate").GetComponent<MeshRenderer>();
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        Vector2 axisRight = Vector2.zero;
        if (_KEYBOARD)
        {
            if (Input.GetKey("up"))
            {
                axisRight.y = 1f;
            }
            if (Input.GetKey("down"))
            {
                axisRight.y = -1f;
            }
            if (Input.GetKey("left"))
            {
                axisRight.x = -1f;
            }
            if (Input.GetKey("right"))
            {
                axisRight.x = 1f;
            }
        }
        else
        {
            axisRight = new Vector2(Input.GetAxis("Horizontal2"), Input.GetAxis("Vertical2"));
        }
        // Handle camera position
        if (Mathf.Abs(axisLeft.x) > 0.15f)
        {
            _camPosDesired += (transform.position + (-transform.forward * 20f) - _camPosDesired) * Time.deltaTime * Mathf.Abs(axisLeft.x) * 0.4f;
        }
        float r2 = (Input.GetAxis("R2") + 1f) / 2f;
        if (_KEYBOARD) r2 = 0f;
        if (r2 > 0.1f)
        {
            _camPosDesired += (transform.position + (-transform.forward * 20f) - _camPosDesired) * Time.deltaTime * 20f;
        }
        // Move camera left and right
        if (Mathf.Abs(axisRight.x) > 0.2f)
        {
            _camPosDesired += axisRight.x * cam.transform.right * Time.deltaTime * 100f;
        }
        // Make sure _camPosDesired stays within certain distance
        _camPosDesired += ((transform.position - (transform.position - _camPosDesired).normalized * _camDistance) - _camPosDesired) * Time.deltaTime * 5f;
        // Handle camera height
        if (Mathf.Abs(axisRight.y) > 0.2f)
        {
            _camHeight += axisRight.y * Time.deltaTime * 45f;
            _camHeight = Mathf.Clamp(_camHeight, -5f, 25f);
        }
        // Interpolate camera for smoothness
        Vector3 xz = (transform.position - _camPosDesired);
        xz.y = 0f;
        _camPos = _anim.transform.position - xz.normalized * _camDistance + Vector3.up * _camHeight;
        cam.transform.position += (_camPos - cam.transform.position) * Time.deltaTime * 5f;
        if (cam.transform.position.y < 0f)
        {
            cam.transform.position = new Vector3(cam.transform.position.x, 0f, cam.transform.position.z);
        }
        // cam.transform.position += (new Vector3(transform.position.x, transform.position.y + 10f, cam.transform.position.z) - cam.transform.position) * Time.deltaTime * 5f;
        // Move camera look with player
        Vector3 forw = cam.transform.forward;
        forw.y = 0f;
        _lookPosDesired = transform.position + transform.forward * 5f;//Vector3.Lerp(transform.position, _holdRB.position, 0.25f);// + cam.transform.right * _axisLeft2.x * 8f + forw.normalized * _axisLeft2.y * 15f + transform.up * Mathf.Clamp(_yVelocity, -1.5f, 3f) * 5f;
        Debug.DrawLine(transform.position, _lookPosDesired);
        _lookPos += (_lookPosDesired - _lookPos) * Time.deltaTime * 4f;
        cam.transform.LookAt(_lookPos);
        // Check particles
        if ((_fVelocity > 0.3f && !_airborn) || (_diving && !_airborn) || (_summersaultSliding && !_airborn))
        {
            if (!_runTrail.isEmitting)
            {
                _runTrail.Play();
            }
        }
        else if (_runTrail.isEmitting)
        {
            _runTrail.Stop();
        }

        _anim.transform.position += -(_anim.transform.position - (transform.position - Vector3.up)) * Time.deltaTime * 20f;
        Quaternion r = transform.rotation;
        r.eulerAngles = new Vector3(_anim.transform.eulerAngles.x, r.eulerAngles.y + 90f, r.eulerAngles.z);
        _anim.transform.rotation = Quaternion.Lerp(_anim.transform.rotation, r, Time.deltaTime * (_summersaultSliding || _airborn ? 6f : 100f));
        /*
        Vector3 pp = ((transform.forward * 600f * Mathf.Clamp(_fVelocity, 0.05f, 1f) * (_airborn ? 2f : 3.5f)) + new Vector3(0f, (_airborn ? 3f : 1.5f), 0f) * 220f).normalized * _fVelocity * 35f;
        LineRenderer rr = GameObject.Find("Line").GetComponent<LineRenderer>();
        rr.SetPositions(Plot(_holdRB, transform.position, pp, 30));*/
    }

    float _lookIter, _camDistance = 10f, _camHeight = 8f, _airTime, _maxClamp, _diveTimer;
    float _xTilt;
    Vector3 _lookPos, _lookPosDesired, _camPos, _camPosDesired, _lastPos;

    public void Step()
    {
        if (_airborn) return;
        PlaySound(10, false);
    }

    AnimState _animstate;

    enum AnimState
    {
        JUMP0,
        RUN,
        IDLE,
        HANGING,
        DIVING,
        WALLSLIDE,
        SLIDING
    }

    float _holdTimer = 1f;

    // Update is called once per frame
    void Update()
    {
        // Set animation variables in anim
        _anim.SetFloat("movespeed", _fVelocity);
        _anim.SetFloat("runmod", 1f);
        _anim.SetFloat("gravity", _yVelocity);
        _anim.SetBool("airborn", _airborn);

        // Check if holding; orient and check throw
        _holdRB.useGravity = true;
        _holdRB.drag = 0.1f;
        if (_holding)
        {
            _heldItem.localRotation = Quaternion.identity;
            _heldItem.localPosition = new Vector3(0f, 1.971f + _heldItem.GetComponent<PlayerCube>().GetSize() / 2f, 0f);
            if (Input.GetKey(KeyCode.E) || Input.GetButton("Square"))
            {
                _holdTimer += Time.deltaTime * 8f;
            }
            if (Input.GetKeyUp(KeyCode.E) || Input.GetButtonUp("Square"))
            {
                Throw();
                _holdTimer = 1f;
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Square"))
            {
                _checkcubeScript.Press();
            }
        }

        if (_diving)
        {
            _anim.SetBool("run", false);
            if (_animstate != AnimState.DIVING)
            {
                _animstate = AnimState.DIVING;
                _anim.SetTrigger("dive");
            }
        }
        else if (_summersaultSliding)
        {
            if (_animstate != AnimState.SLIDING)
            {
                _animstate = AnimState.SLIDING;
                _anim.SetTrigger("slide");
            }
        }
        else if (!_airborn && _fVelocity > 0.001f)
        {
            if (_animstate != AnimState.RUN)
            {
                _anim.SetTrigger("run");
                _animstate = AnimState.RUN;
            }
        }
        else if (!_airborn)
        {
            if (_animstate != AnimState.IDLE)
            {
                _anim.SetTrigger("idle");
                _animstate = AnimState.IDLE;
            }
        }
        else
        {
            _anim.SetBool("run", false);
            _anim.Play("Jump0", 0, 1f - (Mathf.Clamp(_yVelocity, -2.5f, 2.3f) + 2.4f) / 8f);
        }
        // Check if below map
        float minY = 0f;
        if (transform.position.y < minY)
        {
            if (_holding)
            {
                transform.position = _Checkpoint + new Vector3(0f, 5f, 0f);
            }
            else
            {
                transform.position = _holdRB.position + new Vector3(0f, 3f, 0f);
                _fVelocity = 0f;
            }
        }
        // Check if below map
        if (_holdRB.position.y < minY)
        {
            _holdRB.position = _Checkpoint + new Vector3(0f, 10f, 0f);
            _holdRB.velocity = Vector3.zero;
        }
        // Tilt
        if (Mathf.Abs(axisLeft.x) > 0.1f && !_airborn && !_summersaultSliding)
        {
            _xTilt += (20f * axisLeft.x - _xTilt) * Time.deltaTime * 3f;
        }
        else
        {
            _xTilt += (-_xTilt) * Time.deltaTime * 3f;
        }
        Quaternion q = _anim.transform.localRotation;
        Vector3 e = q.eulerAngles;
        e.x = _xTilt;
        q.eulerAngles = e;
        _anim.transform.localRotation = q;
        // Check if hit a wall: fall until ground
        if (_hitWall)
        {
            Gravity();
            MovePosition(-transform.forward * 5f);
            if (!_airborn)
            {
                _hitWall = false;
            }
            return;
        }
        axisLeft = Vector2.zero;
        if (_KEYBOARD)
        {
            if (Input.GetKey("w"))
            {
                axisLeft.y = 1f;
            }
            if (Input.GetKey("s"))
            {
                axisLeft.y = -1f;
            }
            if (Input.GetKey("a"))
            {
                axisLeft.x = -1f;
            }
            if (Input.GetKey("d"))
            {
                axisLeft.x = 1f;
            }
        }
        else axisLeft = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (axisLeft.magnitude < 0.1f) axisLeft = Vector2.zero;
        _axisLeft2 += (axisLeft - _axisLeft2) * Time.deltaTime * 2f;
        // Check if pushing stick in opposite dir
        Vector3 fo = transform.forward,
            mu = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f) * new Vector3(axisLeft.x, 0f, axisLeft.y).normalized;
        bool oppositeStick = false;
        bool onSlippery = false;
        if ((fo - mu).magnitude > 1.7f)
        {
            oppositeStick = true;
        }
        // Lerp _maxClamp: used to clamp _fVelocity
        float maxF = 1f;
        if (axisLeft.magnitude < 0.6f && !_airborn)
        {
            if (axisLeft.magnitude < 0.3f)
            {
                _maxClamp += (0.15f - _maxClamp) * Time.deltaTime * 3f;
            }
            else
            {
                _maxClamp += (0.3f - _maxClamp) * Time.deltaTime * 3f;
            }
        }
        else
        {
            maxF = 1.25f;
            _maxClamp += (maxF - _maxClamp) * Time.deltaTime * 3f;
        }
        // If diving, only slow down on floor
        if (_diving)
        {
            if (!_airborn) _fVelocity = Mathf.Clamp(_fVelocity - Time.deltaTime * (oppositeStick ? 2f : 0.5f) + Time.deltaTime * (onSlippery ? 4.5f : 0f), 0f, 10f);
            if (axisLeft.magnitude > 0f && !_airborn && !oppositeStick)
            {
                Camera cam = Camera.main;
                Vector3 lookPos = (cam.transform.forward * axisLeft.y + cam.transform.right * axisLeft.x);
                lookPos.y = 0f;
                Quaternion targetRotation = Quaternion.LookRotation(lookPos);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f * Time.deltaTime * _fVelocity);
            }
        }
        else if (_summersaultSliding)
        {
            Gravity();
            _fVelocity = Mathf.Clamp(_fVelocity - Time.deltaTime * 2f, 0f, 5f);
            MovePosition(transform.forward * 15f * _fVelocity);
            GetSound(11).pitch = 0.9f;
            if (_fVelocity < 0.1f || _airborn)
            {
                _summersaultSliding = false;
                if (!_airborn)
                {
                    transform.Rotate(new Vector3(0f, 1f, 0f), 180f);
                }
                StopSound(11);
            }
            return;
        }
        // Else, speed up if input
        else if (axisLeft.magnitude != 0f)
        {
            if (oppositeStick)
            {
                if (!_airborn && _fVelocity > 0.2f && Time.time - _diveTimer > 0.1f)
                {
                    _summersaultSliding = true;
                    PlaySound(11, false);
                }
                else _fVelocity = Mathf.Clamp(_fVelocity - Time.deltaTime * (_airborn ? 0.75f : 2f), 0f, 5f);
            }
            else if (_fVelocity <= _maxClamp)
            {
                _fVelocity = Mathf.Clamp(_fVelocity + Time.deltaTime * (_airborn ? 0.1f : 1f), 0f, (_groundSlope != 0f ? 5f : _maxClamp));
            }
            else if (!_airborn)
            {
                _fVelocity = Mathf.Clamp(_fVelocity - Time.deltaTime * 4f, 0f, 5f);
            }
        }
        // Else slow down normally
        else if (!_airborn)
        {
            _fVelocity = Mathf.Clamp(_fVelocity - Time.deltaTime * 4f, 0f, 5f);
        }
        // Long jumping
        if (_longJumping)
        {
            Gravity();
            Vector3 addPos = Camera.main.transform.forward * _axisLeft2.y + Camera.main.transform.right * _axisLeft2.x;
            addPos.y = 0f;
            MovePosition(transform.forward * 20f * Mathf.Clamp(_fVelocity, 0f, 10f) + addPos * 10f * _fVelocity);
            if (!_airborn)
            {
                _longJumping = false;
            }
            return;
        }
        // Diving
        else if (_diving)
        {
            Gravity();
            MovePosition(transform.forward * 20f * _fVelocity);
            GetSound(11).pitch = (0.7f + _fVelocity) * (_airborn ? 0f : 1f);
            // Check for jump after dive
            if (!_airborn && _fVelocity > 0.2f && (Input.GetButtonDown("X") || Input.GetKeyDown(KeyCode.Space)))
            {
                StopDiving();
                if (axisLeft.magnitude > 0.1f && !oppositeStick)
                {
                    _fVelocity = 1.25f;
                    Jump0();
                }
                else
                {
                    _fVelocity = 0.4f;
                    Jump0(1.5f);
                }
            }
            if (!_airborn && _fVelocity <= 0.1f)
            {
                StopDiving();
            }
            return;
        }
        // Rotate player with left stick
        if (axisLeft.magnitude > 0f && !_airborn)
        {
            Camera cam = Camera.main;
            Vector3 lookPos = (cam.transform.forward * axisLeft.y + cam.transform.right * axisLeft.x);
            lookPos.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(lookPos);
            if (axisLeft.magnitude < 0.11f && _fVelocity < 0.1f)
                transform.rotation = targetRotation;
            else
            {
                Quaternion rot = transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
                // Calculate amount rotated and slow player
                float rotAmount = Mathf.Abs(rot.eulerAngles.y - transform.rotation.eulerAngles.y);
                if (!_airborn && rotAmount > 1f && _fVelocity > 0.9f)
                {
                    _fVelocity -= Time.deltaTime * Mathf.Clamp(rotAmount, 1f, 4f) / 3f;
                }
            }
        }
        // Move player
        float mag = axisLeft.magnitude;
        if (mag == 0 && _fVelocity != 0f)
        {
            mag = 0.5f;
        }
        // If in air, do not give complete control
        if (_airborn)
        {
            Vector3 addPos = Camera.main.transform.forward * _axisLeft2.y + Camera.main.transform.right * _axisLeft2.x;
            addPos.y = 0f;
            MovePosition((_fVelocity > 0.2f ? transform.forward * 10f * _fVelocity + addPos * 10f * _fVelocity : addPos * 20f * (_fVelocity + 0.4f)));
        }
        else
        {
            MovePosition(transform.forward * 15f * _fVelocity);
        }
        // Check jump button
        if ((Input.GetButtonDown("X") || Input.GetKeyDown(KeyCode.Space)))
        {
            // First jump
            if (!_airborn)
            {
                // Check for long jump
                if (Input.GetButton("L1") || Input.GetKey(KeyCode.LeftShift))
                {
                    // If moving, long jump
                    if (_fVelocity > 0.5f)
                    {
                        LongJump();
                    }
                }
                // First jump
                else
                {
                    Jump0();
                }
            }
        }
        // Dive / throw
        if (Input.GetButtonDown("Circle") || Input.GetKeyDown("v"))
        {
            if (!_holding && _airborn)
            {
                Dive();
            }
        }
        // Apply gravity
        Gravity();

        Debug.DrawRay(transform.position, transform.forward * 50f);
    }

    void Throw()
    {
        _holding = false;
        _anim.SetTrigger("ToggleHold");
        _anim.SetLayerWeight(1, 0f);
        _heldItem.parent = GameObject.Find("Game").transform;
        // Add forces
        Rigidbody r = _heldItem.GetComponent<Rigidbody>();
        r.isKinematic = false;
        _holdTimer = Mathf.Clamp(_holdTimer, 0.0f, 10f);
        // Check if target is in front or nah
        if (_cs.Count > 0)
        {
            Vector3 point;
            if (_cs.Count == 1)
            {
                point = _cs[0].transform.position;
            }
            else
            {
                float cp = Mathf.Infinity;
                int iter = 0;
                for (int i = 0; i < _cs.Count; i++)
                {
                    float dis = Vector3.Distance(_cs[i].transform.position, transform.position);
                    if (dis < cp)
                    {
                        cp = dis;
                        iter = i;
                    }
                }
                point = _cs[iter].transform.position;
            }
            _throwGuess.transform.position = point;

            float angle = (_airborn ? 30f : 10f);//(Vector3.Angle(transform.position, point) + 5f);
            Vector3 vel = new Vector3(float.NaN, float.NaN, float.NaN);
            while(Vector3.Equals(vel, new Vector3(float.NaN, float.NaN, float.NaN)))
            {
                vel = GetVelocity(point, angle);
                angle += 5f;
            }
            /*if (vel.magnitude > 50f)
            {
                vel = vel.normalized * 50;
            } else if (vel.magnitude < 40f && !_airborn && point.y < transform.position.y - 2f)
            {
                vel = vel.normalized * 40f;
            }*/
            if(!_airborn)vel = vel.normalized * _holdTimer * 5f;
            Vector2 v1 = new Vector2(transform.position.x, transform.position.z),
                v2 = new Vector2(point.x, point.z);
            Debug.Log(vel.magnitude + " : " + (Mathf.Atan2(v2.y, v2.x) - Mathf.Atan2(v1.y, v1.x)));
            r.velocity = vel;
        }
        else
        {
            r.velocity = transform.GetComponent<Rigidbody>().velocity;
            r.AddForce(((transform.forward * 600f * (_airborn ? 2f : 3.5f)) + new Vector3(0f, (_airborn ? 3f : 1.5f), 0f) * 300f).normalized * _holdTimer * 250f);
        }
        r.AddTorque(new Vector3(transform.forward.z, 0f, -transform.forward.x) * 100f);
        _heldItem.gameObject.layer = 0;
        // Fire script
        PlayerCube script = _heldItem.GetComponent<PlayerCube>();
        if (script != null)
        {
            script.OnThrow();
        }
        // Remove held item
        _heldItem = null;
        _throwTimer = Time.time;
    }

    bool Hold(GameObject holdObject)
    {
        PlayerCube script = holdObject.GetComponent<PlayerCube>();
        if (script != null && !_holding && !_diving && Time.time - _throwTimer > 0.2f)
        {
            if (!script.OnHold()) return false;
            _holding = true;
            _anim.SetTrigger("ToggleHold");
            _anim.SetLayerWeight(1, 1f);
            _heldItem = holdObject.transform;
            _heldItem.rotation = Quaternion.identity;
            _heldItem.parent = _anim.transform;
            _heldItem.localPosition = new Vector3(0f, 1.971f + script.GetSize() / 2f, 0f);
            _heldItem.GetComponent<Rigidbody>().isKinematic = true;
            _heldItem.gameObject.layer = 2;
            PlaySound(14, false);
            return true;
        }
        return false;
    }

    void Jump0(float yVal = 1.25f)
    {
        PlaySound(0);
        _yVelocity = yVal * JumpAttributes._JUMP_MODIFIER;

        JumpAttributes._holdTimer = JumpAttributes._HOLDTIMER_SET;
    }

    void LongJump()
    {
        _yVelocity = JumpAttributes._LONGJUMP_MODIFIER;
        _longJumping = true;
        _fVelocity = 1f;
        PlaySound(4);
    }

    void Dive()
    {
        _diving = true;
        _fVelocity = 1.25f;
        _yVelocity = 0.8f;
        PlaySound(5);   // Voice
        PlaySound(11, false);  // Slide noise
    }

    void StopDiving()
    {
        if (!_diving) return;
        _diving = false;
        _diveTimer = Time.time;
        StopSound(11);
    }

    static class JumpAttributes
    {
        // Used to decrease player's y vel
        public static float _GRAVITY_MODIFIER = 10f,
            // Used for jumping
            _JUMP_MODIFIER = 1.5f,
            // Used for holding down jump button
            _JUMP_HOLD_MODIFIER = 7f,
            _holdTimer, _HOLDTIMER_SET = 0.25f,
            // Used for longjumping
            _LONGJUMP_MODIFIER = 1.1f,
            _LONGJUMP_HOLD_MODIFIER = 7.6f,
            // Diving
            _DIVING_HOLD_MODIFIER = 5f;
    }

    void Gravity()
    {
        JumpAttributes._holdTimer -= Time.deltaTime;
        if ((_airborn && _yVelocity > 0.2f && (Input.GetButton("X") || Input.GetKey(KeyCode.Space)) && !_diving && !_longJumping) && JumpAttributes._holdTimer > 0f)
        {
            _yVelocity = Mathf.Clamp(_yVelocity + Time.deltaTime * JumpAttributes._JUMP_HOLD_MODIFIER, -4f, 10f);
        }
        else if (_airborn)
        {
            float mod = 0f;
            if(_longJumping) mod = JumpAttributes._LONGJUMP_HOLD_MODIFIER;
            else if (_diving) mod = JumpAttributes._DIVING_HOLD_MODIFIER;
            _yVelocity = Mathf.Clamp(_yVelocity + Time.deltaTime *mod, -4f, 10f);
        }
        // Apply gravity; slowly reduce yVelocity until negative
        _yVelocity = Mathf.Clamp(_yVelocity - Time.deltaTime * JumpAttributes._GRAVITY_MODIFIER * _gravityModifier, -4f, 10f);
        // If has velocity, add
        if (_yVelocity > 0f)
        {
            RaycastHit h;
            if (Physics.Raycast(transform.position + new Vector3(0f, 0.75f, 0f), transform.up, out h))
            {
                // Check for hold and distance
                if (h.distance <= 0.25f && !Hold(h.collider.gameObject))
                {
                    _yVelocity = -0.25f;
                }
            }
            transform.position += new Vector3(0f, 1f, 0f) * Time.deltaTime * _yVelocity * 10f;
            if (!_airborn)
            {
                _airborn = true;
                transform.parent = GameObject.Find("Game").transform;
            }
        }
        // Else, check for floor below
        else
        {
            RaycastHit h1, h2;
            Physics.Raycast(transform.position - Vector3.up / 2f + transform.forward * 0.2f, -transform.up, out h1);
            Physics.Raycast(transform.position - Vector3.up / 2f - transform.forward * 0.2f, -transform.up, out h2);
            if (h1.collider != null || h2.collider != null)
            {
                _groundSlope = h1.point.y - h2.point.y;
                if(Mathf.Abs(_groundSlope) > 0.75f)
                {
                    _groundSlope = 0f;
                }
                Debug.Log(_groundSlope + " : " + _fVelocity);

                bool uh1 = false;
                if (h1.distance < 0.9f && h1.collider != null)
                {
                    uh1 = true;
                }
                if ((h1.distance <= 0.9f && h1.collider != null) || (h2.distance <= 0.9f && h2.collider != null))
                {
                    _yVelocity = 0f;
                    transform.position = new Vector3(transform.position.x, (uh1 ? h1.point.y : h2.point.y) + 1f, transform.position.z);
                    if (_airborn)
                    {
                        if (h1.collider != null && h1.collider.transform.parent.name.Equals("MPlatform"))
                        {
                            transform.parent = h1.collider.transform.parent;
                            h1.collider.transform.parent.parent.GetComponent<PlatformScript>().OnLand();
                        }
                        else if (h2.collider != null && h2.collider.transform.parent.name.Equals("MPlatform"))
                        {
                            transform.parent = h2.collider.transform.parent;
                            h2.collider.transform.parent.parent.GetComponent<PlatformScript>().OnLand();
                        }
                        else
                        {
                            transform.parent = GameObject.Find("Game").transform;
                        }
                        // Set airborn
                        _airborn = false;
                    }
                    // If not in air, check ground for objects
                    else
                    {
                        if (h1.collider != null)
                        {
                            CheckFloor(h1.collider);
                        }
                        else if (h2.collider != null)
                        {
                            CheckFloor(h2.collider);
                        }
                    }
                }
                else
                {
                    transform.position += new Vector3(0f, 1f, 0f) * Time.deltaTime * _yVelocity * 10f;
                    if (!_airborn)
                    {
                        transform.parent = GameObject.Find("Game").transform;
                        _airborn = true;
                    }
                }
            }
            else
            {
                transform.position += new Vector3(0f, 1f, 0f) * Time.deltaTime * _yVelocity * 10f;
                if (!_airborn)
                {
                    transform.parent = GameObject.Find("Game").transform;
                    _airborn = true;
                }
            }
        }
    }

    float ti = 0f, _groundSlope;
    void CheckFloor(Collider c)
    {
        switch (c.name)
        {
            case ("Summoner"):
                _Checkpoint = c.transform.position;
                ti += Time.deltaTime;

                _holdRB.AddTorque(_holdRB.velocity * 20f);
                float forceMod = 1f;
                if ((Vector3.Distance(_holdRB.position, c.transform.position) < 30f))
                {
                    _holdRB.drag = 5f;
                    forceMod = 0.5f;
                }

                if (Vector2.Distance(new Vector2(_holdRB.position.x, _holdRB.position.z), new Vector2(c.transform.position.x, c.transform.position.z)) > 10f)
                {
                    if (ti > 1f)
                    {
                        _holdRB.AddForce(new Vector3(0f, 1f, 0f) * 1000f);
                        ti = 0f;
                    }
                }

                _holdRB.AddForce((c.transform.position - _holdRB.position + new Vector3(0f, 4f, 0f)).normalized * 150f * forceMod);
                _holdRB.useGravity = false;
                break;
            case ("Tramp"):
                _yVelocity = 6f;
                break;
            default:
                ti = 0f;
                break;
        }
    }

    void MovePosition(Vector3 movePos)
    {
        // Change velocity based on slope
        if (!_airborn && _groundSlope != 0f) {
            float c = -_groundSlope * Time.deltaTime * 10f;
            if (c > 0f)
            {
                c *= (_diving ? 1.75f : 2.25f);
            }
            _fVelocity = Mathf.Clamp(_fVelocity + c, 0f, 2f);
        }
        // Cast rays in x dir
        RaycastHit h1;
        float dist = 0.6f;
        if (Physics.Raycast(transform.position - transform.up * 0.9f, new Vector3(movePos.x, 0f, 0f), out h1) || Physics.Raycast(transform.position + Vector3.up, new Vector3(movePos.x, 0f, 0f), out h1))
        {
            if (h1.distance < dist)
            {
                // Check for hold
                Hold(h1.collider.gameObject);

                _longJumping = false;
                if (h1.normal.y == 0f)
                {
                    movePos.x = 0f;
                    float sign = -Mathf.Sign(h1.point.x - transform.position.x);
                    transform.position = new Vector3(h1.point.x + (dist - 0.05f) * sign, transform.position.y, transform.position.z);
                    // If moving too fast or diving/longjumping, hit wall
                    if ((_fVelocity > 0.2f) && (_diving || _longJumping || (_fVelocity > 1.3f && !_airborn)))
                    {
                        if (h1.point.y < transform.position.y && _diving)
                        {
                            _fVelocity = 0f;
                            _yVelocity = 1f;
                            StopDiving();
                            PlaySound(6);
                            _hitWall = true;
                        }
                    }
                    else
                    {
                        _fVelocity /= 1.05f;
                    }
                }
            }
        }
        // Cast rays in z dir
        if (Physics.Raycast(transform.position - transform.up * 0.9f, new Vector3(0f, 0f, movePos.z), out h1) || Physics.Raycast(transform.position + Vector3.up, new Vector3(0f, 0f, movePos.z), out h1))
        {
            if (h1.distance < dist)
            {
                // Check for hold
                Hold(h1.collider.gameObject);

                _longJumping = false;
                if (h1.normal.y == 0f)
                {
                    movePos.z = 0f;
                    float sign = -Mathf.Sign(h1.point.z - transform.position.z);
                    transform.position = new Vector3(transform.position.x, transform.position.y, h1.point.z + (dist - 0.05f) * sign);
                    // If moving too fast or diving/longjumping, hit wall
                    if ((_fVelocity > 0.2f) && (_diving || _longJumping || (_fVelocity > 1.3f && !_airborn)))
                    {
                        if (h1.point.y < transform.position.y && _diving)
                        {
                            _fVelocity = 0f;
                            _yVelocity = 1f;
                            StopDiving();
                            PlaySound(6);
                            _hitWall = true;
                        }
                    }
                    else
                    {
                        _fVelocity -= 1f * Time.deltaTime;
                    }
                }
            }
        }
        // Move transform based on rays
        transform.position += movePos * Time.deltaTime;
    }

    void PlaySound(int index, bool mainSound = true)
    {
        if (mainSound)
        {
            if (_currentSound != null) _currentSound.Stop();
            _currentSound = transform.GetChild(0).GetChild(index).GetComponent<AudioSource>();
            _currentSound.Play();
            return;
        }
        AudioSource s = transform.GetChild(0).GetChild(index).GetComponent<AudioSource>();
        s.pitch = 0.85f + Random.value * 0.3f;
        s.Play();
    }

    void StopSound(int index)
    {
        transform.GetChild(0).GetChild(index).GetComponent<AudioSource>().Stop();
    }

    AudioSource GetSound(int index)
    {
        return transform.GetChild(0).GetChild(index).GetComponent<AudioSource>();
    }

    bool SoundPlaying(int index)
    {
        return transform.GetChild(0).GetChild(index).GetComponent<AudioSource>().isPlaying;
    }

    public static Vector3[] Plot(Rigidbody rigidbody, Vector3 pos, Vector3 velocity, int steps)
    {
        Vector3[] results = new Vector3[steps];

        float timestep = Time.fixedDeltaTime / Physics.solverVelocityIterationCount;
        Vector3 gravityAccel = Physics.gravity * timestep * timestep;
        float drag = 1f - timestep * rigidbody.drag;
        Vector3 moveStep = velocity * timestep;

        for (int i = 0; i < steps; ++i)
        {
            moveStep += gravityAccel;
            moveStep *= drag;
            pos += moveStep;
            results[i] = pos;
        }

        return results;
    }

    List<Collider> _cs = new List<Collider>();
    private void OnTriggerStay(Collider other)
    {
        if(other.name.Substring(0, 4).Equals("Cube"))
        {
            foreach(Collider c in _cs)
            {
                if (c.GetInstanceID() == other.GetInstanceID()) return;
            }
            _cs.Add(other);
            other.GetComponent<MeshRenderer>().material.color = Color.red;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name.Substring(0, 4).Equals("Cube"))
        {
            if (_cs.Remove(other))
                other.GetComponent<MeshRenderer>().material.color = Color.white;
        }
    }

    Vector3 GetVelocity(Vector3 point, float initialAngle = 45f)
    {
        float gravity = Physics.gravity.magnitude;
        // Selected angle in radians
        float angle = initialAngle * Mathf.Deg2Rad;

        // Positions of this object and the target on the same plane
        Vector3 planarTarget = new Vector3(point.x, 0, point.z);
        Vector3 planarPostion = new Vector3(_holdRB.position.x, 0, _holdRB.position.z);

        // Planar distance between objects
        float distance = Vector3.Distance(planarTarget, planarPostion);
        // Distance along the y axis between objects
        float yOffset = _holdRB.position.y - point.y;

        float initialVelocity = (1 / Mathf.Cos(angle)) * Mathf.Sqrt((0.5f * gravity * Mathf.Pow(distance, 2)) / (distance * Mathf.Tan(angle) + yOffset));

        Vector3 velocity = new Vector3(0, initialVelocity * Mathf.Sin(angle), initialVelocity * Mathf.Cos(angle));

        // Rotate our velocity to match the direction between the two objects
        float angleBetweenObjects = Vector3.Angle(Vector3.forward, planarTarget - planarPostion) * (point.x > transform.position.x ? 1 : -1);
        Vector3 finalVelocity = Quaternion.AngleAxis(angleBetweenObjects, Vector3.up) * velocity;

        return finalVelocity;
    }
}
 