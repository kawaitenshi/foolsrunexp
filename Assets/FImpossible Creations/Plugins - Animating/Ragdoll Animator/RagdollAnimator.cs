using System;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    [AddComponentMenu("FImpossible Creations/Ragdoll Animator")]
    [DefaultExecutionOrder(-1)]
    public class RagdollAnimator : MonoBehaviour
    {
        [HideInInspector] public bool _EditorDrawSetup = true;

        [SerializeField]
        private RagdollProcessor Processor;

        [Tooltip("! REQUIRED ! Just object with Animator and skeleton as child transforms")]
        public Transform ObjectWithAnimator;
        [Tooltip("If null then it will be found automatically - do manual if you encounter some errors after entering playmode")]
        public Transform RootBone;

        [Tooltip("! OPTIONAL ! Leave here nothing to not use the feature! \n\nObject with bones structure to which ragdoll should try fit with it's pose.\nUseful only if you want to animate ragdoll with other animations than the model body animator.")]
        public Transform CustomRagdollAnimator;
        [Tooltip("If generated ragdoll should be destroyed when main skeleton root object stops existing")]
        public bool AutoDestroy = true;
        [Tooltip("Generated ragdoll dummy will be put inside this transform as child object.\n\nAssign main character object for ragdoll to react with character movement rigidbody motion, set other for no motion reaction.")]
        public Transform TargetParentForRagdollDummy;
        public RagdollProcessor Parameters { get { return Processor; } }

        private void Reset()
        {
            if (Processor == null) Processor = new RagdollProcessor();
            Processor.TryAutoFindReferences(transform);
            Animator an = GetComponentInChildren<Animator>();
            if (an) ObjectWithAnimator = an.transform;
        }

        private void Start()
        {
            Processor.Initialize(this, ObjectWithAnimator, CustomRagdollAnimator, RootBone, TargetParentForRagdollDummy);
            if (AutoDestroy) { autoDestroy = Processor.RagdollDummyBase.gameObject.AddComponent<RagdollAutoDestroy>(); autoDestroy.Parent = Processor.Pelvis.gameObject; }
        }

        private void FixedUpdate()
        {
            Processor.FixedUpdate();
        }

        private void LateUpdate()
        {
            Processor.LateUpdate();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Processor.DrawGizmos();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                Parameters.SwitchAllExtendedAnimatorSync(Parameters.ExtendedAnimatorSync);
            }
        }
#endif


        // --------------------------------------------------------------------- UTILITIES


        /// <summary>
        /// Adding physical push impact to single rigidbody limb
        /// </summary>
        /// <param name="limb"> Access 'Parameters' for ragdoll limb </param>
        /// <param name="powerDirection"> World space direction vector </param>
        /// <param name="duration"> Time in seconds </param>
        public void User_SetLimbImpact(Rigidbody limb, Vector3 powerDirection, float duration)
        {
            StartCoroutine(Processor.User_SetLimbImpact(limb, powerDirection, duration));
        }

        /// <summary>
        /// Transitioning ragdoll blend value
        /// </summary>
        public void User_EnableFreeRagdoll(float blend = 1f)
        {
            Parameters.FreeFallRagdoll = true;
            User_FadeRagdolledBlend(blend, 0.2f);
        }

        /// <summary>
        /// Adding physical push impact to all limbs of the ragdoll
        /// </summary>
        /// <param name="powerDirection"> World space direction vector </param>
        /// <param name="duration"> Time in seconds </param>
        public void User_SetPhysicalImpactAll(Vector3 powerDirection, float duration)
        {
            StartCoroutine(Processor.User_SetPhysicalImpactAll(powerDirection, duration));
        }

        /// <summary>
        /// Enable / disable animator component with delay
        /// </summary>
        public void User_SwitchAnimator(Transform unityAnimator = null, bool enabled = false, float delay = 0f)
        {
            if (unityAnimator == null) unityAnimator = ObjectWithAnimator;
            if (unityAnimator == null) return;

            Animator an = unityAnimator.GetComponent<Animator>();
            if (an)
            {
                StartCoroutine(Processor.User_SwitchAnimator(an, enabled,  delay));
            }
        }

        /// <summary>
        /// Triggering different methods which are used in the demo scene for animating getting up from ragdolled state
        /// </summary>
        /// <param name="groundMask"></param>
        public void User_GetUpStack(RagdollProcessor.EGetUpType getUpType, LayerMask groundMask, float targetRagdollBlend = 0f, float targetMusclesPower = 0.85f)
        {
            StopAllCoroutines();
            User_SwitchAnimator(null, true);
            User_ForceRagdollToAnimatorFor(0.75f, 0.2f);
            Parameters.FreeFallRagdoll = false;
            User_FadeMuscles(targetMusclesPower, 1f, 0.05f);
            User_FadeRagdolledBlend(targetRagdollBlend, 1.25f);
            User_RepositionRoot(null, null, getUpType, groundMask);
        }

        /// <summary>
        /// Transitioning all rigidbody muscles power to target value
        /// </summary>
        /// <param name="forcePoseEnd"> Target muscle power </param>
        /// <param name="duration"> Transition duration </param>
        /// <param name="delay"> Delay to start transition </param>
        public void User_FadeMuscles(float forcePoseEnd = 0f, float duration = 0.75f, float delay = 0f)
        {
            StartCoroutine(Parameters.User_FadeMuscles(forcePoseEnd, duration, delay));
        }

        /// <summary>
        /// Forcing applying rigidbody pose to the animator pose and fading out to zero smoothly
        /// </summary>
        internal void User_ForceRagdollToAnimatorFor(float duration = 1f, float forcingFullDelay = 0.2f)
        {
            StartCoroutine(Parameters.User_ForceRagdollToAnimatorFor(duration, forcingFullDelay));
        }

        /// <summary>
        /// Transitioning ragdoll blend value
        /// </summary>
        public void User_FadeRagdolledBlend(float targetBlend = 0f, float duration = 0.75f, float delay = 0f)
        {
            StartCoroutine(Parameters.User_FadeRagdolledBlend(targetBlend, duration, delay));
        }

        /// <summary>
        /// Setting all ragdoll limbs rigidbodies kinematic or non kinematic
        /// </summary>
        public void User_SetAllKinematic(bool kinematic = true)
        {
            Parameters.User_SetAllKinematic(kinematic);
        }

        /// <summary>
        /// Making pelvis kinematic and anchored to pelvis position
        /// </summary>
        public void User_AnchorPelvis(bool anchor = true, float duration = 0f)
        {
            StartCoroutine(Parameters.User_AnchorPelvis(anchor, duration));
        }

        /// <summary>
        /// Moving ragdoll controller object to fit with current ragdolled position hips
        /// </summary>
        public void User_RepositionRoot(Transform root = null, Vector3? worldUp = null, RagdollProcessor.EGetUpType getupType = RagdollProcessor.EGetUpType.None, LayerMask? snapToGround = null)
        {
            Parameters.User_RepositionRoot(root, null, worldUp, getupType, snapToGround);
        }


        #region Auto Destroy Reference

        private void OnDestroy()
        {
            if (autoDestroy != null) autoDestroy.StartChecking();
        }

        private RagdollAutoDestroy autoDestroy = null;
        private class RagdollAutoDestroy : MonoBehaviour
        {
            public GameObject Parent;
            public void StartChecking() { Check(); if (Parent != null) InvokeRepeating("Check", 0.05f, 0.5f); }
            void Check() { if (Parent == null) Destroy(gameObject); }
        }

        #endregion

    }
}