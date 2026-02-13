using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts
{
    public class TileView : MonoBehaviour
    {
        [SerializeField] private Image image;

        public void SetImage(Sprite sprite)
        {
            image.sprite = sprite; 
        }
    }
}