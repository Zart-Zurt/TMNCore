using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Utils
{
    public static class EasyMethods
    {
        public static bool IsEqualV3(Vector3 v1, Vector3 v2, float minDistance)
        {
            return Mathf.Abs(v1.x - v2.x) < minDistance + 0.1f &&
                   Mathf.Abs(v1.z - v2.z) < minDistance + 0.1f;
        }

        public static T FindClosestObject<T>(string tag, Vector3 pos) where T : class
        {
            var allObjects = GameObject.FindGameObjectsWithTag(tag);
            GameObject closest = null;
            var distance = Mathf.Infinity;
            foreach (var go in allObjects)
            {
                var curDistance = (go.transform.position - pos).sqrMagnitude;
                if (curDistance < distance)
                {
                    closest = go;
                    distance = curDistance;
                }
            }

            if (closest == null)
            {
                return null;
            }

            if (typeof(T) == typeof(GameObject))
            {
                return closest as T;
            }

            if (typeof(T) == typeof(Transform))
            {
                return closest.transform as T;
            }

            return closest.GetComponent<T>();
        }

        public static Transform FindClosestObject(List<GameObject> allObjects, Vector3 pos)
        {
            Transform closest = null;
            var distance = Mathf.Infinity;
            foreach (var go in allObjects)
            {
                var curDistance = (go.transform.position - pos).sqrMagnitude;
                if (curDistance < distance)
                {
                    closest = go.transform;
                    distance = curDistance;
                }
            }

            return closest;
        }

        public static Transform FindClosestObject(List<Transform> allObjects, Vector3 pos)
        {
            Transform closest = null;
            var distance = Mathf.Infinity;
            foreach (var go in allObjects)
            {
                var curDistance = (go.position - pos).sqrMagnitude;
                if (curDistance < distance)
                {
                    closest = go;
                    distance = curDistance;
                }
            }

            return closest;
        }

        public static GameObject FindClosestObjectWithRange(string tag, Vector3 pos, float range)
        {
            var allObjects = GameObject.FindGameObjectsWithTag(tag);
            GameObject closest = null;
            var distance = range * range;
            foreach (var go in allObjects)
            {
                var curDistance = (go.transform.position - pos).sqrMagnitude;
                if (curDistance <= distance)
                {
                    closest = go;
                    distance = curDistance;
                }
            }

            return closest;
        }

        public static void CloseOpenAllList(List<GameObject> list, bool situation)
        {
            if (list == null)
            {
                return;
            }

            foreach (var item in list)
            {
                if (item != null)
                {
                    item.SetActive(situation);
                }
            }
        }

        public static void EnableAllList(List<MeshRenderer> list, bool situation)
        {
            if (list == null)
            {
                return;
            }

            foreach (var item in list)
            {
                if (item != null)
                {
                    item.enabled = situation;
                }
            }
        }

        public static void EnableAllList(List<SkinnedMeshRenderer> list, bool situation)
        {
            if (list == null)
            {
                return;
            }

            foreach (var item in list)
            {
                if (item != null)
                {
                    item.enabled = situation;
                }
            }
        }

        public static void EnableAllList(List<Collider> list, bool situation)
        {
            if (list == null)
            {
                return;
            }

            foreach (var item in list)
            {
                if (item != null)
                {
                    item.enabled = situation;
                }
            }
        }

        public static bool IsIndexInList<T>(List<T> list, int index)
        {
            return list != null && list.Count > index && index >= 0;
        }

        public static bool IsThisIndexInList<T>(this List<T> list, int index)
        {
            return list != null && list.Count > index && index >= 0;
        }

        public static string MoneyTextConverter(int moneyAmount)
        {
            var text = "";
            switch (moneyAmount)
            {
                case >= 1000000000:
                {
                    var amount = moneyAmount / 1000000000f;
                    amount = (float)Math.Round(amount, 1);
                    text = amount.ToString();
                    text = ChangeCharFormString(text, ",", ".");
                    text += "B";
                    break;
                }
                case >= 1000000:
                {
                    var amount = moneyAmount / 1000000f;
                    amount = (float)Math.Round(amount, 1);
                    text = amount.ToString();
                    text = ChangeCharFormString(text, ",", ".");
                    text += "M";
                    break;
                }
                case >= 1000:
                {
                    var amount = moneyAmount / 1000f;
                    amount = (float)Math.Round(amount, 1);
                    text = amount.ToString();
                    text = ChangeCharFormString(text, ",", ".");
                    text += "K";
                    break;
                }
                default:
                {
                    text = moneyAmount.ToString();
                    break;
                }
            }

            return text;
        }

        public static string ChangeCharFormString(string text, string oldChar, string newChar)
        {
            var i = text.IndexOf(oldChar, StringComparison.Ordinal);
            while (i != -1)
            {
                text = text.Substring(0, i) + newChar + text.Substring(i + 1);
                i = text.IndexOf(oldChar, StringComparison.Ordinal);
            }

            return text;
        }

        public static List<T> ShuffleList<T>(this List<T> list)
        {
            if (list == null)
            {
                return null;
            }

            for (var i = list.Count - 1; i > 0; i--)
            {
                var index = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[index]) = (list[index], list[i]);
            }

            return list;
        }

        public static void ChangeAlpha(this SpriteRenderer item, float alpha)
        {
            var color = item.color;
            color.a = alpha;
            item.color = color;
        }

        public static void ChangeAlpha(this MeshRenderer item, float alpha)
        {
            var color = item.material.color;
            color.a = alpha;
            item.material.color = color;
        }

        public static void ChangeAlpha(this Material item, float alpha)
        {
            var color = item.color;
            color.a = alpha;
            item.color = color;
        }

        public static void ChangeAlpha(this Image item, float alpha)
        {
            var color = item.color;
            color.a = alpha;
            item.color = color;
        }
    }
}