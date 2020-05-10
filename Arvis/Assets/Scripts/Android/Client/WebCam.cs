﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using OpenCvSharp;
using System.IO;

public class WebCam : MonoBehaviour
{
    private static WebCamTexture _cam;
    private RawImage _display;
    [SerializeField, Header("Virtual Camera")]
    private Camera _virtualCamera;
    [SerializeField, Header("Virtual World(Render texture)")]
    private RenderTexture _vWorld;
    [SerializeField, Header("Virtual Display")]
    private RawImage _vDisplay;

    [SerializeField]
    private Canvas _canvas;
    [SerializeField]
    private Button _buttonDetection;

    // 움직일(터치할) 오브젝트
    [SerializeField, Header("Object to Move")]
    private GameObject _object;
    // 가상 손의 손가락
    [SerializeField, Header("Finger & Center")]
    private GameObject[] _handObject;

    // Resize할 크기
    private const int _width = 16 * 15;
    private const int _height = 9 * 15;

    // 손 인식에 사용될 프레임 이미지
    private Mat _imgFrame;
    private Mat _imgMask;
    private Mat _imgHand;

    private SkinDetector _skinDetector;
    private HandDetector _handDetector;
    private HandManager _handManager;

    public SkinDetector SkinDetector
    {
        set
        {
            _skinDetector = value;
        }
        get
        {
            return _skinDetector;
        }
    }
    public HandDetector HandDetector
    {
        set
        {
            _handDetector = value;
        }
        get
        {
            return _handDetector;
        }
    }

    private int _frame = 0;
    public static bool isAndroid;

    private void Start()
    {
    #if UNITY_EDITOR    // for PC
        isAndroid = false;
    #elif UNITY_ANDROID // for Android
        isAndroid = true;
    #endif

        _display = GetComponent<RawImage>();
        _display.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);

        _vDisplay.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
        _vDisplay.texture = _vWorld;

        if(!isAndroid)
        {
            _display.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        _vWorld.width = Screen.width;
        _vWorld.height = Screen.height;

        // for Updating camera field of view
        _virtualCamera.enabled = false;
        _virtualCamera.enabled = true;

        // 원본 화면 = _cam
        _cam = new WebCamTexture(Screen.width, Screen.height, 60);

        _display.texture = _cam;
        _cam.Play();

        _skinDetector = new SkinDetector();
        _handDetector = new HandDetector();

        // no resize : _cam.width, _cam.height
        // resize : _width, _height
        _handManager = new HandManager(_handObject, _display, _width, _height);
    }

    private void Update()
    {
        // _frame++;
        // if(_frame <= 15)
        // {
        //     return;
        // }

        // // YOLO 수행
        // if(!_handDetector.IsInitialized)
        // {
        //     Client.Connect();

        //     Yolo();
        //     _handDetector.IsInitialized = Client.Receive(_skinDetector.HandBoundary);

        //     Client.Close();
        // }

        // 다시 손 인식 필요
        if(!_handDetector.IsInitialized)
        {
            // 인식 버튼 활성화
            if(!_buttonDetection.gameObject.activeSelf)
                _buttonDetection.gameObject.SetActive(true);

            return;
        }

        _imgFrame = OpenCvSharp.Unity.TextureToMat(_cam);

        Texture2D texture = new Texture2D(_width, _height);
        Cv2.Resize(_imgFrame, _imgFrame, new Size(_width, _height));

        _handDetector.IsCorrectDetection = true;

        // 피부색으로 마스크 이미지를 검출
        _imgMask = _skinDetector.GetSkinMask(_imgFrame);

        // 손의 점들을 얻음
        _imgHand = _handDetector.GetHandLineAndPoint(_imgFrame, _imgMask);

        // 손 인식이 정확하지 않으면 프레임을 업데이트 하지 않음
        // if(!_handDetector.IsCorrectDetection)
        // {
        //     texture = OpenCvSharp.Unity.MatToTexture(_imgHand, texture);
        //     return;
        // }

        // 손가락 끝점을 그림
        _handDetector.DrawFingerPointAtImg(_imgHand);

        // 화면상의 손가락 끝 좌표를 가상세계 좌표로 변환
        _handManager.InputPoint(_handDetector.FingerPoint, _handDetector.Center);

        // 가상 손을 움직임
        _handManager.MoveHand();

        _handDetector.MainPoint.Clear();
        _handManager.Cvt3List.Clear();

        texture = OpenCvSharp.Unity.MatToTexture(_imgHand, texture);
        //_display.texture = texture;
    }

    //private void Yolo()
    //{
    //    Texture2D img = new Texture2D(_cam.width, _cam.height);
    //    img.SetPixels32(_cam.GetPixels32());

    //    byte[] jpg = img.EncodeToJPG();
    //    Debug.Log("jpg " + jpg.Length);

    //    // jpg 전송
    //    Client.Send(BitConverter.GetBytes(jpg.Length));
    //    Client.Send(jpg);
    //}

    private void OnApplicationQuit()
    {
        if(Client.IsConnected)
            Client.Close();
    }
}
