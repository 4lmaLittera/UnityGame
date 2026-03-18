using UnityEngine;

public class ProjectileDoorController : MonoBehaviour
{
    [Header("Door Motion")]
    [SerializeField] private Transform _doorTarget;
    [SerializeField] private Vector3 _openLocalOffset = new Vector3(0f, 3f, 0f);
    [SerializeField] private float _openSpeed = 4f;

    [Header("Behavior")]
    [SerializeField] private bool _closeAutomatically = false;
    [SerializeField] private float _closeDelay = 5f;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _openClip;
    [SerializeField] private AudioClip _closeClip;
    [Range(0f, 1f)]
    [SerializeField] private float _openVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float _closeVolume = 1f;

    private Vector3 _closedLocalPosition;
    private Vector3 _openLocalPosition;
    private bool _isOpen;
    private float _closeAtTime;

    private void Awake()
    {
        if (_doorTarget == null)
        {
            _doorTarget = transform;
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

        _closedLocalPosition = _doorTarget.localPosition;
        _openLocalPosition = _closedLocalPosition + _openLocalOffset;
    }

    private void Update()
    {
        Vector3 target = _isOpen ? _openLocalPosition : _closedLocalPosition;
        _doorTarget.localPosition = Vector3.MoveTowards(_doorTarget.localPosition, target, _openSpeed * Time.deltaTime);

        if (_closeAutomatically && _isOpen && Time.time >= _closeAtTime)
        {
            CloseDoor();
        }
    }

    public void OpenDoor()
    {
        if (_isOpen)
        {
            if (_closeAutomatically)
            {
                _closeAtTime = Time.time + Mathf.Max(0f, _closeDelay);
            }
            return;
        }

        _isOpen = true;
        PlaySfx(_openClip, _openVolume);

        if (_closeAutomatically)
        {
            _closeAtTime = Time.time + Mathf.Max(0f, _closeDelay);
        }
    }

    public void CloseDoor()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        PlaySfx(_closeClip, _closeVolume);
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (_audioSource == null || clip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(clip, volume);
    }
}
