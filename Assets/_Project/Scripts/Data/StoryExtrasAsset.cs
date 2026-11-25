using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>M4 묶음 에셋: 특성·시설·일기/코멘트 줄·0막 프롤로그 (M5: 엔딩). 원본은 Content JSON (가져오기 도구가 채운다).</summary>
    [CreateAssetMenu(menuName = "ApoGirl/Story Extras", fileName = "StoryExtras")]
    public sealed class StoryExtrasAsset : ScriptableObject
    {
        [SerializeField] private List<TraitDef> _traits = new List<TraitDef>();
        [SerializeField] private List<FacilityDef> _facilities = new List<FacilityDef>();
        [SerializeField] private List<LineDef> _lines = new List<LineDef>();
        [SerializeField] private PrologueDef _prologue = new PrologueDef();
        [SerializeField] private List<EndingDef> _endings = new List<EndingDef>();
        [SerializeField] private List<RoleDef> _roles = new List<RoleDef>();
        [SerializeField] private List<StartBonusDef> _startBonuses = new List<StartBonusDef>();
        [SerializeField] private List<FactionDef> _factions = new List<FactionDef>();
        [SerializeField] private List<AdviceDef> _advice = new List<AdviceDef>();

        public DefinitionExtras ToDefinition()
        {
            return new DefinitionExtras
            {
                Traits = new List<TraitDef>(_traits),
                Facilities = new List<FacilityDef>(_facilities),
                Lines = new List<LineDef>(_lines),
                Prologue = _prologue ?? new PrologueDef(),
                Endings = new List<EndingDef>(_endings ?? new List<EndingDef>()),
                Roles = new List<RoleDef>(_roles ?? new List<RoleDef>()),
                StartBonuses = new List<StartBonusDef>(_startBonuses ?? new List<StartBonusDef>()),
                Factions = new List<FactionDef>(_factions ?? new List<FactionDef>()),
                Advice = new List<AdviceDef>(_advice ?? new List<AdviceDef>()),
            };
        }

#if UNITY_EDITOR
        public void EditorSet(List<TraitDef> traits, List<FacilityDef> facilities, List<LineDef> lines, PrologueDef prologue, List<EndingDef> endings, List<RoleDef> roles, List<StartBonusDef> startBonuses, List<FactionDef> factions, List<AdviceDef> advice)
        {
            _factions = factions;
            _advice = advice;
            _startBonuses = startBonuses;
            _roles = roles;
            _endings = endings;
            _traits = traits;
            _facilities = facilities;
            _lines = lines;
            _prologue = prologue;
        }
#endif
    }
}
