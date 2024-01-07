/*
*Copyright(C) by SWM
*Author: Samuel
*Version: 4.4.6
*UnityVersion：2018.4.23f1
*Date: 2020-11-27 12:02:29
*/
using UnityEngine;

namespace Samuel.Utility
{
    public class FaceToCamera : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] bool _free = false;

        private void Awake ()
        {
            if (_target == null)
                _target = transform;
        }

        // Update is called once per frame
        void Update ()
        {
            if (_free)
            {
                _target.rotation = Quaternion.LookRotation (_target.position - Camera.main.transform.position);
            }
            else
                _target.rotation = Quaternion.LookRotation (Vector3.ProjectOnPlane (_target.position - Camera.main.transform.position, _target.up), _target.up);
        }
    }
}