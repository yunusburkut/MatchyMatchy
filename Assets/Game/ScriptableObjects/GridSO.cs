using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.ScriptableObjects
{
    [CreateAssetMenu(fileName = "GridSO", menuName = "Game/Grid")]
    public class GridSO : ScriptableObject
    {
        public int gridX;
        public int gridY;
        public int tileSize;
        public int tileSpacing;
        public Sprite[] Sprites;
        
    }
}