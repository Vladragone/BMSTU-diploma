public interface IFsmPuzzleObject
{
    string ObjectId { get; }

    bool ApplyPuzzleEvent(PuzzleEvent puzzleEvent);
}