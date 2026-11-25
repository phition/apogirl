using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Planning;
using ApoGirl.Core.State;

namespace ApoGirl.Systems.Session
{
    /// <summary>
    /// 계획 보드의 편집 기록 (Docs/TEACHER_PLAN_PROPOSAL.md 5.3): 되돌리기·다시 하기와 선생님 분필 자국 (칸마다 선생님이 넣었는지·까닭).
    /// 세이브에 넣지 않는다 (결정 D3). 주가 바뀌거나 판을 불러오면 비운다.
    /// </summary>
    public sealed class PlanBoardMemory
    {
        public const int MaxSteps = 40;

        private sealed class Entry
        {
            public string[] Ids;
            public bool[] Teacher;
            public bool[] Pinned;
            public TeacherNote[] Reasons;
            public List<TeacherNote> Notes;
        }

        private readonly List<Entry> _undo = new List<Entry>();
        private readonly List<Entry> _redo = new List<Entry>();
        private int _week = -1;

        public readonly bool[] Teacher = new bool[WeekPlan.SlotCount];
        public readonly bool[] Pinned = new bool[WeekPlan.SlotCount];
        public readonly TeacherNote[] Reasons = new TeacherNote[WeekPlan.SlotCount];
        public readonly List<TeacherNote> Notes = new List<TeacherNote>();

        public bool CanUndo => _undo.Count > 0;

        public bool CanRedo => _redo.Count > 0;

        /// <summary>다른 주로 넘어갔으면 기록을 비운다.</summary>
        public void Sync(int week)
        {
            if (week != _week)
            {
                Clear();
                _week = week;
            }
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
            ClearMarks();
        }

        public void ClearMarks()
        {
            Array.Clear(Teacher, 0, Teacher.Length);
            Array.Clear(Pinned, 0, Pinned.Length);
            Array.Clear(Reasons, 0, Reasons.Length);
            Notes.Clear();
        }

        /// <summary>편집 직전 상태를 되돌리기 기록에 쌓는다 (다시 하기 기록은 버린다).</summary>
        public void Push(GameState state)
        {
            _undo.Add(Capture(state));
            if (_undo.Count > MaxSteps)
            {
                _undo.RemoveAt(0);
            }

            _redo.Clear();
        }

        /// <summary>바뀐 것이 없던 편집: 방금 쌓은 기록을 뺀다.</summary>
        public void Drop()
        {
            if (_undo.Count > 0)
            {
                _undo.RemoveAt(_undo.Count - 1);
            }
        }

        public bool Undo(GameState state, IDefinitionDatabase db) => Step(state, db, _undo, _redo);

        public bool Redo(GameState state, IDefinitionDatabase db) => Step(state, db, _redo, _undo);

        /// <summary>플레이어가 고친 칸은 분필 자국을 지운다 (선생님 칸이 아니게 된다).</summary>
        public void ForgetChanged(string[] before, string[] after)
        {
            for (var i = 0; i < Teacher.Length && i < before.Length && i < after.Length; i++)
            {
                if (before[i] != after[i])
                {
                    Teacher[i] = false;
                    Pinned[i] = false;
                    Reasons[i] = null;
                    Notes.RemoveAll(n => n.Slot == i);
                }
            }
        }

        public void SetTeacher(TeacherResult result)
        {
            for (var i = 0; i < Teacher.Length; i++)
            {
                Teacher[i] = result.Placed[i];
                Pinned[i] = result.Pinned[i];
                Reasons[i] = result.Reasons[i];
            }

            Notes.Clear();
            Notes.AddRange(result.Notes);
        }

        private bool Step(GameState state, IDefinitionDatabase db, List<Entry> from, List<Entry> to)
        {
            if (from.Count == 0)
            {
                return false;
            }

            var entry = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);
            to.Add(Capture(state));
            PlanEditing.Restore(state, db, entry.Ids);
            Array.Copy(entry.Teacher, Teacher, Teacher.Length);
            Array.Copy(entry.Pinned, Pinned, Pinned.Length);
            Array.Copy(entry.Reasons, Reasons, Reasons.Length);
            Notes.Clear();
            Notes.AddRange(entry.Notes);
            return true;
        }

        private Entry Capture(GameState state)
        {
            return new Entry
            {
                Ids = PlanEditing.Snapshot(state),
                Teacher = (bool[])Teacher.Clone(),
                Pinned = (bool[])Pinned.Clone(),
                Reasons = (TeacherNote[])Reasons.Clone(),
                Notes = Notes.ToList(),
            };
        }
    }
}
