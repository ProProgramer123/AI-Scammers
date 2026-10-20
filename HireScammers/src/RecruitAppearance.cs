using System;
using System.Collections.Generic;
using UnityEngine;
namespace HireScammers {
// Keep cloned NPCs independent of materials owned and destroyed by the native generator.
public sealed class AppearanceResources : MonoBehaviour {
 public readonly List<Material> Materials=new List<Material>();
 void OnDestroy(){foreach(var material in Materials)if(material)Destroy(material);Materials.Clear();}
}
public static class RecruitAppearance {
 static readonly Color[] Shirts={new Color(.10f,.48f,.42f),new Color(.55f,.15f,.19f),new Color(.43f,.28f,.65f),new Color(.70f,.48f,.12f),new Color(.14f,.27f,.55f),new Color(.75f,.30f,.11f),new Color(.35f,.42f,.46f),new Color(.28f,.45f,.15f)};
 static readonly Color[] Pants={new Color(.14f,.18f,.22f),new Color(.28f,.23f,.19f),new Color(.18f,.21f,.29f),new Color(.26f,.27f,.26f)};
 public static int Palette(int slot,ulong owner){return (int)((owner%8+(ulong)Math.Max(0,slot))%8);}
 public static Color Shirt(int slot,ulong owner){return Shirts[Palette(slot,owner)];}
 public static int Apply(GameObject model,int slot,ulong owner){
  int shirtId=Shader.PropertyToID("_ShirtColor"),pantsId=Shader.PropertyToID("_PantsColor"),vestId=Shader.PropertyToID("_VestColor");
  int palette=Palette(slot,owner),changed=0;var shirt=Shirts[palette];var pants=Pants[palette%Pants.Length];
  foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){
   var materials=renderer.sharedMaterials;
   for(int i=0;i<materials.Length;i++){
    var material=materials[i];if(!material||!material.HasProperty(shirtId)||!material.HasProperty(pantsId))continue;
    // These native shader properties affect clothing only; skin and hair stay intact.
    material.SetColor(shirtId,shirt);material.SetColor(pantsId,pants);if(material.HasProperty(vestId))material.SetColor(vestId,shirt);
    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,i);block.SetColor(shirtId,shirt);block.SetColor(pantsId,pants);if(material.HasProperty(vestId))block.SetColor(vestId,shirt);renderer.SetPropertyBlock(block,i);changed++;
   }
  }
  return changed;
 }
}
}
