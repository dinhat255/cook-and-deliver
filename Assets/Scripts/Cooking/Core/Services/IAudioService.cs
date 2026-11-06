namespace CookAndDeliver.Cooking.Core.Services
{
    public enum CookingSound
    {
        Tap,
        Sizzle,
        Ding,
        Burnt,
        Trash,
        Coin,
        Pack
    }

    // Interface for audio playback service across cooking interactions
    public interface IAudioService : IService
    {
        void PlaySound(CookingSound sound);
        void PlayBGM(AudioClipSO bgmClip);
        void StopBGM(AudioClipSO bgmClip);
    }
}
