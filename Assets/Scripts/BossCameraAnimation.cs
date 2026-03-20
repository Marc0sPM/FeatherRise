using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossCameraAnimation : MonoBehaviour
{
    [SerializeField] private float _cameraDistance;
    [SerializeField] private float _movementSpeed;
    [SerializeField] private float _decimals;
    private CinemachineVirtualCamera _myCinemachineVirutalCameraComponent;
    // Start is called before the first frame update
    void Start()
    {
        _myCinemachineVirutalCameraComponent = GetComponent<CinemachineVirtualCamera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_myCinemachineVirutalCameraComponent.m_Lens.OrthographicSize <= _cameraDistance - _decimals)
        {
            _myCinemachineVirutalCameraComponent.m_Lens.OrthographicSize = Mathf.Lerp(_myCinemachineVirutalCameraComponent.m_Lens.OrthographicSize, _cameraDistance, Time.deltaTime * _movementSpeed);
        }
    }
}
