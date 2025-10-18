using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NeoTools.Utils;

public class FsmState
{
    public string Name { get; }
    public Func<bool>? CanEnter { get; set; }
    public Func<bool>? CanExit { get; set; }
    public Action<float>? OnUpdate { get; set; }
    public Action? OnEntered { get; set; }
    public Action? OnExited { get; set; }

    public FsmState(string name)
    {
        Name = name;
    }
}

public class AnonymousState
{
    public FsmState State { get; }

    public AnonymousState
    (
        string Name,
        Action? onEntered = null,
        Action? onExited = null,
        Action<float>? onUpdate = null,
        Func<bool>? canEnter = null,
        Func<bool>? canExit = null
    )
    {
        var state = new FsmState(Name);
        state.CanEnter = canEnter;
        state.CanExit = canExit;
        state.OnEntered = onEntered;
        state.OnExited = onExited;
        state.OnUpdate = onUpdate;

        State = state;
    }
}

public class Fsm
{
    public enum Mode
    {
        AUTO,
        MANUAL,
        COMBINE,
    }

    public Mode RuleMode { get; set; } = Mode.COMBINE;
    public bool Debug { get; set; } = false;

    private HashSet<FsmState> _states = new();
    protected FsmState? curState;
    protected FsmState? prevState;

    public Fsm(List<AnonymousState> states)
    {
        foreach (var state in states)
            AddState(state.State);


        var firstState = _states.FirstOrDefault();
        if (firstState == null)
            return;
        ChangeState(firstState);
    }

    public Fsm(ICollection<FsmState>? states = null)
    {
        if (states == null)
            return;

        _states = states.ToHashSet<FsmState>();
        var firstState = _states.FirstOrDefault();
        if (firstState == null)
            return;
        ChangeState(firstState);
    }

    public void AddState(FsmState state)
    {
        if (_states.Contains(state))
            return;

        if (_states.FirstOrDefault(s => s.Name == state.Name) != null)
        {
            GD.PrintErr($"State {state.Name} already is exist");
            return;
        }

        _states.Add(state);
    }

    public void Tick(float dt)
    {
        if (RuleMode == Mode.MANUAL)
        {
            curState?.OnUpdate?.Invoke(dt);
            return;
        }

        if (curState != null && curState.CanEnter != null && (curState?.CanExit != null && !curState.CanExit()))
        {
            curState.OnUpdate?.Invoke(dt);
            return;
        }

        foreach (var state in _states)
        {
            if (state.CanEnter == null || !state.CanEnter())
                continue;

            ChangeState(state);
            break;
        }

        curState?.OnUpdate?.Invoke(dt);
    }

    public void ChangeState(string name)
    {
        if (RuleMode == Mode.AUTO)
            return;

        var newState = _states.FirstOrDefault(s => s.Name == name);
        if (newState == null)
        {
            GD.PrintErr($"State {name} not exist");
            return;
        }
        ChangeState(newState);
    }

    private void ChangeState(FsmState newState)
    {
        if (curState?.CanExit != null && !curState.CanExit())
            return;
        if (newState == null || curState == newState)
            return;

        curState?.OnExited?.Invoke();
        newState.OnEntered?.Invoke();
        prevState = curState;
        curState = newState;

        if (Debug)
            GD.Print($"Change state from '{prevState?.Name ?? "null"}' to '{curState?.Name}'");
    }
}
