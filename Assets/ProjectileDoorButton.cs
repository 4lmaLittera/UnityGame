using UnityEngine;

public class ProjectileDoorButton : MonoBehaviour
{
    [SerializeField] private ProjectileDoorController _door;
    [SerializeField] private bool _singleUse = true;
    [SerializeField] private bool _keepWorldAnchored = true;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hitClip;
    [Range(0f, 1f)]
    [SerializeField] private float _hitVolume = 1f;

    [Header("Visual")]
    [SerializeField] private bool _depressVisual = true;
    [SerializeField] private Vector3 _pressedLocalOffset = new Vector3(0f, -0.08f, 0f);

    private bool _triggered;
    private Vector3 _startLocalPos;
    private Vector3 _startWorldPos;
    private Quaternion _startWorldRot;
    private Vector3 _pressedWorldPos;

    private void Awake()
    {
        if (_door == null)
        {
            _door = GetComponentInParent<ProjectileDoorController>();
        }

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
        }

        _startLocalPos = transform.localPosition;
        _startWorldPos = transform.position;
        _startWorldRot = transform.rotation;
        _pressedWorldPos = _startWorldPos + transform.TransformVector(_pressedLocalOffset);
    }

    private void LateUpdate()
    {
        if (!_keepWorldAnchored)
        {
            return;
        }

        transform.position = (_triggered && _depressVisual) ? _pressedWorldPos : _startWorldPos;
        transform.rotation = _startWorldRot;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryActivate(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivate(other);
    }

    private void TryActivate(Collider other)
    {
        if (_triggered && _singleUse)
        {
            return;
        }

        if (!IsValidProjectile(other))
        {
            return;
        }

        if (_door != null)
        {
            _door.OpenDoor();
        }

        PlayHitSfx();

        if (_depressVisual)
        {
            if (_keepWorldAnchored)
            {
                transform.position = _pressedWorldPos;
            }
            else
            {
                transform.localPosition = _startLocalPos + _pressedLocalOffset;
            }
        }

        _triggered = true;
    }

    private void PlayHitSfx()
    {
        if (_audioSource == null || _hitClip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(_hitClip, _hitVolume);
    }

    private static bool IsValidProjectile(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.GetComponent<IDamageSource>() != null)
        {
            return true;
        }

        if (other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<IDamageSource>() != null)
        {
            return true;
        }

        return false;
    }
}
