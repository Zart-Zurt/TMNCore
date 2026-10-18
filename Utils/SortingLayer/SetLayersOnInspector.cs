using System.Linq;
using UnityEngine;

namespace Core.Utils
{
    public class SetLayersOnInspector : MonoBehaviour
    {
        [Header("Layer")] [SerializeField] private bool changeLayer;
        [SerializeField] [SortingLayer] private int sortingLayer;

        [Header("Order")] [SerializeField] private bool changeOrder;
        [SerializeField] private int sortingOrder;

        [ContextMenu("Set All Sprite Layers")]
        private void SetAllSpriteLayers()
        {
            var list = GetComponentsInChildren<SpriteRenderer>(true).ToList();
            foreach (var item in list)
            {
                if (changeLayer)
                {
                    item.SetSortingLayerByIndex(SortingLayer.GetLayerValueFromID(sortingLayer) + 6);
                }

                if (changeOrder)
                {
                    item.SetOrderInLayerByNum(sortingOrder);
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }
    }
}