using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace MeteorLaunching
{
    public class MeteorLaunching : ModBehaviour
    {
        PlayerBody playerBody;
        Transform launcher;
        GameObject[] projectiles;
        int p = 0;
        SimpleFluidVolume sunFluid;
        protected OWAudioSource audio;
        public float launchSpeed = 1000;
        public float launchSize = 1;
        public bool useOWInput = false;

        private void Start()
        {
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                playerBody = FindObjectOfType<PlayerBody>();
                projectiles = new GameObject[] {
                    GameObject.Find("VolcanicMoon_Body/Sector_VM/Effects_VM/VolcanoPivot/MeteorLauncher").GetComponent<MeteorLauncher>()._meteorPrefab,
                    GameObject.Find("Player_Body/RoastingSystem").GetComponent<RoastingStickController>()._mallowBodyPrefab
                };
                sunFluid = GameObject.Find("Sun_Body/Sector_SUN/Volumes_SUN/ScaledVolumesRoot/DestructionFluidVolume").GetComponent<SimpleFluidVolume>();
                audio = GameObject.Find("Player_Body/Audio_Player/OneShotAudio_Player").GetComponent<OWAudioSource>();
                StartCoroutine(SetLauncher());
            };
        }

        private IEnumerator SetLauncher()
        {
            yield return new WaitForEndOfFrame();
            launcher = FindObjectOfType<FirstPersonManipulator>().transform;
        }

        private void Update()
        {
            if (BackPressed() || Mouse.current.middleButton.wasPressedThisFrame)
            {
                GameObject newMeteor = Instantiate(projectiles[p], launcher.position + launcher.forward * .5f + launcher.forward * launchSize * projectiles[p].GetComponentInChildren<MeshRenderer>().bounds.size.x * .5f, launcher.rotation);
                /*GameObject newMeteor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                newMeteor.transform.position = launcher.position + launcher.forward * launchSize * 2;
                newMeteor.transform.rotation = launcher.rotation;
                var owrigid = newMeteor.AddComponent<OWRigidbody>();
                //newMeteor.AddComponent<CenterOfTheUniverseOffsetApplier>();
                GameObject Detector = new GameObject("Detector");
                Detector.transform.SetParent(newMeteor.transform);
                var force = Detector.AddComponent<DynamicForceDetector>();
                var fluid = Detector.AddComponent<DynamicFluidDetector>();
                owrigid._attachedForceDetector = force;
                owrigid._attachedFluidDetector = fluid;*/
                newMeteor.GetComponent<Rigidbody>().velocity = launcher.forward * launchSpeed;
                newMeteor.transform.localScale = new Vector3(launchSize, launchSize, launchSize);
                newMeteor.name = "Projectile";
                if (p == 1)
                {
                    Destroy(newMeteor.GetComponent<SelfDestruct>());
                    newMeteor.GetComponentInChildren<MeshRenderer>().material.color = new Color(1, 1, 1, 0);
                }
                
                else if (p == 0) {
                    FluidVolume closeFluid = sunFluid;
                    var fluids = FindObjectsOfType<SimpleFluidVolume>();
                    foreach (var fluid in fluids)
                    {
                        float distance = Vector3.Distance(playerBody.GetPosition(), fluid.transform.position);
                        if (distance < 1000 && fluid._allowShipAutoroll && distance < Vector3.Distance(playerBody.GetPosition(), closeFluid.transform.position))
                        {
                            closeFluid = fluid;
                        }
                    }
                    //ModHelper.Console.WriteLine($"" + closeFluid);
                    newMeteor.transform.Find("ConstantDetectors").GetComponent<ConstantFluidDetector>()._onlyDetectableFluid = closeFluid;
                    MeteorController newMeteorContr = newMeteor.GetComponent<MeteorController>();
                    newMeteorContr._heat = 1;
                    newMeteorContr._hasLaunched = true;
                    newMeteorContr._suspendRoot = playerBody.transform;
                }
                audio.PlayOneShot(AudioType.BH_MeteorLaunch, 0.25f);
            }
            if (Keyboard.current.lKey.wasPressedThisFrame || (OWInput.IsNewlyPressed(InputLibrary.rollMode) && BackPressed()))
            {
                p++;
                if (p > projectiles.Length)
                    p = 0;
                ModHelper.Console.WriteLine($"" + projectiles[p]);
            }
        }

        private bool BackPressed()
        {
            return useOWInput && OWInput.IsNewlyPressed(InputLibrary.cancel) && OWInput.IsInputMode(InputMode.Character) && (Locator.GetToolModeSwapper().IsInToolMode(ToolMode.None) || Locator.GetToolModeSwapper().IsInToolMode(ToolMode.Item));
        }

        public override void Configure(IModConfig config)
        {
            this.useOWInput = config.GetSettingsValue<bool>("Use back button");
            this.launchSpeed = config.GetSettingsValue<float>("Meteor Launch Speed");
            this.launchSize = config.GetSettingsValue<float>("Meteor Launch Size");
        }
    }
}
