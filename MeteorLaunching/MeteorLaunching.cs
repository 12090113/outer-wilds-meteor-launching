using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MeteorLaunching
{
    public class MeteorLaunching : ModBehaviour
    {
        private static MeteorLaunching instance;
        public static MeteorLaunching Instance => instance;

        public PlayerBody playerBody;
        public Transform launcher;
        public GameObject[] projectiles;
        public int p = 0;
        public SimpleFluidVolume sunFluid;
        public OWAudioSource audio;
        public Text text;
        public float launchSpeed = 1000;
        public float launchSize = 1;
        public bool useOWInput = false;
        public int timer = -1;
        public bool initialized = false;
        public bool lateInitialized = false;
        public bool initializedProjectiles = false;

        public class ProjectilesInitializeEvent : UnityEvent { }
        public class ProjectileSwitchEvent : UnityEvent<int> { }
        public class ProjectileLaunchEvent : UnityEvent<int, GameObject> { }

        public ProjectilesInitializeEvent OnProjectilesInitialized;
        public ProjectileSwitchEvent OnProjectileSwitched;
        public ProjectileLaunchEvent OnProjectileLaunched;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            OnProjectilesInitialized = new ProjectilesInitializeEvent();
            OnProjectileSwitched = new ProjectileSwitchEvent();
            OnProjectileLaunched = new ProjectileLaunchEvent();
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                initialized = false;
                lateInitialized = false;
                initializedProjectiles = false;
                if (loadScene != OWScene.SolarSystem) return;
                playerBody = FindObjectOfType<PlayerBody>();
                projectiles = new GameObject[] {
                    GameObject.Find("VolcanicMoon_Body/Sector_VM/Effects_VM/VolcanoPivot/MeteorLauncher").GetComponent<MeteorLauncher>()._meteorPrefab,
                    GameObject.Find("Player_Body/RoastingSystem").GetComponent<RoastingStickController>()._mallowBodyPrefab
                };
                projectiles[0].name = "Meteor";
                projectiles[1].name = "Marshmallow";
                initializedProjectiles = true;
                OnProjectilesInitialized?.Invoke();
                sunFluid = GameObject.Find("Sun_Body/Sector_SUN/Volumes_SUN/ScaledVolumesRoot/DestructionFluidVolume").GetComponent<SimpleFluidVolume>();
                audio = GameObject.Find("Player_Body/Audio_Player/OneShotAudio_Player").GetComponent<OWAudioSource>();
                text = Instantiate(GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas/SecondaryGroup/GForce/NumericalReadout/GravityText"), GameObject.Find("PlayerHUD/HelmetOnUI/UICanvas/SecondaryGroup/GForce/NumericalReadout").transform).GetComponent<Text>();
                initialized = true;
                StartCoroutine(LateInitialize());
            };
        }

        private IEnumerator LateInitialize()
        {
            yield return new WaitForEndOfFrame();
            launcher = FindObjectOfType<FirstPersonManipulator>().transform;
            if (useOWInput)
                text.text = "Press L or Roll + Back\nto switch projectiles"; //"Selected Projectile:\n" + projectiles[p].name;
            else
                text.text = "Press L\nto switch projectiles";
            text.transform.localPosition = new Vector3(-150, 290, 0);
            lateInitialized = true;
        }

        private GameObject LaunchMeteor()
        {
            if (projectiles == null) return null;
            if (projectiles.Length == 0) return null;
            if (!lateInitialized) return null;
            if (p >= projectiles.Length)
                p = 0;
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
            if (p == 0)
                OnMeteorLaunched(newMeteor);
            else if (p == 1)
                OnMarshmallowLaunched(newMeteor);
            OnProjectileLaunched?.Invoke(p, newMeteor);
            audio.PlayOneShot(AudioType.BH_MeteorLaunch, 0.25f);
            return newMeteor;
        }

        private void OnMeteorLaunched(GameObject newMeteor)
        {
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

        private void OnMarshmallowLaunched(GameObject marshmallow)
        {
            marshmallow.GetComponent<Rigidbody>().velocity = launcher.forward * (launchSpeed / 4);
            Destroy(marshmallow.GetComponent<SelfDestruct>());
            marshmallow.GetComponentInChildren<MeshRenderer>().material.color = new Color(1, 1, 1, 0);
        }

        private void SwitchProjectile()
        {
            p++;
            ProjectileSwitched();
        }

        private void SwitchToProjectile(int index)
        {
            p = index;
            ProjectileSwitched();
        }

        private void ProjectileSwitched()
        {
            if (projectiles == null)
            {
                p = 0;
                return;
            }
            if (p >= projectiles.Length)
                p = 0;
            if (projectiles.Length == 0) return;
            if (!initialized) return;
            text.text = "Selected Projectile:\n" + projectiles[p].name;
            audio.PlayOneShot(AudioType.Menu_ChangeTab);
            timer = 100;
            OnProjectileSwitched?.Invoke(p);
        }

        private void Update()
        {
            if ((OWInput.IsPressed(InputLibrary.rollMode) && BackPressed()) || Keyboard.current.lKey.wasPressedThisFrame)
                SwitchProjectile();
            else if (BackPressed() || Mouse.current.middleButton.wasPressedThisFrame)
                LaunchMeteor();
        }

        private void FixedUpdate()
        {
            if (timer == 0)
            {
                text.text = "";
            }
            if (timer >= 0)
            {
                timer--;
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
            
        public override object GetApi() => new Api();

        public class Api
        {
            public GameObject[] GetProjectiles() => Instance.projectiles;

            public void AddProjectile(GameObject projectile)
            {
                if (!Instance.initializedProjectiles)
                {
                    Instance.ModHelper.Console.WriteLine("Cannot add a projectile when the projectiles array is not initialized!", MessageType.Error);
                    return;
                }
                List<GameObject> projectiles = Instance.projectiles.ToList();
                projectiles.Add(projectile);
                Instance.projectiles = projectiles.ToArray();
            }

            public bool IsInitialized() => Instance.initialized;
            public bool IsLateInitialized() => Instance.lateInitialized;
            public bool IsProjectilesInitialized() => Instance.initializedProjectiles;

            public int GetSelectedProjectileIndex() => Instance.p;

            public GameObject LaunchProjectile() => Instance.LaunchMeteor();

            public void SwitchProjectile() => Instance.SwitchProjectile();

            public void SwitchToProjectile(int index) => Instance.SwitchToProjectile(index);

            public UnityEvent GetProjectilesInitializedEvent()
            {
                return Instance.OnProjectilesInitialized;
            }

            public UnityEvent<int, GameObject> GetProjectileLaunchedEvent()
            {
                return Instance.OnProjectileLaunched;
            }

            public UnityEvent<int> GetProjectileSwitchedEvent()
            {
                return Instance.OnProjectileSwitched;
            }
        }
    }
}
