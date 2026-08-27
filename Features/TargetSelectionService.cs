namespace DesktopScroll;

public sealed class TargetSelectionService
{
    private readonly OverlayService _overlayService;
    private readonly AppStateMachine _stateMachine;
    private string _typedPrefix = string.Empty;
    private List<GridCell> _candidates = new();
    private int _selectedCandidateIndex;

    public TargetSelectionService(OverlayService overlayService, AppStateMachine stateMachine)
    {
        _overlayService = overlayService;
        _stateMachine = stateMachine;
    }

    public void EnterSelectionMode()
    {
        _stateMachine.EnterTargetSelection();
        _overlayService.ShowOverlays();
        _typedPrefix = string.Empty;
        _candidates = _overlayService.ApplyPrefix(_typedPrefix).ToList();
        _selectedCandidateIndex = 0;
        HighlightSelection();
    }

    public void ExitSelectionMode()
    {
        _typedPrefix = string.Empty;
        _candidates.Clear();
        _selectedCandidateIndex = 0;
        _overlayService.CloseOverlays();
        _stateMachine.ExitScrollMode();
    }

    public bool HandleKeyDown(Keys key, out Point? confirmedTarget)
    {
        confirmedTarget = null;

        if (_stateMachine.CurrentMode != AppMode.TargetSelection)
        {
            return false;
        }

        if (key is Keys.Left or Keys.Right or Keys.Up or Keys.Down)
        {
            if (_typedPrefix.Length > 0 && _typedPrefix.Length < _overlayService.LabelLength)
            {
                MoveSelection(key);
                HighlightSelection();
                return true;
            }

            return false;
        }

        var letter = ToLowerLetter(key);
        if (letter is null)
        {
            return false;
        }

        if (_typedPrefix.Length >= _overlayService.LabelLength)
        {
            _typedPrefix = string.Empty;
        }

        _typedPrefix += letter.Value;
        _candidates = _overlayService.ApplyPrefix(_typedPrefix).ToList();
        _selectedCandidateIndex = 0;
        HighlightSelection();

        if (_typedPrefix.Length == _overlayService.LabelLength && _overlayService.TryResolveLabel(_typedPrefix, out var cell))
        {
            confirmedTarget = cell.Center;
            _stateMachine.SetLastTarget(cell.Center);
            _overlayService.CloseOverlays();
            return true;
        }

        return true;
    }

    private void MoveSelection(Keys direction)
    {
        if (_candidates.Count <= 1)
        {
            return;
        }

        var current = _candidates[Math.Clamp(_selectedCandidateIndex, 0, _candidates.Count - 1)];
        var nextIndex = _selectedCandidateIndex;
        var bestScore = double.MaxValue;

        for (var i = 0; i < _candidates.Count; i++)
        {
            if (i == _selectedCandidateIndex)
            {
                continue;
            }

            var candidate = _candidates[i];
            var dx = candidate.Center.X - current.Center.X;
            var dy = candidate.Center.Y - current.Center.Y;

            var validDirection = direction switch
            {
                Keys.Left => dx < 0,
                Keys.Right => dx > 0,
                Keys.Up => dy < 0,
                Keys.Down => dy > 0,
                _ => false
            };

            if (!validDirection)
            {
                continue;
            }

            var score = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(direction is Keys.Left or Keys.Right ? dy : dx) * 0.5;
            if (score < bestScore)
            {
                bestScore = score;
                nextIndex = i;
            }
        }

        _selectedCandidateIndex = nextIndex;
    }

    private void HighlightSelection()
    {
        if (_candidates.Count == 0)
        {
            _overlayService.HighlightCell(null);
            return;
        }

        _selectedCandidateIndex = Math.Clamp(_selectedCandidateIndex, 0, _candidates.Count - 1);
        _overlayService.HighlightCell(_candidates[_selectedCandidateIndex]);
    }

    private static char? ToLowerLetter(Keys key)
    {
        if (key is >= Keys.A and <= Keys.Z)
        {
            return char.ToLowerInvariant((char)('A' + (key - Keys.A)));
        }

        return null;
    }
}
