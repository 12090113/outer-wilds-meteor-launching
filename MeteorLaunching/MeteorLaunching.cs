using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MeteorLaunching
{
    public class MeteorLaunching : ModBehaviour
    {
        PlayerBody playerBody;
        Transform launcher;
        GameObject meteor;
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
                launcher = FindObjectOfType<FirstPersonManipulator>().transform;
                meteor = GameObject.Find("VolcanicMoon_Body/Sector_VM/Effects_VM/VolcanoPivot/MeteorLauncher").GetComponent<MeteorLauncher>()._meteorPrefab;
                sunFluid = GameObject.Find("Sun_Body/Sector_SUN/Volumes_SUN/ScaledVolumesRoot/DestructionFluidVolume").GetComponent<SimpleFluidVolume>();
                audio = GameObject.Find("Player_Body/Audio_Player/OneShotAudio_Player").GetComponent<OWAudioSource>();
            };
        }

        private void Update()
        {
            if (Mouse.current.middleButton.wasPressedThisFrame || (useOWInput && OWInput.IsNewlyPressed(InputLibrary.cancel) && OWInput.IsInputMode(InputMode.Character) && (Locator.GetToolModeSwapper().IsInToolMode(ToolMode.None) || Locator.GetToolModeSwapper().IsInToolMode(ToolMode.Item))))
            {
                GameObject newMeteor = Instantiate(meteor, launcher.position + launcher.forward * launchSize*20, launcher.rotation);
                newMeteor.GetComponent<Rigidbody>().velocity = playerBody.GetVelocity() + launcher.forward * launchSpeed;
                newMeteor.transform.localScale =  new Vector3(launchSize, launchSize, launchSize);
                newMeteor.name = "pew pew KABOOM";

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
                audio.PlayOneShot(AudioType.BH_MeteorLaunch, 0.25f);
            }
        }

        public override void Configure(IModConfig config)
        {
            this.useOWInput = config.GetSettingsValue<bool>("Use back button");
            this.launchSpeed = config.GetSettingsValue<float>("Meteor Launch Speed");
            this.launchSize = config.GetSettingsValue<float>("Meteor Launch Size");
        }
    }
}
