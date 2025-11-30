using UnityEngine;
using System.Collections.Generic;

public class BillboardBusiness : DecorBusiness {
    [SerializeField] private Vector2 _boxSize = new(2f, 2f);
    [SerializeField] private float _cooldown = 0.5f;
    [SerializeField] private int _maxColliders = 32;
    private readonly HashSet<Car> _registry = new();

    private Collider2D[] _results;
    private float _timer = 0;

    private void Awake() {
        _results = new Collider2D[_maxColliders];
    }

    private void Poll2D() {
        int count = Physics2D.OverlapBoxNonAlloc(transform.position, _boxSize, 0f, _results);
        for (int i = 0; i < count; i++) {
            var col = _results[i];
            if (!col || !col.TryGetComponent(out Car car)) continue;
            if (_registry.Contains(car)) continue;
            _registry.Add(car);
            MakeMoney();
        }
    }

    private void Update() {
        Poll2D();
        CleanupOldEntries();
    }

    private void CleanupOldEntries() {
        _timer += Time.deltaTime;
        if (_timer >= _cooldown) {
            _timer -= _cooldown;
            _registry.Clear();
        }
    }
}