using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ars_unity_extensions.Runtime
{
    public static class ArsExtensions
    {
        #region Animator

        public static float GetAnimationClipLenght(this Animator animator, string clipName)
        {
            var clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(i => i.name.Equals(clipName));
            return clip ? clip.length : 0f;
        }

        #endregion

        #region Random_IEnumerable_ICollection_Enum

        public static T RandomOne<T>(this IEnumerable<T> collection) where T : class
        {
            var enumerable = collection as T[] ?? collection.ToArray();

            if (!(bool)enumerable?.Any())
            {
                return null;
            }

            return enumerable.ElementAt(UnityEngine.Random.Range(0, enumerable.Length));
        }

        public static T RandomOne<T>(this ICollection<T> collection) where T : struct
        {
            if (!(collection?.Any() ?? false))
            {
                return default;
            }

            return collection.ElementAt(UnityEngine.Random.Range(0, collection.Count));
        }

        /// <summary>
        /// Example:
        /// var randomMyEnum = typeof(MyEnum).RandomOne<MyEnum>();
        /// </summary>
        public static T RandomOne<T>(this Type e) where T : Enum
        {
            var collection = EnumToEnumerable<T>(e).ToArray();

            if (!(collection?.Any() ?? false))
            {
                return default;
            }

            return collection.ElementAt(UnityEngine.Random.Range(0, collection.Count()));
        }

        /// <summary>
        /// Example:
        /// var except = new List<MyEnum> { MyEnum.Item1, MyEnum.Item2 };
        /// var randomMyEnum = typeof(MyEnum).RandomOne<MyEnum>(except);
        /// </summary>
        public static T RandomOne<T>(this Type e, IEnumerable<T> except) where T : Enum
        {
            var collection = EnumToEnumerable<T>(e).ToList();

            if (!(bool)collection?.Any())
            {
                return default;
            }

            foreach (var t in except) collection.Remove(t);

            return !collection.Any() ? default : collection.ElementAt(UnityEngine.Random.Range(0, collection.Count()));
        }

        #endregion

        #region Camera

        /// <summary>
        ///     Transforms a Input.mousePosition point from screen space into world space.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Vector2 TouchToWorldPoint(this Camera camera)
        {
            Vector2 touchPoint = Input.mousePosition;

            return camera.ScreenToWorldPoint(new Vector3(touchPoint.x, touchPoint.y, camera.nearClipPlane));
        }

        public static Vector2 TouchToWorldPoint(this Camera camera, Vector2 touchPoint) =>
            camera.ScreenToWorldPoint(new Vector3(touchPoint.x, touchPoint.y, camera.nearClipPlane));

        public static RaycastHit2D TouchToRaycastHit(this Camera camera, string tag = "") =>
            TouchToRaycastHit(camera, Input.mousePosition, tag);

        public static RaycastHit2D[] TouchToRaycastHits(this Camera camera, string tag = "") =>
            TouchToRaycastHits(camera, Input.mousePosition, tag);

        public static RaycastHit2D TouchToRaycastHit(this Camera camera, Vector3 touchPosition, string tag = "")
        {
            var hits = TouchToRaycastHits(camera, touchPosition, tag);

            return tag != string.Empty ? hits.FirstOrDefault(i => i.transform.CompareTag(tag)) : hits[0];
        }

        public static RaycastHit2D[] TouchToRaycastHits(this Camera camera, Vector3 touchPosition, string tag = "")
        {
            var ray = camera.ScreenPointToRay(touchPosition);
            var hits = Physics2D.RaycastAll(ray.origin, ray.direction);

            return tag != string.Empty ? hits.Where(i => i.transform.CompareTag(tag)).ToArray() : hits;
        }

        public static RaycastHit2D WorldToRaycastHit(this Camera camera, Vector2 worldPosition, string tag = "") =>
            TouchToRaycastHit(camera, camera.WorldToScreenPoint(worldPosition), tag);

        #endregion

        #region RectTransform

        public static Vector3 WorldPosition(this RectTransform rectTransform)
        {
            var wInCorners = new Vector3[4];
            rectTransform.GetWorldCorners(wInCorners);

            var x = wInCorners[2].x + (wInCorners[0].x - wInCorners[2].x) / 2f;
            var y = wInCorners[2].y + (wInCorners[0].y - wInCorners[2].y) / 2f;

            return new Vector3(x, y);
        }

        /// <summary>
        ///     Converts the anchoredPosition of the first RectTransform to the second RectTransform,
        ///     taking into consideration offset, anchors and pivot, and returns the new anchoredPosition
        /// </summary>
        public static Vector2 RelativeToRectTransform(this RectTransform from, RectTransform to)
        {
            Vector2 localPoint;

            var fromPivotDerivedOffset = new Vector2(from.rect.width * 0.5f + from.rect.xMin,
                from.rect.height * 0.5f + from.rect.yMin);

            var screenP = RectTransformUtility.WorldToScreenPoint(null, from.position);
            screenP += fromPivotDerivedOffset;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(to, screenP, null, out localPoint);

            var pivotDerivedOffset =
                new Vector2(to.rect.width * 0.5f + to.rect.xMin, to.rect.height * 0.5f + to.rect.yMin);

            return to.anchoredPosition + localPoint - pivotDerivedOffset;
        }

        #endregion

        #region Enum

        public static IEnumerable<T> EnumToEnumerable<T>(this Type e) =>
            Enum.GetNames(e).Select(item => (T)Enum.Parse(e, item));

        #endregion

        #region Enumerable

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection) =>
            collection.OrderBy(i => Guid.NewGuid());

        #endregion
    }

    public static class ArsUtils
    {
        public static string ToHtmlStringRgba(Color color) =>
            $"#{ColorUtility.ToHtmlStringRGBA(color)}";

        #region 2DPath

        public static Vector3[] TransformToVectorPath(Transform[] path)
        {
            var vectorPath = new Vector3[path.Length];

            for (var i = 0; i < path.Length; i++) vectorPath[i] = path[i].position;

            return vectorPath;
        }

        public static float CalculatePathDuration
        (
            Vector3[] path,
            Vector3 startPosition,
            float speed = 1f,
            bool run = false,
            float runSpeed = 3f,
            bool jump = false,
            float jumpSpeed = 5f
        )
        {
            var duration = 0f;

            duration += Vector2.Distance(startPosition, path[0]);
            for (var i = 1; i < path.Length; i++) duration += Vector2.Distance(path[i - 1], path[i]);

            duration *= speed;

            if (run)
            {
                duration *= runSpeed;
            }

            if (jump)
            {
                duration *= jumpSpeed;
            }

            return duration;
        }

        public static float CalculatePathDuration
        (
            Transform[] path,
            Vector3 startPosition,
            float speed = 1f,
            bool run = false,
            float runSpeed = 3f,
            bool jump = false,
            float jumpSpeed = 5f
        )
        {
            return CalculatePathDuration(TransformToVectorPath(path), startPosition, speed, run, runSpeed, jump,
                jumpSpeed);
        }

        #endregion

        #region Pointer

        public static bool IsPointerOverUIObject()
        {
            if (!EventSystem.current)
            {
                return false;
            }

            var eventDataCurrentPosition = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

            return results.Any();
        }

        public static bool IsPointerOverGameObject()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began
                                     && EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId))
            {
                return true;
            }

            return false;
        }

        #endregion


#if UNITY_EDITOR
        public static RaycastHit2D UIToWorldRaycastHit(Vector2 mousePosition, string tag = "")
        {
            var worldRay = HandleUtility.GUIPointToWorldRay(mousePosition);
            var hits = Physics2D.RaycastAll(worldRay.origin, worldRay.direction);

            return tag != string.Empty ? hits.FirstOrDefault(i => i.transform.CompareTag(tag)) : hits[0];
        }

        public static RaycastHit2D UIToWorldRaycastHit(string tag = "") =>
            UIToWorldRaycastHit(Event.current.mousePosition, tag);
#endif
    }
}