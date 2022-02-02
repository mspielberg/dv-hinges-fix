using HarmonyLib;
using UnityEngine;

namespace DvMod.HingesFix
{
    public static class HingeDrag
    {
        private static CapsuleCollider? collider;

        [HarmonyPatch(typeof(Grabber), nameof(Grabber.OnDraggingEnter))]
        public static class OnDraggingEnterPatch
        {
            public static bool Prefix(Grabber __instance)
            {
                if (!(__instance.currentlyHovered is GrabHandlerHingeJoint handler))
                    return true;
                var globalAxis = __instance.currentlyHovered.transform.TransformDirection(__instance.currentlyHovered.GetAxis());
                var projection = Vector3.ProjectOnPlane(__instance.ray.direction, globalAxis);
                var angle = Vector3.Angle(projection, __instance.ray.direction);
                if (angle > 30)
                    return true;

                __instance.currentlyGrabbed = __instance.currentlyHovered;

                var colliderGO = new GameObject();
                colliderGO.transform.SetParent(__instance.currentlyGrabbed.transform);
                colliderGO.transform.localRotation = Quaternion.LookRotation(Vector3.forward, handler.GetAxis());
                colliderGO.transform.localPosition = handler.GetAnchor();
                RaycastHit hit = __instance.hit;
                if (hit.collider == null && !Physics.SphereCast(__instance.ray, __instance.sphereCastRadius, out hit, __instance.sphereCastMaxDistance, __instance.sphereCastMask.value, QueryTriggerInteraction.Collide))
                    return false;
                Vector3 localHit = colliderGO.transform.InverseTransformPoint(hit.point);
                localHit.y = 0;
                var radius = localHit.magnitude;
                collider = colliderGO.AddComponent<CapsuleCollider>();
                collider.isTrigger = true;
                collider.radius = radius;
                colliderGO.transform.parent = __instance.currentlyGrabbed.transform.parent;

                __instance.currentlyGrabbed.StartInteraction(hit.point, __instance);
                __instance.currentlyGrabbed.EndInteractionForced += __instance.EndInteractionForced;
                __instance.Grab(__instance.currentlyGrabbed.transform);

                return false;
            }
        }

        [HarmonyPatch(typeof(Grabber), nameof(Grabber.OnDraggingExit))]
        public static class OnDraggingExitPatch
        {
            public static void Postfix()
            {
                if (collider != null)
                {
                    GameObject.Destroy(collider.gameObject);
                    collider = null;
                }
            }
        }

        [HarmonyPatch(typeof(Grabber), nameof(Grabber.OnDragUpdate))]
        public static class OnDragUpdatePatch
        {
            public static bool Prefix(Grabber __instance)
            {
                if (collider == null)
                    return true;
                if (__instance.currentlyGrabbed == null)
                {
                    Debug.LogWarning($"Destroyed while dragging interactable! Returning to {Grabber.State.Idle}!");
                    __instance.isUnexpectedlyDestroyed = true;
                    __instance.fsm.Fire(Grabber.Trigger.UnexpectedDestroy);
                    return false;
                }
                if (__instance.input.UseKeyUp)
                {
                    __instance.fsm.Fire(Grabber.Trigger.Release);
                    return false;
                }
                if (collider.Raycast(__instance.ray, out RaycastHit hit, __instance.sphereCastMaxDistance))
                {
                    __instance.currentlyGrabbed.FeedPosition(hit.point);
                }
                if (__instance.holdStateForced)
                {
                    if (__instance.ForceHoldCoro != null)
                    {
                        __instance.StopCoroutine(__instance.ForceHoldCoro);
                    }
                    __instance.ForceHoldCoro = __instance.StartCoroutine(__instance.ForceHold());
                }
                return false;
            }
        }
    }
}
