using System;
using System.Collections.Generic;
using System.IO;
using CookAndDeliver.Delivery.Core;
using UnityEngine;

namespace CookAndDeliver.Delivery
{
    public sealed class DeliveryController : MonoBehaviour
    {
        public DeliveryConfigSO config;
        public DeliveryWorldView world;
        public DeliveryUIView ui;
        public DeliverySwipeInput input;
        public AudioSource audioSource;
        public AudioClip tapClip, hitClip, deliverClip, finishClip;
        [Header("Delivery BGM & Vehicle Audio")]
        public AudioSource bgmAudioSource;
        public AudioSource vehicleAudioSource;
        public AudioClip bgmClip;
        public AudioClip vehicleMoveClip;
        [Range(0f, 1f)] public float bgmVolume = 0.45f;
        [Range(0f, 1f)] public float pausedBgmMultiplier = 0.45f;
        [Range(0f, 1f)] public float vehicleVolume = 0.55f;
        [Range(0.5f, 1.5f)] public float vehicleMinPitch = 0.88f;
        [Range(0.5f, 1.5f)] public float vehicleMaxPitch = 1.04f;
        public DeliverySimulation Simulation { get; private set; }
        public DeliveryConfigSO Config => config;
        public DeliveryProgressStore Progress { get; private set; }

        // External hooks for gameplay loop bridges (e.g. Cooking)
        public static Func<DeliveryRunRequest> ExternalRequestProvider;
        public static Func<IReadOnlyList<DeliveryOrder>> ExternalPreviewOrdersProvider;
        public static Action<DeliveryRunResult> OnExternalRunCompleted;
        public static Action OnExternalReturnRequested;
        public static Func<bool> ExternalIsTutorialCompleted;
        public static Action ExternalOnTutorialCompleted;
        public static string ExternalReturnButtonLabel = "Về quán";
        public static int PendingOrderCount = 0;

        public bool IsSimulationFrozen { get; private set; }

        private float hudTime;
        private ScreenOrientation previousOrientation;
        private bool isVehicleAudioPaused;
        private bool hasStarted;

        private void Awake()
        {
            EnsureSingleAudioListener();
            ConfigureLoopAudioSource(bgmAudioSource);
            ConfigureLoopAudioSource(vehicleAudioSource);
            config.Validate();
            Application.targetFrameRate = 60;
            previousOrientation = Screen.orientation;
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.Portrait;
            Progress = new DeliveryProgressStore(Path.Combine(Application.persistentDataPath, "Delivery"));
            Simulation = new DeliverySimulation(config.settings);
            Simulation.StateChanged += StateChanged;
            Simulation.Damaged += Damaged;
            Simulation.OrderResolved += OrderResolved;
            Simulation.RunCompleted += Completed;
            input.widthFraction = config.swipeWidthFraction;
            input.horizontalRatio = config.swipeHorizontalRatio;
            world.warningSeconds = ui.warningSeconds = config.settings.WarningSeconds;
            input.Swiped += RequestLaneChange;

            ui.startButton.onClick.AddListener(OnStartClicked);
            ui.pauseButton.onClick.AddListener(Pause);
            ui.resumeButton.onClick.AddListener(Resume);
            ui.restartButton.onClick.AddListener(OnRestartClicked);
            ui.pauseHomeButton.onClick.AddListener(OnHomeClicked);
            ui.resultReplayButton.onClick.AddListener(OnRestartClicked);
            ui.resultHomeButton.onClick.AddListener(OnHomeClicked);

            StateChanged();
        }

        private void Start()
        {
            hasStarted = true;
            world.Render(Simulation, 0);
            ui.Render(Simulation);
            StartBgm();
            SyncLoopAudio();
        }

        private void OnEnable()
        {
            if (!hasStarted || Simulation == null) return;
            StartBgm();
            SyncLoopAudio();
        }

        private void OnDisable()
        {
            StopLoopAudio();
        }

        public void StartDefaultRun(int? seed = null)
        {
            StartRun(config.CreateRequest(seed ?? Environment.TickCount));
        }

