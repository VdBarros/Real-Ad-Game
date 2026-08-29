using Game.Presentation.Pure;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game.Presentation
{
    [ExecuteAlways]
    public sealed class FigureAnimator : MonoBehaviour
    {
        Animator rig;

        WorldModels library;

        PartModel worn;

        PlayableGraph graph;

        AnimationPlayableOutput output;

        AnimationClipPlayable playing;

        AnimationClip loaded;

        FigureMotion motion = FigureMotion.Still;

        bool grafted;

        public FigureAct Act
        {
            get { return motion.Cue.Act; }
        }

        public AnimationClip Playing
        {
            get { return loaded; }
        }

        public float PlayingSeconds
        {
            get { return loaded == null ? 0f : loaded.length; }
        }

        public float PlayingTime
        {
            get { return loaded == null ? 0f : motion.TimeIn(loaded.length); }
        }

        public bool IsRigged
        {
            get { return rig != null; }
        }

        public bool HasClipsToPlay
        {
            get { return grafted; }
        }

        public static FigureAnimator Raise(GameObject figure, PartModel mesh, WorldModels models)
        {
            if (figure == null || models == null || !ArtPacks.IsRiggedCharacter(mesh))
            {
                return null;
            }

            var animator = figure.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning(
                    "The " + mesh + " mesh carries no animator, so the figure it dresses keeps the static pose "
                    + "the import gave it. Check that the character postprocessor still builds a rig.");
                return null;
            }

            var driven = figure.AddComponent<FigureAnimator>();
            driven.Begin(animator, mesh, models);
            return driven;
        }

        void Begin(Animator animator, PartModel mesh, WorldModels models)
        {
            rig = animator;
            worn = mesh;
            library = models;
            rig.applyRootMotion = false;
            rig.runtimeAnimatorController = null;
            rig.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            graph = PlayableGraph.Create("figure-" + name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            output = AnimationPlayableOutput.Create(graph, "figure", rig);
            grafted = false;

            Load();
        }

        public void Cue(FigureCue wanted)
        {
            motion = motion.Cued(wanted);
        }

        public void Advance(float deltaSeconds)
        {
            motion = motion.Advanced(deltaSeconds);
            Sample();
        }

        void Sample()
        {
            var clip = Load();
            if (clip == null)
            {
                return;
            }

            playing.SetTime(motion.TimeIn(clip.length));
            graph.Evaluate(0f);
        }

        AnimationClip Load()
        {
            if (!graph.IsValid())
            {
                return null;
            }

            var clip = library.ClipOf(worn, motion.Cue.Clip);
            if (clip == null)
            {
                return null;
            }

            if (!ReferenceEquals(clip, loaded))
            {
                Graft(clip);
            }

            return clip;
        }

        void Graft(AnimationClip clip)
        {
            if (playing.IsValid())
            {
                graph.DestroyPlayable(playing);
            }

            loaded = clip;
            playing = AnimationClipPlayable.Create(graph, clip);
            playing.SetApplyFootIK(false);
            playing.SetApplyPlayableIK(false);
            playing.SetSpeed(0d);
            output.SetSourcePlayable(playing);
            grafted = true;
        }

        void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Advance(Time.deltaTime);
        }

        void OnDestroy()
        {
            if (graph.IsValid())
            {
                graph.Destroy();
            }

            loaded = null;
            library = null;
            grafted = false;
        }
    }
}
