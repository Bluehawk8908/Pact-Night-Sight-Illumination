using System.Collections;
using System.Reflection;
using UnityEngine;
using MelonLoader;
using GHPC.Vehicle;
using GHPC.Weapons;
using GHPC.Equipment.Optics;
using Reticle;
using GHPC.State;
using PactNightSightIllumination;
using HarmonyLib;

[assembly: MelonInfo(typeof(PactNightSights), "Pact Night Sight Illumination", "1.0.0", "Bluehawk")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace PactNightSightIllumination
{
    public class PactNightSights : MelonMod
    {
        static bool activeScene = false;
        public static GameObject gameManager;        

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu2_Scene" || sceneName == "t64_menu" || sceneName == "MainMenu2-1_Scene")
            {
                activeScene = false;
                return;
            }

            gameManager = GameObject.Find("_APP_GHPC_");
            if (gameManager == null) { return; }

            StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Illuminate), GameStatePriority.Medium);
        }
    
        public IEnumerator Illuminate(GameState _)
        {
            if (activeScene == true) { yield break; }
            activeScene = true;
            Vehicle[] list = GameObject.FindObjectsByType<Vehicle>(FindObjectsSortMode.None);

            foreach (Vehicle vic in list)
            {
                FireControlSystem fcs = vic.WeaponsManager.Weapons[0].FCS;
                if (fcs.NightOptic == null) { continue; }                
                UsableOptic nightOptic = fcs.NightOptic;
                ReticleMesh nightRetMesh = nightOptic.reticleMesh;
                if (nightRetMesh.reticleSO.name != "T55-NVS") { continue; }
                ReticleTree.Light light = new ReticleTree.Light();
                light.color = new Reticle.RGB(0f, 1f, 0f, true);
                light.type = ReticleTree.Light.Type.Powered;                
                nightRetMesh.lights.Add(new ReticleEditor.LightState(light, 0.2f));
                nightRetMesh.reticleSO.lights.Add(light);                
            }

            yield break;
        }
    }

    [HarmonyPatch(typeof(UsableOptic), "ToggleIllumination")]
    public static class ForceToggle
    {
        private static void Postfix(UsableOptic __instance)
        {
            if (__instance.reticleMesh.reticleSO.name == "T55-NVS")
            {
                ReticleMesh retMesh = __instance.reticleMesh;               
                FieldInfo field = typeof(ReticleMesh).GetField("lightCols", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo update = retMesh.GetType().GetMethod("UpdateLights", BindingFlags.Instance | BindingFlags.NonPublic);
                Vector4[] colours = field.GetValue(retMesh) as Vector4[];
                if (colours[0].y == 0) { colours[0].y = 0.2f; }
                else { colours[0].y = 0f; }                
                update.Invoke(retMesh, null);                
            }
        }
    }
}
