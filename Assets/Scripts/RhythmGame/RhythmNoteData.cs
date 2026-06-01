using UnityEngine;

public enum NoteDirection { Left, Down, Up, Right }
public enum RhythmJudgment { None, Perfect, Good, Miss }

[System.Serializable]
public class RhythmNoteData
{
    public NoteDirection direction;
    public float targetTime;
    public bool isHit;
    public bool isMissed;
    public RhythmJudgment judgment = RhythmJudgment.None;
}