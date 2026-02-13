using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image image;

    private int X { get; set; }
    private int Y { get; set; }

    public event Action<int, int> PointerDown;
    public event Action<int, int> PointerUp;
    
    public void Initialize(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void SetImage(Sprite sprite)
    {
        image.sprite = sprite;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        PointerDown?.Invoke(X, Y);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        PointerUp?.Invoke(X, Y);
    }
}