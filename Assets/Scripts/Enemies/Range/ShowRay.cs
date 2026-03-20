using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowRay : MonoBehaviour
{
    Transform _myTransform;

    [SerializeField]
    private float _range;
    [SerializeField]
    private LineRenderer _lineRenderer;
    [SerializeField]
    private float _rayTime;
    [SerializeField]
    private LayerMask _playerLayer;
    [SerializeField]
    private Transform _playerTransform;
    private bool _stopRay;
    public bool StopRay { get { return _stopRay; }}

    RaycastHit2D _raycastHit2D;

    private void Start()
    {
        _myTransform = transform;
    }
    private void Update()
    {
        _raycastHit2D = Physics2D.Raycast(_myTransform.position, _playerTransform.position, Mathf.Infinity, _playerLayer);
        if (_raycastHit2D)
        {
            
        }
        
    }
    //public void ShootRay()
    //{
    //    _raycastHit2D = Physics2D.Raycast(transform.position, transform.right, _range, _playerLayer);
    //    if (_raycastHit2D )
    //    {
    //        StartCoroutine(LineGenerator(_raycastHit2D.point));
    //    }
    //    else
    //    {
    //        Vector3 NoPoint = new Vector3(_range, 0, 0) * transform.right.x + transform.position;
    //        StartCoroutine(LineGenerator(NoPoint));
    //    }
        
    //}
    public IEnumerator CanRay()
    {
        yield return new WaitForSeconds(0.1f);

        _lineRenderer.enabled = false;
        

        yield return new WaitForSeconds(_rayTime);

        _stopRay = false;
        
    }
    public void Stop()
    {
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, _playerTransform.position);
        _stopRay = true;
        StartCoroutine(CanRay());
    }
}
