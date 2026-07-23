using UnityEngine;
using DG.Tweening;
using Dott;

public class DOTween_ReverseTween : MonoBehaviour
{
    [Tooltip("The GameObject holding the DOTweenAnimation or DOTweenTimeline. Leave empty to use this GameObject.")]
    [SerializeField] private GameObject target;

    [Tooltip("Print detailed logs for wiring / tween state debugging.")]
    [SerializeField] private bool verboseLogging = false;

    private DOTweenTimeline[] timelines;
    private DOTweenAnimation[] animations;

    private void Start()
    {
        // Initialize at Start so the target sits at its true starting position when DOTween captures each tween's "from" value.
        // Also disable AutoKill so the Sequence/tween survives the forward play and can be reversed later.
        GameObject go = target != null ? target : gameObject;
        timelines = go.GetComponents<DOTweenTimeline>();
        animations = go.GetComponents<DOTweenAnimation>();

        if (timelines.Length > 0)
        {
            foreach (var timeline in timelines)
            {
                timeline.Play(); // creates Sequence, captures current pos as "from" for every child
                if (timeline.Sequence != null)
                {
                    timeline.Sequence.SetAutoKill(false);
                    timeline.Sequence.Pause();
                    timeline.Sequence.Goto(0f, false); // snap target back to start, do not play
                }
            }
        }
        else
        {
            foreach (var anim in animations)
            {
                anim.CreateTween(regenerateIfExists: false, andPlay: false);
                if (anim.tween != null)
                {
                    anim.tween.SetAutoKill(false);
                    anim.tween.Pause();
                    anim.tween.Goto(0f, false);
                }
            }
        }
    }

    // Plays the tween normally. WIRE YOUR OnCollide UnityEvent TO THIS instead of DOTweenTimeline.DOPlay(), otherwise the direction stays "backward" after a reverse and the cycle breaks.
    public void PlayForward()
    {
        if (verboseLogging) Debug.Log($"[{name}] PlayForward()", this);

        if (timelines != null)
        {
            foreach (var t in timelines) t.Sequence?.PlayForward();
        }
        if (animations != null && (timelines == null || timelines.Length == 0))
        {
            foreach (var a in animations) a.tween?.PlayForward();
        }
    }

    // Plays the tween in reverse. Wire this to your OnExit UnityEvent.
    public void PlayReverse()
    {
        if (verboseLogging) Debug.Log($"[{name}] PlayReverse()", this);

        if (timelines != null)
        {
            foreach (var t in timelines) t.Sequence?.PlayBackwards();
        }
        if (animations != null && (timelines == null || timelines.Length == 0))
        {
            foreach (var a in animations) a.tween?.PlayBackwards();
        }
    }
}
