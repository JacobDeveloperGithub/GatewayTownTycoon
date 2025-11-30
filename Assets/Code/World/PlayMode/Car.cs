using UnityEngine;
using FSA;
using System;

public class Car : MonoBehaviour {
    private enum States {Idle, Moving, Arrived}
    private StateMachine _machine;

    [SerializeField] private float _speed = 1.5f;

    private RoadGameGraphNode _destination;
    private RoadGraph _graph;
    private RoadGameGraphNode[] _path;
    private RoadGameGraphNode _target;
    private int _index = 0;
    private bool _exited = false;

    public void Exit() => _exited = true;
    public bool ActiveInSimulation() => !_exited;

    public void Initialize(RoadGraph graph) {
        _graph = graph;
        _exited = false;

        _machine = new StateMachineBuilder()
            .WithState(States.Idle)
            .WithTransition(States.Moving, IsPathValid)

            .WithState(States.Moving)
            .WithOnRun(Move)
            .WithTransition(States.Arrived, HasArrivedAtGoal)

            .WithState(States.Arrived)
            .WithTransition(States.Idle, () => _target != null)

            .Build();
    }

    public void SetSpeed(float speed) => _speed = speed;

    public void DriveTo(RoadGameGraphNode destination, RoadGameGraphNode startAt = null) {
        _destination = destination;
        startAt ??= _graph.GetGraph().ClosestNodeToPoint(transform.position);
        Span<RoadGameGraphNode> solvedPath = _graph.GetGraph().GetPath(startAt, _destination);
        _path = new RoadGameGraphNode[solvedPath.Length];
        solvedPath.CopyTo(_path);
        _index = 0;
        _target = _path[0];
    }

    public void ExitBusiness(RoadGameGraphNode exit) {
        DriveTo(_graph.GetRandomExit(), exit);
    }

    private void Update() {
        if (_exited) return;
        if (_machine == null) return;
        _machine.RunStateMachine(Time.deltaTime);
    }

    private void Move() {
        float movement = _speed * Time.deltaTime;
        do {
            Vector2 dir = _target.Position - (Vector2)transform.position;
            float dist = dir.magnitude;
            dir = dir.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0,0,angle);
            if (movement >= dist) {
                transform.position = _target.Position;
                movement -= dist;
                SetNextTarget();
            } else {
                transform.position = Vector2.MoveTowards(transform.position, _target.Position, movement);
                movement = 0;
            }
        } while(movement > 0 && _target != null);
    }

    private void SetNextTarget() {
        _target?.OnNodeEntry?.Invoke(this);
        _index++;
        if (_index < _path.Length) {
            _target = _path[_index];
        } else {
            _target = null;
        }
    }

    private bool IsPathValid() => _path != null && _path.Length > 2;
    private bool HasArrivedAtGoal() => _target == null;
}