        public void StartRun(DeliveryRunRequest request)
        {
            IsSimulationFrozen = false;
            input.Cancel();
            world.ResetView();
            hudTime = 0;
            StopVehicleAudio();
            Simulation.StartRun(request);
            ui.Render(Simulation);
            SyncLoopAudio();
        }

        private void OnStartClicked()
        {
            Play(tapClip);
            if (ExternalRequestProvider != null)
            {
                var req = ExternalRequestProvider.Invoke();
                if (req != null)
                {
                    StartRun(req);
                    return;
                }
            }
            StartDefaultRun();
        }

        private void OnRestartClicked()
        {
            Play(tapClip);
            if (ExternalRequestProvider != null)
            {
                var req = ExternalRequestProvider.Invoke();
                if (req != null)
                {
                    StartRun(req);
                    return;
                }
            }
            StartDefaultRun();
        }

        private void OnHomeClicked()
        {
            Play(tapClip);
            if (OnExternalReturnRequested != null)
            {
                OnExternalReturnRequested.Invoke();
            }
            else
            {
                AbortRun();
            }
        }

        public void SetSimulationFrozen(bool frozen)
        {
            IsSimulationFrozen = frozen;
            input.Cancel();
            SyncLoopAudio();
        }

        public void RequestLaneChange(int direction)
        {
            if (IsSimulationFrozen) return;
            Simulation.RequestLaneChange(direction);
        }
        public void Pause() { IsSimulationFrozen = false; input.Cancel(); Simulation.Pause(); Play(tapClip); }
        public void Resume() { IsSimulationFrozen = false; input.Cancel(); Simulation.Resume(); Play(tapClip); }

        public void AbortRun()
        {
            IsSimulationFrozen = false;
            input.Cancel();
            StopVehicleAudio();
            Simulation.AbortRun();
            world.ResetView();
            world.Render(Simulation, 0);
            ui.Render(Simulation);
            Play(tapClip);
            SyncLoopAudio();
        }

        private void Update()
        {
            var dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            if (!IsSimulationFrozen)
            {
                Simulation.Tick(dt);
                world.Render(Simulation, dt, Simulation.InterpolationAlpha);
            }
            else
            {
                world.Render(Simulation, 0f, Simulation.InterpolationAlpha);
            }
            SyncLoopAudio();
            hudTime -= dt;
            if (hudTime <= 0) { ui.Render(Simulation); hudTime = .1f; }
            if (Simulation.State == DeliveryState.Running) ui.Tick(dt);
            ui.AnimateDeliveryWarning(Simulation, world.DeliveryPulse);
        }

        private void StateChanged()
        {
            input.Cancel();
            input.Enabled = Simulation.CanSteer;
            ui.SetState(Simulation.State);
            if (Simulation.State == DeliveryState.Ready) ui.SetReady(config, Progress.Data);
            ui.Render(Simulation);
            SyncLoopAudio();
        }

        private void Damaged(int integrity)
        {
            world.Hit(Simulation.RiderX, Simulation.Distance);
            Play(hitClip);
            ui.Notify("−" + config.settings.CollisionDamage + "%");
            ui.Render(Simulation);
        }

        private void OrderResolved(OrderResult result)
        {
            if (result.Outcome == OrderOutcome.Delivered)
            {
                Play(deliverClip);
                ui.Notify("+" + result.Reward);
            }
            else if (result.Outcome == OrderOutcome.Missed)
            {
                ui.Notify("Lỡ đơn");
            }
            ui.Render(Simulation);
        }

        private void Completed(DeliveryRunResult result)
        {
            var saved = Progress.Record(result);
            ui.ShowResult(result, Progress.Data, saved);
            Play(finishClip);
            OnExternalRunCompleted?.Invoke(result);
        }

        private void EnsureSingleAudioListener()
        {
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length <= 1) return;

