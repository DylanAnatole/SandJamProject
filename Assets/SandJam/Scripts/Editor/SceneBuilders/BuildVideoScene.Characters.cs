using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using SandJamTest.Scene3D;

namespace SandJamTest.Editor
{
    public static partial class BuildVideoScene
    {
        static AnimatorController characterController;
        static void PrepareCharacterAnimation()
        {
            var originalWalk=Resources.Load<AnimationClip>("CharacterAnimation/WalkOriginal");
            var originalSit=Resources.Load<AnimationClip>("CharacterAnimation/SitOriginal");
            if(!originalWalk || !originalSit)throw new Exception("Recovered character clips missing");
            var walk=UnityEngine.Object.Instantiate(originalWalk);walk.name="Recovered Walk";
            var settings=AnimationUtility.GetAnimationClipSettings(walk);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(walk,settings);
            walk=Save(walk,"CharacterWalk.anim");
            var sit=UnityEngine.Object.Instantiate(originalSit);sit.name="Recovered Sit";
            settings=AnimationUtility.GetAnimationClipSettings(sit);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(sit,settings);sit=Save(sit,"CharacterSit.anim");
            string path=Folder+"/CharacterMotion.controller";
            characterController=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(!characterController)characterController=AnimatorController.CreateAnimatorControllerAtPath(path);
            // Rebuild only this generated controller; keep its GUID stable.
            foreach(var layer in characterController.layers)foreach(var state in layer.stateMachine.states)layer.stateMachine.RemoveState(state.state);
            characterController.parameters=new AnimatorControllerParameter[0];characterController.AddParameter("Walking",AnimatorControllerParameterType.Bool);
            var machine=characterController.layers[0].stateMachine;
            var idle=machine.AddState("Sit");idle.motion=sit;
            var moving=machine.AddState("Walk");moving.motion=walk;
            machine.defaultState=idle;
            var toWalk=idle.AddTransition(moving);toWalk.hasExitTime=false;toWalk.duration=.08f;toWalk.AddCondition(AnimatorConditionMode.If,0,"Walking");
            var toSit=moving.AddTransition(idle);toSit.hasExitTime=false;toSit.duration=.1f;toSit.AddCondition(AnimatorConditionMode.IfNot,0,"Walking");
            EditorUtility.SetDirty(characterController);
        }
        static CharacterMotion AnimatedCharacter(Transform parent,int color,float height,bool walking=false)
        {
            var prefab=Resources.Load<GameObject>("Original/CharacterVisual");
            var visual=UnityEngine.Object.Instantiate(prefab,parent,false);visual.name="Recovered animated character";
            visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one;
            var skin=visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if(!skin || skin.bones.Length!=41 || skin.bones.Any(b=>!b))throw new Exception("Recovered 41-bone rig is incomplete");
            var surface=DepthMaterial("AnimatedCharacter_"+color,VideoColor(color),1,.014f,.16f);
            // The exported body has two submeshes. Both must receive a material or parts vanish.
            skin.sharedMaterials=Enumerable.Repeat(surface,skin.sharedMesh.subMeshCount).ToArray();skin.updateWhenOffscreen=true;
            skin.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;skin.receiveShadows=false;
            var rig=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="character_animations");
            var clip=Resources.Load<AnimationClip>("CharacterAnimation/WalkOriginal");clip.SampleAnimation(rig.gameObject,0);
            // Compute the bind-pose skinning once. The exported mesh has a centimetre transform;
            // BakeMesh before Animator initialization applies that scale differently from runtime.
            var mesh=skin.sharedMesh;var sourceVertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;
            var matrices=skin.bones.Select((bone,i)=>bone.localToWorldMatrix*bind[i]).ToArray();
            var vertices=new Vector3[sourceVertices.Length];
            var skinBounds=new Bounds();
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];var v=sourceVertices[i];
                var world=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                vertices[i]=visual.transform.InverseTransformPoint(world);
                var local=skin.transform.InverseTransformPoint(world);if(i==0)skinBounds=new Bounds(local,Vector3.zero);else skinBounds.Encapsulate(local);
            }
            var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var vertex in vertices)bounds.Encapsulate(vertex);
            skinBounds.Expand(skinBounds.size*.5f);skin.localBounds=skinBounds;
            float scale=height/bounds.size.y;
            if(scale<=0 || scale>10)throw new Exception("Recovered rig normalization is invalid: "+bounds);
            visual.transform.localScale=Vector3.one*scale;
            visual.transform.localPosition=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
            var animator=rig.gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=characterController;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var driver=parent.gameObject.AddComponent<CharacterMotion>();driver.Animator=animator;driver.WalkContinuously=walking;
            Resources.Load<AnimationClip>(walking?"CharacterAnimation/WalkOriginal":"CharacterAnimation/SitOriginal").SampleAnimation(rig.gameObject,walking?0:1.3f);
            return driver;
        }
    }
}
