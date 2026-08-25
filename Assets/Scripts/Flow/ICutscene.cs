namespace Game.Flow
{
    public interface ICutscene
    {
        bool IsPlaying { get; }

        void Play();

        void Skip();

        void Advance(float deltaSeconds);
    }
}
