namespace DesktopScroll.Tests;

public sealed class AppStateMachineTests
{
    [Fact]
    public void EnterTargetSelection_SetsSelectionState()
    {
        var stateMachine = new AppStateMachine();

        stateMachine.EnterTargetSelection();

        Assert.Equal(AppMode.TargetSelection, stateMachine.CurrentMode);
        Assert.True(stateMachine.IsTargetSelectionVisible);
    }

    [Fact]
    public void EnterScrollMode_StoresTargetAndHidesSelection()
    {
        var stateMachine = new AppStateMachine();
        var target = new Point(480, 270);

        stateMachine.EnterTargetSelection();
        stateMachine.EnterScrollMode(target);

        Assert.Equal(AppMode.ScrollMode, stateMachine.CurrentMode);
        Assert.False(stateMachine.IsTargetSelectionVisible);
        Assert.Equal(target, stateMachine.GetLastTarget());
    }

    [Fact]
    public void ExitScrollMode_ReturnsToIdleAndPreservesLastTarget()
    {
        var stateMachine = new AppStateMachine();
        var target = new Point(-320, 640);
        stateMachine.EnterScrollMode(target);

        stateMachine.ExitScrollMode();

        Assert.Equal(AppMode.Idle, stateMachine.CurrentMode);
        Assert.False(stateMachine.IsTargetSelectionVisible);
        Assert.Equal(target, stateMachine.GetLastTarget());
    }
}