using CheeseTama.Gameplay.Bond;
using UnityEngine;

namespace CheeseTama.UI
{
    /// <summary>
    /// Optional presentation adapter for a bond result. It has no GameManager or
    /// save dependency, so the authoritative caller decides when a reaction wins.
    /// </summary>
    public sealed class BondReactionPresenter : MonoBehaviour
    {
        [SerializeField] private CheeseTamaSpeechBubbleController speechBubble;
        [SerializeField] private CheeseTamaVisualController visualController;

        public void Configure(
            CheeseTamaSpeechBubbleController bubble,
            CheeseTamaVisualController visual)
        {
            speechBubble = bubble;
            visualController = visual;
        }

        public bool Present(
            BondReactionResult result,
            bool playSound = false,
            bool playVisual = true)
        {
            if (!result.HasSpecialReaction || speechBubble == null)
            {
                return false;
            }

            if (!speechBubble.Show(result.Dialogue, playSound))
            {
                return false;
            }

            if (playVisual && visualController != null)
            {
                visualController.ReactAction(ResolveVisualAction(result));
            }

            return true;
        }

        public static CheeseTamaVisualAction ResolveVisualAction(BondReactionResult result)
        {
            return result.Interaction switch
            {
                BondInteraction.Feed => CheeseTamaVisualAction.FeedMilk,
                BondInteraction.Pet => CheeseTamaVisualAction.Pet,
                BondInteraction.Play => CheeseTamaVisualAction.Play,
                BondInteraction.Clean => CheeseTamaVisualAction.Clean,
                BondInteraction.Rest => CheeseTamaVisualAction.Rest,
                BondInteraction.Cook => CheeseTamaVisualAction.Cook,
                _ => result.VisualCue switch
                {
                    BondVisualCue.EnergeticHop => CheeseTamaVisualAction.Play,
                    BondVisualCue.HeartSparkle => CheeseTamaVisualAction.Pet,
                    BondVisualCue.CalmSway => CheeseTamaVisualAction.Rest,
                    BondVisualCue.FocusedNod => CheeseTamaVisualAction.Cook,
                    _ => CheeseTamaVisualAction.Neutral
                }
            };
        }
    }
}