            var localScene = gameObject.scene;
            bool hasExternalListener = false;
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i].enabled && listeners[i].gameObject.scene != localScene)
                {
                    hasExternalListener = true;
                    break;
                }
            }

            if (!hasExternalListener) return;
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i].gameObject.scene == localScene)
                {
                    listeners[i].enabled = false;
                }
            }
        }

        private static void ConfigureLoopAudioSource(AudioSource source)
        {
            if (source == null) return;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
        }

        private void StartBgm()
        {
            if (bgmAudioSource == null || bgmClip == null) return;
            if (bgmAudioSource.clip != bgmClip) bgmAudioSource.clip = bgmClip;
            bgmAudioSource.loop = true;
            bgmAudioSource.spatialBlend = 0f;
            bgmAudioSource.volume = TargetBgmVolume();
            if (!bgmAudioSource.isPlaying) bgmAudioSource.Play();
        }

        private float TargetBgmVolume()
        {
            if (Simulation != null && Simulation.State == DeliveryState.Paused)
            {
                return bgmVolume * pausedBgmMultiplier;
            }
            return bgmVolume;
        }

        private void SyncLoopAudio()
        {
            if (bgmAudioSource != null && bgmClip != null)
            {
                if (!bgmAudioSource.isPlaying && isActiveAndEnabled)
                {
                    StartBgm();
                }
                else
                {
                    bgmAudioSource.volume = TargetBgmVolume();
                }
            }

            UpdateVehicleAudio();
        }

        private void UpdateVehicleAudio()
        {
            if (vehicleAudioSource == null || vehicleMoveClip == null || Simulation == null) return;

            bool isMoving = Simulation.State == DeliveryState.Running && !IsSimulationFrozen;
            if (isMoving)
            {
                if (vehicleAudioSource.clip != vehicleMoveClip)
                {
                    vehicleAudioSource.clip = vehicleMoveClip;
                    vehicleAudioSource.loop = true;
                    vehicleAudioSource.spatialBlend = 0f;
                    isVehicleAudioPaused = false;
                }

                float baseSpeed = Mathf.Max(1f, Simulation.BaseSpeed);
                float speedRatio = Mathf.Clamp01(Simulation.Speed / baseSpeed);
                vehicleAudioSource.volume = vehicleVolume * Mathf.Lerp(0.72f, 1f, speedRatio);
                vehicleAudioSource.pitch = Mathf.Lerp(vehicleMinPitch, vehicleMaxPitch, speedRatio);

                if (!vehicleAudioSource.isPlaying)
                {
                    if (isVehicleAudioPaused)
                    {
                        vehicleAudioSource.UnPause();
                        isVehicleAudioPaused = false;
                    }
                    else
                    {
                        vehicleAudioSource.Play();
                    }
                }
                return;
            }

            if (Simulation.State == DeliveryState.Paused || (Simulation.State == DeliveryState.Running && IsSimulationFrozen))
            {
                if (vehicleAudioSource.isPlaying)
                {
                    vehicleAudioSource.Pause();
                    isVehicleAudioPaused = true;
                }
                return;
            }

            StopVehicleAudio();
        }

        private void StopVehicleAudio()
        {
            isVehicleAudioPaused = false;
            if (vehicleAudioSource != null && (vehicleAudioSource.isPlaying || vehicleAudioSource.time > 0f))
            {
                vehicleAudioSource.Stop();
            }
        }

        private void StopLoopAudio()
        {
            StopVehicleAudio();
            if (bgmAudioSource != null && bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Stop();
            }
        }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }
        private void OnApplicationPause(bool pause) { if (pause && Simulation != null) Pause(); }
        private void OnApplicationFocus(bool focus) { if (!focus && Simulation != null) Pause(); }

        private void OnDestroy()
        {
            StopLoopAudio();
            if (Simulation == null) return;
            if (Application.isMobilePlatform) Screen.orientation = previousOrientation;
            Simulation.StateChanged -= StateChanged;
            Simulation.Damaged -= Damaged;
            Simulation.OrderResolved -= OrderResolved;
            Simulation.RunCompleted -= Completed;
            input.Swiped -= RequestLaneChange;
        }
    }
}
