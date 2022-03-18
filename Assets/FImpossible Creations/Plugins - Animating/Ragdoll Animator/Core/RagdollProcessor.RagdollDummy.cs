using FIMSpace;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class RagdollProcessor
    {
        internal Transform ObjectWithAnimator;
        internal Behaviour animator;
        internal Transform RagdollDummyBase;
        internal Transform RagdollDummyRoot;
        //internal Transform RagdollDummyRootParent;
        internal Transform RagdollDummySkeleton;
        internal Transform RootInParent;
        internal Transform RagdollDummyAnimator;


        private void PrepareRagdollDummy(Transform objectWithAnimator, Transform rootBone)
        {
            Vector3 startScale = objectWithAnimator.localScale;
            //objectWithAnimator.localScale = Vector3.one;

            ObjectWithAnimator = objectWithAnimator;
            animator = objectWithAnimator.GetComponent<Animator>();
            if (animator == null) animator = objectWithAnimator.GetComponent<Animation>();

            RagdollDummyAnimator = objectWithAnimator;

            GameObject ragdollReference = new GameObject(objectWithAnimator.name + "-Ragdoll-SkeletonOrigin");
            GameObject ragdollBase = new GameObject(objectWithAnimator.name + "-Ragdoll");
            RagdollDummyBase = ragdollBase.transform;
            RagdollDummyBase.position = ObjectWithAnimator.position;
            RagdollDummyBase.rotation = ObjectWithAnimator.rotation;
            RagdollDummyBase.localScale = ObjectWithAnimator.lossyScale;

            ragdollReference.transform.SetParent(RagdollDummyBase, true);
            ragdollReference.transform.position = objectWithAnimator.position;
            ragdollReference.transform.rotation = objectWithAnimator.rotation;
            ragdollReference.transform.localScale = Vector3.one;

            foreach (Transform t in objectWithAnimator.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<ConfigurableJoint>())
                { CharacterJoint cj = t.GetComponent<CharacterJoint>(); if (cj) { GameObject.Destroy(cj); } }
            }

            RagdollDummyRoot = ragdollReference.transform;

            Transform rootSkelBone = rootBone;

            if (rootSkelBone == null)
            {
                // Get main skinned mesh
                SkinnedMeshRenderer[] meshes = ObjectWithAnimator.GetComponentsInChildren<SkinnedMeshRenderer>();

                if (meshes.Length == 0)
                {
                    if (ObjectWithAnimator.childCount > 0)
                        for (int i = 0; i < ObjectWithAnimator.childCount; i++)
                        {
                            meshes = ObjectWithAnimator.GetChild(i).GetComponentsInChildren<SkinnedMeshRenderer>();
                            if (meshes.Length > 0) break;
                        }
                }

                if (meshes.Length == 0)
                {
                    UnityEngine.Debug.Log("[Ragdoll Animator] NOT FOUND SKINNED MESH RENDERERS IN TARGET MODEL! Skinned meshes are required by the component!");
                    UnityEngine.Debug.LogError("[Ragdoll Animator] NOT FOUND SKINNED MESH RENDERERS IN TARGET MODEL! Skinned meshes are required by the component!");
                    return;
                }

                SkinnedMeshRenderer mesh = meshes[0]; // Mesh with root nearest to animator
                int mainC = int.MaxValue;
                for (int m = 0; m < meshes.Length; m++)
                {
                    if (meshes[m].bones.Length > mainC)
                    {
                        mesh = meshes[m];
                        mainC = meshes[m].bones.Length;
                    }
                }

                rootSkelBone = mesh.rootBone;
            }

            Transform rootInParent = FTransformMethods.FindChildByNameInDepth(rootSkelBone.name, objectWithAnimator, true);

            if (rootInParent.parent != objectWithAnimator)
            {
                ragdollReference.transform.rotation = rootInParent.parent.rotation;
            }

            // Removing ragdoll components from source skeleton
            foreach (Transform t in rootInParent.GetComponentsInChildren<Transform>(true))
            {
                Collider cl = t.GetComponent<Collider>();
                if (cl) cl.enabled = false;
            }

            GameObject skeleton = GameObject.Instantiate(rootInParent.gameObject, RagdollDummyBase);
            RootInParent = rootInParent;
            RagdollDummySkeleton = skeleton.transform;
            skeleton.name = rootInParent.name;
            //skeleton.transform.localScale = rootInParent.transform.lossyScale;
            skeleton.transform.SetParent(ragdollReference.transform, true);
            skeleton.transform.position = rootInParent.transform.position;
            skeleton.transform.rotation = rootInParent.transform.rotation;
            skeleton.transform.localScale = rootInParent.transform.localScale;
            Transform sTransform = skeleton.transform;

            foreach (Transform t in skeleton.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<ConfigurableJoint>())
                { CharacterJoint cj = t.GetComponent<CharacterJoint>(); if (cj) { GameObject.Destroy(cj); } }
            }

            Transform ragPelvis = FTransformMethods.FindChildByNameInDepth(Pelvis.name, sTransform);


            Rigidbody rootRig = ragPelvis.transform.parent.gameObject.AddComponent<Rigidbody>();
            rootRig.isKinematic = true;
            Rigidbody hipsRig = ragPelvis.GetComponent<Rigidbody>();
            if (hipsRig) rootRig.interpolation = hipsRig.interpolation;

            
            ConfigurableJoint pelvConf = ragPelvis.GetComponent<ConfigurableJoint>();
            
            if (!pelvConf)
                if (HipsPin)
                {
                    pelvConf = ragPelvis.gameObject.AddComponent<ConfigurableJoint>();
                    pelvConf.angularXMotion = ConfigurableJointMotion.Free;
                    pelvConf.angularYMotion = ConfigurableJointMotion.Free;
                    pelvConf.angularZMotion = ConfigurableJointMotion.Free;

                    pelvConf.xMotion = ConfigurableJointMotion.Free;
                    pelvConf.yMotion = ConfigurableJointMotion.Free;
                    pelvConf.zMotion = ConfigurableJointMotion.Free;
                }

            if (pelvConf) pelvConf.connectedBody = rootRig;

            string chName = "";
            if (Chest) chName = Chest.name;

            SetRagdollTargetBones(
                sTransform,
                    ragPelvis,
                    FTransformMethods.FindChildByNameInDepth(SpineStart.name, sTransform),
                    FTransformMethods.FindChildByNameInDepth(chName, sTransform),
                    FTransformMethods.FindChildByNameInDepth(Head.name, sTransform),

                    FTransformMethods.FindChildByNameInDepth(LeftUpperArm.name, sTransform),
                    FTransformMethods.FindChildByNameInDepth(LeftForeArm.name, sTransform),
                    FTransformMethods.FindChildByNameInDepth(RightUpperArm.name, sTransform),
                    FTransformMethods.FindChildByNameInDepth(RightForeArm.name, sTransform),

                    FTransformMethods.FindChildByNameInDepth(LeftUpperLeg.name, sTransform),
                    FTransformMethods.FindChildByNameInDepth(LeftLowerLeg.name, sTransform),
                    FTransformMethods.FindChildByNameInDepth(RightUpperLeg.name, sTransform),
                    FTransformMethods.FindChildByNameInDepth(RightLowerLeg.name, sTransform)
                );


            SetAnimationPoseBones
                (
                    Pelvis,
                    SpineStart,
                    Chest,
                    Head,

                    LeftUpperArm,
                    LeftForeArm,
                    RightUpperArm,
                    RightForeArm,

                    LeftUpperLeg,
                    LeftLowerLeg,
                    RightUpperLeg,
                    RightLowerLeg
                    );


            if (posingLeftFist != null) { posingLeftFist.SetVisibleBone(LeftForeArm.GetChild(0)); }
            if (posingRightFist != null) { posingRightFist.SetVisibleBone(RightForeArm.GetChild(0)); }
            if (posingLeftFoot != null) { posingLeftFoot.SetVisibleBone(LeftLowerLeg.GetChild(0)); }
            if (posingRightFoot != null) { posingRightFoot.SetVisibleBone(RightLowerLeg.GetChild(0)); }


            // Removing ragdoll components from source skeleton
            foreach (Transform t in rootInParent.GetComponentsInChildren<Transform>(true))
            {
                ConfigurableJoint cc = t.GetComponent<ConfigurableJoint>();
                if (cc) GameObject.Destroy(cc);
                CharacterJoint cj = t.GetComponent<CharacterJoint>();
                if (cj) GameObject.Destroy(cj);
                Collider cl = t.GetComponent<Collider>();
                if (cl) GameObject.Destroy(cl);
                Rigidbody r = t.GetComponent<Rigidbody>();
                if (r) GameObject.Destroy(r);
            }


            // Optimizing ragdoll hierarchy
            if (posingLeftFist == null)
                DestroyChildren(posingLeftForeArm.transform);
            else
                DestroyChildren(posingLeftFist.transform);

            if (posingRightFist == null)
                DestroyChildren(posingRightForeArm.transform);
            else
                DestroyChildren(posingRightFist.transform);

            DestroyChildren(posingHead.transform);


            #region Collecting bones without rigidbodies which should be animated to keep ragdoll in sync with animator pose with higher precision

            List<Transform> toReAnimate = new List<Transform>();

            Transform p = posingHead.transform.parent;

            while (p != null)
            {
                Joint j = p.GetComponent<Joint>();
                if (j == null) if (toReAnimate.Contains(p) == false) toReAnimate.Add(p);
                p = p.parent; if (p == posingPelvis.transform) break;
            }

            p = posingLeftUpperArm.transform;
            while (p != null)
            {
                Joint j = p.GetComponent<Joint>();
                if (j == null) if (toReAnimate.Contains(p) == false) toReAnimate.Add(p);
                p = p.parent; if (p == posingPelvis.transform) break;
            }

            p = posingRightUpperArm.transform;
            while (p != null)
            {
                Joint j = p.GetComponent<Joint>();
                if (j == null) if (toReAnimate.Contains(p) == false) toReAnimate.Add(p);
                p = p.parent; if (p == posingPelvis.transform) break;
            }

            for (int t = 0; t < toReAnimate.Count; t++)
            {
                Transform ragBone = toReAnimate[t];
                Transform animBone = FTransformMethods.FindChildByNameInDepth(ragBone.name, objectWithAnimator.transform);

                if (animBone != null)
                {
                    PosingBone childB = null;

                    if (animBone.childCount > 0)
                    {
                        Transform childT = animBone.GetChild(0);

                        PosingBone pb = posingPelvis;
                        while (pb != null)
                        {
                            if (pb.visibleBone == childT) { childB = pb; break; }
                            pb = pb.child;
                        }

                        if (childB != null)
                            toReanimateBones.Add(new ToAnimateBone(animBone, ragBone, childB));
                    }
                }
            }

            #endregion


            //posingLeftUpperArm.transform.SetParent(ragdollReference.transform, true);
            //posingRightUpperArm.transform.SetParent(ragdollReference.transform, true);
            //posingRightUpperLeg.transform.SetParent(ragdollReference.transform, true);
            //posingLeftUpperLeg.transform.SetParent(ragdollReference.transform, true);
            Ragdoll_IgnoreCollision(posingSpineStart.collider, posingPelvis.collider);
            if (Chest) Ragdoll_IgnoreCollision(posingPelvis.collider, posingChest.collider);
            if (Chest) Ragdoll_IgnoreCollision(posingSpineStart.collider, posingChest.collider);
            if (Chest) Ragdoll_IgnoreCollision(posingChest.collider, posingHead.collider); else Ragdoll_IgnoreCollision(posingSpineStart.collider, posingHead.collider);
            if (Chest) Ragdoll_IgnoreCollision(posingChest.collider, posingLeftUpperArm.collider); else Ragdoll_IgnoreCollision(posingSpineStart.collider, posingLeftUpperArm.collider);
            if (Chest) Ragdoll_IgnoreCollision(posingChest.collider, posingRightUpperArm.collider); else Ragdoll_IgnoreCollision(posingSpineStart.collider, posingRightUpperArm.collider);
            Ragdoll_IgnoreCollision(posingRightUpperArm.collider, posingHead.collider);
            Ragdoll_IgnoreCollision(posingRightUpperArm.collider, posingRightForeArm.collider);
            Ragdoll_IgnoreCollision(posingLeftUpperArm.collider, posingHead.collider);
            Ragdoll_IgnoreCollision(posingLeftUpperArm.collider, posingLeftForeArm.collider);
            Ragdoll_IgnoreCollision(posingPelvis.collider, posingLeftUpperLeg.collider);
            Ragdoll_IgnoreCollision(posingPelvis.collider, posingRightUpperLeg.collider);
            Ragdoll_IgnoreCollision(posingRightUpperLeg.collider, posingRightLowerLeg.collider);
            Ragdoll_IgnoreCollision(posingLeftUpperLeg.collider, posingLeftLowerLeg.collider);

            if (posingRightFist != null) Ragdoll_IgnoreCollision(posingRightFist.collider, posingRightForeArm.collider);
            if (posingLeftFist != null) Ragdoll_IgnoreCollision(posingLeftFist.collider, posingLeftForeArm.collider);
            if (posingRightFoot != null) Ragdoll_IgnoreCollision(posingRightFoot.collider, posingRightLowerLeg.collider);
            if (posingLeftFoot != null) Ragdoll_IgnoreCollision(posingLeftFoot.collider, posingLeftLowerLeg.collider);

            PosingBone c = posingPelvis;
            while (c != null)
            {
                if (c.collider != null) c.collider.enabled = true;
                c = c.child;
            }

            if (containerForDummy) RagdollDummyBase.SetParent(containerForDummy, true);

            //RagdollDummyBase.localScale = objectWithAnimator.localScale;
            //objectWithAnimator.localScale = startScale;
        }

        public void Ragdoll_IgnoreCollision(Collider a, Collider b)
        {
            if (a != null && b != null) Physics.IgnoreCollision(a, b);
        }

        private void DestroyChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                GameObject.Destroy(parent.GetChild(i).gameObject);
        }
    }
}